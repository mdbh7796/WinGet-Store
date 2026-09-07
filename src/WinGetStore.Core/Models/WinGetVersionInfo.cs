namespace WinGetStore.Models;

public class WinGetVersionInfo
{
    public bool IsAvailable { get; set; }
    public string? Version { get; set; }
    public string? Path { get; set; }
    public string? ErrorMessage { get; set; }
}
