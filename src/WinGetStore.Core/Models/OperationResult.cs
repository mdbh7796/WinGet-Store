namespace WinGetStore.Models;

public class OperationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Output { get; set; }
    public int ExitCode { get; set; }

    public static OperationResult Succeeded(string? output = null) => new()
    {
        Success = true,
        Output = output,
        ExitCode = 0
    };

    public static OperationResult Failed(string errorMessage, int exitCode = -1) => new()
    {
        Success = false,
        ErrorMessage = errorMessage,
        ExitCode = exitCode
    };
}
