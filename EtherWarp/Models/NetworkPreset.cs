namespace EtherWarp.Models;

public class NetworkPreset
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string IPAddress { get; set; } = string.Empty;
    public string SubnetMask { get; set; } = string.Empty;
    public string? Gateway { get; set; }
    public string? PrimaryDNS { get; set; }
    public string? SecondaryDNS { get; set; }

    public NetworkPreset()
    {
        Id = Guid.NewGuid();
    }

    public NetworkPreset(NetworkPreset source)
    {
        Id           = source.Id;
        Name         = source.Name;
        IPAddress    = source.IPAddress;
        SubnetMask   = source.SubnetMask;
        Gateway      = source.Gateway;
        PrimaryDNS   = source.PrimaryDNS;
        SecondaryDNS = source.SecondaryDNS;
    }
}
