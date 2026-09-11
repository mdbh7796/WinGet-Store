using System.Diagnostics;
using System.Text;
using WinGetStore.Services.Interfaces;

namespace WinGetStore.Services;

public class ProcessService : IProcessService
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(5);

    public async Task<ProcessResult> RunAsync(
        string fileName,
        IEnumerable<string> arguments,
        CancellationToken cancellationToken = default,
        TimeSpan? timeout = null,
        Action<string>? onStandardOutput = null,
        Action<string>? onStandardError = null)
    {
        var result = new ProcessResult();
        var stdoutBuilder = new StringBuilder();
        var stderrBuilder = new StringBuilder();

        if (cancellationToken.IsCancellationRequested)
        {
            result.Cancelled = true;
            return result;
        }

        var effectiveTimeout = timeout ?? DefaultTimeout;
        if (effectiveTimeout < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must not be negative.");

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            // ArgumentList delegates quoting and escaping to the runtime. Building
            // Arguments manually breaks arguments containing quotes or backslashes.
            foreach (var argument in arguments ?? throw new ArgumentNullException(nameof(arguments)))
                startInfo.ArgumentList.Add(argument);

            using var process = new Process { StartInfo = startInfo };
            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data is null) return;
                lock (stdoutBuilder) stdoutBuilder.AppendLine(e.Data);
                try { onStandardOutput?.Invoke(e.Data); } catch { /* callbacks must not stop capture */ }
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data is null) return;
                lock (stderrBuilder) stderrBuilder.AppendLine(e.Data);
                try { onStandardError?.Invoke(e.Data); } catch { /* callbacks must not stop capture */ }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using var timeoutCts = new CancellationTokenSource(effectiveTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, timeoutCts.Token);

            try
            {
                await process.WaitForExitAsync(linkedCts.Token);
                result.ExitCode = process.ExitCode;
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested &&
                                                     !cancellationToken.IsCancellationRequested)
            {
                result.TimedOut = true;
                Kill(process);
            }
            catch (OperationCanceledException)
            {
                result.Cancelled = true;
                Kill(process);
            }

            // WaitForExit() is required after the async wait so redirected output
            // events queued by the process are delivered before returning.
            if (!process.HasExited)
                Kill(process);
            process.WaitForExit();
        }
        catch (Exception ex)
        {
            result.ExitCode = -1;
            lock (stderrBuilder) stderrBuilder.AppendLine(ex.Message);
        }

        result.StandardOutput = stdoutBuilder.ToString().Trim();
        result.StandardError = stderrBuilder.ToString().Trim();
        return result;
    }

    private static void Kill(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException) { }
        catch (System.ComponentModel.Win32Exception) { }
    }
}
