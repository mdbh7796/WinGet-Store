using WinGetStore.Services;
using WinGetStore.Services.Interfaces;
using Xunit;

namespace WinGetStore.Tests;

public class ProcessServiceTests
{
    private readonly ProcessService _processService = new();

    [Fact]
    public async Task RunAsync_SimpleCommand_ReturnsSuccess()
    {
        var result = await _processService.RunAsync(
            "cmd.exe",
            new[] { "/c", "echo", "Hello" },
            CancellationToken.None,
            TimeSpan.FromSeconds(5));

        Assert.True(result.Success);
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Hello", result.StandardOutput);
    }

    [Fact]
    public async Task RunAsync_NonExistentExecutable_ReturnsFailure()
    {
        var result = await _processService.RunAsync(
            "nonexistent.exe",
            Array.Empty<string>(),
            CancellationToken.None,
            TimeSpan.FromSeconds(5));

        Assert.False(result.Success);
        Assert.NotEqual(0, result.ExitCode);
    }

    [Fact]
    public async Task RunAsync_Cancellation_StopsProcess()
    {
        using var cts = new CancellationTokenSource();

        var task = _processService.RunAsync(
            "ping.exe",
            new[] { "-n", "30", "127.0.0.1" },
            cts.Token,
            TimeSpan.FromSeconds(10));

        await Task.Delay(500);
        cts.Cancel();

        var result = await task;
        Assert.True(result.Cancelled);
    }

    [Fact]
    public async Task RunAsync_Timeout_KillsProcess()
    {
        var result = await _processService.RunAsync(
            "ping.exe",
            new[] { "-n", "30", "127.0.0.1" },
            CancellationToken.None,
            TimeSpan.FromSeconds(1));

        Assert.True(result.TimedOut);
    }

    [Fact]
    public async Task RunAsync_OutputCapture_Works()
    {
        var capturedOutput = new List<string>();

        var result = await _processService.RunAsync(
            "cmd.exe",
            new[] { "/c", "echo Line1 && echo Line2" },
            CancellationToken.None,
            TimeSpan.FromSeconds(5),
            line => capturedOutput.Add(line));

        Assert.True(result.Success);
        Assert.Contains(capturedOutput, l => l.Trim() == "Line1");
        Assert.Contains(capturedOutput, l => l.Trim() == "Line2");
    }

    [Fact]
    public async Task RunAsync_EmptyArguments_Succeeds()
    {
        var result = await _processService.RunAsync(
            "cmd.exe",
            new[] { "/c", "echo" },
            CancellationToken.None,
            TimeSpan.FromSeconds(5));

        Assert.True(result.Success);
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task RunAsync_ArgumentWithSpaces_EscapedCorrectly()
    {
        var result = await _processService.RunAsync(
            "cmd.exe",
            new[] { "/c", "echo", "Hello World" },
            CancellationToken.None,
            TimeSpan.FromSeconds(5));

        Assert.True(result.Success);
        Assert.Contains("Hello World", result.StandardOutput);
    }
}
