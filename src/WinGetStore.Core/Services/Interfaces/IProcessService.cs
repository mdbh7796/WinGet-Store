using System.Diagnostics;

namespace WinGetStore.Services.Interfaces;

public class ProcessResult
{
    public int ExitCode { get; set; }
    public string StandardOutput { get; set; } = string.Empty;
    public string StandardError { get; set; } = string.Empty;
    public bool TimedOut { get; set; }
    public bool Cancelled { get; set; }

    public bool Success => ExitCode == 0 && !TimedOut && !Cancelled;
}

public interface IProcessService
{
    Task<ProcessResult> RunAsync(
        string fileName,
        IEnumerable<string> arguments,
        CancellationToken cancellationToken = default,
        TimeSpan? timeout = null,
        Action<string>? onStandardOutput = null,
        Action<string>? onStandardError = null);
}
