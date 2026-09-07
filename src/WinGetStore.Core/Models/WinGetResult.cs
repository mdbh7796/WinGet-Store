namespace WinGetStore.Models;

public class WinGetResult
{
    public IReadOnlyList<Package> Packages { get; set; } = Array.Empty<Package>();
    public string? Error { get; set; }
}
