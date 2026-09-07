namespace WinGetStore.Models;

public class PackageDetails
{
    public string Name { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? License { get; set; }
    public string? HomeUrl { get; set; }
    public string? InstallerUrl { get; set; }
    public string? PackageFamilyName { get; set; }
    public string? Moniker { get; set; }
    public IReadOnlyList<string> Tags { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> InstallerSwitches { get; set; } = Array.Empty<string>();
}
