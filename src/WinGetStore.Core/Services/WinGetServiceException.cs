namespace WinGetStore.Services;

/// <summary>Describes a failed WinGet query whose list-returning API has no result envelope.</summary>
public sealed class WinGetServiceException : Exception
{
    public WinGetServiceException(
        string operation,
        string message,
        int exitCode,
        bool timedOut = false)
        : base(message)
    {
        Operation = operation;
        ExitCode = exitCode;
        TimedOut = timedOut;
    }

    public string Operation { get; }
    public int ExitCode { get; }
    public bool TimedOut { get; }
}
