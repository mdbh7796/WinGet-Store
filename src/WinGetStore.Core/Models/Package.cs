namespace WinGetStore.Models;

public class Package
{
    public string Name { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string? AvailableVersion { get; set; }
    public string? PackageFamilyName { get; set; }
}
