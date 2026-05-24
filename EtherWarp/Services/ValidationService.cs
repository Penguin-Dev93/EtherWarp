using System.Net;
using System.Net.Sockets;

namespace EtherWarp.Services;

public static class ValidationService
{
    public static bool IsValidIPAddress(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return false;
        return IPAddress.TryParse(input, out var addr)
               && addr.AddressFamily == AddressFamily.InterNetwork;
    }

    public static bool IsValidSubnetMask(string? input)
    {
        if (!IsValidIPAddress(input)) return false;

        var bytes = IPAddress.Parse(input!).GetAddressBytes();
        uint mask = ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16)
                  | ((uint)bytes[2] << 8)  | bytes[3];

        // Valid mask: all 1s followed by all 0s — no 1 after a 0
        uint inverted = ~mask;
        return (inverted & (inverted + 1)) == 0;
    }

    public static bool IsValidOptionalIP(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return true;
        return IsValidIPAddress(input);
    }

    public static (bool IsValid, Dictionary<string, string> Errors) ValidatePreset(
        string name, string adapterName, string ip, string subnet,
        string? gateway, string? primaryDns, string? secondaryDns)
    {
        var errors = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(name))
            errors["Name"] = "Preset name is required.";

        if (string.IsNullOrWhiteSpace(adapterName))
            errors["AdapterName"] = "Adapter selection is required.";

        if (!IsValidIPAddress(ip))
            errors["IPAddress"] = "Enter a valid IPv4 address (e.g. 192.168.1.10).";

        if (!IsValidSubnetMask(subnet))
            errors["SubnetMask"] = "Enter a valid subnet mask (e.g. 255.255.255.0).";

        if (!IsValidOptionalIP(gateway))
            errors["Gateway"] = "Enter a valid IPv4 address or leave blank.";

        if (!IsValidOptionalIP(primaryDns))
            errors["PrimaryDNS"] = "Enter a valid IPv4 address or leave blank.";

        if (!IsValidOptionalIP(secondaryDns))
            errors["SecondaryDNS"] = "Enter a valid IPv4 address or leave blank.";

        if (!string.IsNullOrWhiteSpace(secondaryDns) && string.IsNullOrWhiteSpace(primaryDns))
            errors["SecondaryDNS"] = "A Primary DNS is required when Secondary DNS is set.";

        return (errors.Count == 0, errors);
    }
}
