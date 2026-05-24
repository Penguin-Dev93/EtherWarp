using System.Diagnostics;
using System.Net.NetworkInformation;
using EtherWarp.Models;

namespace EtherWarp.Services;

public class NetworkService
{
    private static readonly string[] _virtualKeywords =
    [
        "virtual", "hyper-v", "vmware", "vethernet", "loopback",
        "tunnel", "isatap", "teredo", "6to4"
    ];

    public List<string> GetPhysicalAdapterNames()
    {
        try
        {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni =>
                    ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet &&
                    ni.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                    (ni.OperationalStatus == OperationalStatus.Up ||
                     ni.OperationalStatus == OperationalStatus.Down) &&
                    !_virtualKeywords.Any(kw =>
                        ni.Description.Contains(kw, StringComparison.OrdinalIgnoreCase)))
                .Select(ni => ni.Name)
                .OrderBy(n => n)
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    public async Task<(bool Success, string Message)> ApplyPresetAsync(NetworkPreset preset)
    {
        // Step 1: Set static IP
        string ipArgs;
        if (!string.IsNullOrWhiteSpace(preset.Gateway))
        {
            ipArgs = $"interface ip set address name=\"{preset.AdapterName}\" " +
                     $"static {preset.IPAddress} {preset.SubnetMask} {preset.Gateway} 1";
        }
        else
        {
            ipArgs = $"interface ip set address name=\"{preset.AdapterName}\" " +
                     $"static {preset.IPAddress} {preset.SubnetMask}";
        }

        var ipResult = await RunNetshAsync(ipArgs);
        if (!ipResult.Success)
            return ipResult;

        // Step 2: Set DNS (only if at least PrimaryDNS is set)
        if (!string.IsNullOrWhiteSpace(preset.PrimaryDNS))
        {
            var dnsResult = await RunNetshAsync(
                $"interface ip set dns name=\"{preset.AdapterName}\" static {preset.PrimaryDNS} primary");
            if (!dnsResult.Success)
                return dnsResult;

            if (!string.IsNullOrWhiteSpace(preset.SecondaryDNS))
            {
                var dns2Result = await RunNetshAsync(
                    $"interface ip add dns name=\"{preset.AdapterName}\" {preset.SecondaryDNS} index=2");
                if (!dns2Result.Success)
                    return dns2Result;
            }
        }

        return await VerifyAppliedAsync(preset);
    }

    public async Task<(bool Success, string Message)> ResetToDHCPAsync(string adapterName)
    {
        var ipResult = await RunNetshAsync(
            $"interface ip set address name=\"{adapterName}\" dhcp");
        if (!ipResult.Success)
            return (false, $"Failed to reset IP: {ipResult.Message}");

        var dnsResult = await RunNetshAsync(
            $"interface ip set dns name=\"{adapterName}\" dhcp");
        if (!dnsResult.Success)
            return (false, $"Failed to reset DNS: {dnsResult.Message}");

        return (true, $"Adapter '{adapterName}' has been reset to DHCP.");
    }

    private static async Task<(bool Success, string Message)> RunNetshAsync(string arguments)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var psi = new ProcessStartInfo
        {
            FileName = "netsh",
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            return (false, "Command timed out after 15 seconds.");
        }

        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        var combined = (stdout + stderr).Trim();

        return (process.ExitCode == 0, combined);
    }

    private static async Task<(bool Success, string Message)> VerifyAppliedAsync(NetworkPreset preset)
    {
        await Task.Delay(1500);

        var iface = NetworkInterface.GetAllNetworkInterfaces()
            .FirstOrDefault(ni => ni.Name.Equals(preset.AdapterName, StringComparison.OrdinalIgnoreCase));

        if (iface is null)
            return (false, $"Adapter '{preset.AdapterName}' not found during verification.");

        var found = iface.GetIPProperties().UnicastAddresses
            .Any(a => a.Address.ToString().Equals(preset.IPAddress, StringComparison.OrdinalIgnoreCase));

        if (found)
            return (true,
                $"Configuration applied successfully.\nAdapter: {preset.AdapterName}\nIP: {preset.IPAddress}/{preset.SubnetMask}");

        return (false,
            "Command ran but IP address was not detected on the adapter. " +
            "It may still be applying, or the adapter may have rejected the configuration.");
    }
}
