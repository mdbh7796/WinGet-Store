using System.Text.RegularExpressions;
using WinGetStore.Models;
using WinGetStore.Services.Interfaces;

namespace WinGetStore.Services;

public class WinGetService : IWinGetService
{
    private readonly IProcessService _processService;

    private const string WinGetExe = "winget.exe";

    public WinGetService(IProcessService processService)
    {
        _processService = processService;
    }

    public async Task<WinGetVersionInfo> DetectWinGetAsync(CancellationToken cancellationToken = default)
    {
        var result = new WinGetVersionInfo();

        try
        {
            var processResult = await _processService.RunAsync(
                WinGetExe,
                new[] { "--version" },
                cancellationToken,
                TimeSpan.FromSeconds(10));

            if (processResult.Success && !string.IsNullOrWhiteSpace(processResult.StandardOutput))
            {
                var versionText = processResult.StandardOutput.Trim();
                var versionMatch = Regex.Match(versionText, @"v?([\d.]+)");
                if (versionMatch.Success)
                {
                    result.Version = versionMatch.Groups[1].Value;
                }
                else
                {
                    result.Version = versionText;
                }
                result.IsAvailable = true;
            }
            else
            {
                result.ErrorMessage = !string.IsNullOrWhiteSpace(processResult.StandardError)
                    ? processResult.StandardError
                    : "WinGet returned non-zero exit code";
            }
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"Failed to detect WinGet: {ex.Message}";
        }

        return result;
    }

    public async Task<WinGetResult> SearchPackagesAsync(
        string query,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new WinGetResult { Error = "Search query cannot be empty" };

        var arguments = new List<string>
        {
            "search", query,
            "--disable-interactivity",
            "--accept-source-agreements"
        };

        var processResult = await _processService.RunAsync(
            WinGetExe,
            arguments,
            cancellationToken,
            TimeSpan.FromSeconds(30));

        if (!processResult.Success)
        {
            return new WinGetResult
            {
                Error = !string.IsNullOrWhiteSpace(processResult.StandardError)
                    ? processResult.StandardError
                    : $"Search failed with exit code {processResult.ExitCode}"
            };
        }

        var packages = ParseTableOutput<Package>(processResult.StandardOutput);
        return new WinGetResult { Packages = packages };
    }

    public async Task<IReadOnlyList<Package>> GetInstalledPackagesAsync(
        CancellationToken cancellationToken)
    {
        var arguments = new List<string>
        {
            "list",
            "--disable-interactivity",
            "--accept-source-agreements"
        };

        var processResult = await _processService.RunAsync(
            WinGetExe,
            arguments,
            cancellationToken,
            TimeSpan.FromSeconds(60));

        if (!processResult.Success)
            return Array.Empty<Package>();

        return ParseTableOutput<Package>(processResult.StandardOutput);
    }

    public async Task<IReadOnlyList<PackageUpdate>> GetAvailableUpdatesAsync(
        CancellationToken cancellationToken)
    {
        var arguments = new List<string>
        {
            "upgrade",
            "--disable-interactivity",
            "--accept-source-agreements"
        };

        var processResult = await _processService.RunAsync(
            WinGetExe,
            arguments,
            cancellationToken,
            TimeSpan.FromSeconds(60));

        if (!processResult.Success)
            return Array.Empty<PackageUpdate>();

        return ParseUpgradeOutput(processResult.StandardOutput);
    }

    public async Task<OperationResult> InstallPackageAsync(
        string packageId,
        string? source = null,
        CancellationToken cancellationToken = default)
    {
        var arguments = new List<string>
        {
            "install", packageId,
            "--disable-interactivity",
            "--accept-package-agreements",
            "--accept-source-agreements"
        };

        if (!string.IsNullOrWhiteSpace(source))
        {
            arguments.Add("--source");
            arguments.Add(source);
        }

        return await RunWinGetOperationAsync(arguments, cancellationToken);
    }

    public async Task<OperationResult> UpdatePackageAsync(
        string packageId,
        CancellationToken cancellationToken = default)
    {
        var arguments = new List<string>
        {
            "upgrade", packageId,
            "--disable-interactivity",
            "--accept-package-agreements",
            "--accept-source-agreements"
        };

        return await RunWinGetOperationAsync(arguments, cancellationToken);
    }

    public async Task<OperationResult> UpdateAllPackagesAsync(
        CancellationToken cancellationToken = default)
    {
        var arguments = new List<string>
        {
            "upgrade",
            "--all",
            "--disable-interactivity",
            "--accept-package-agreements",
            "--accept-source-agreements"
        };

        return await RunWinGetOperationAsync(arguments, cancellationToken, TimeSpan.FromMinutes(30));
    }

    public async Task<OperationResult> UninstallPackageAsync(
        string packageId,
        CancellationToken cancellationToken = default)
    {
        var arguments = new List<string>
        {
            "uninstall", packageId,
            "--disable-interactivity",
            "--accept-source-agreements"
        };

        return await RunWinGetOperationAsync(arguments, cancellationToken);
    }

    public async Task<PackageDetails?> GetPackageDetailsAsync(
        string packageId,
        string? source = null,
        CancellationToken cancellationToken = default)
    {
        var arguments = new List<string>
        {
            "show", packageId,
            "--disable-interactivity",
            "--accept-source-agreements"
        };

        if (!string.IsNullOrWhiteSpace(source))
        {
            arguments.Add("--source");
            arguments.Add(source);
        }

        var processResult = await _processService.RunAsync(
            WinGetExe,
            arguments,
            cancellationToken,
            TimeSpan.FromSeconds(30));

        if (!processResult.Success)
            return null;

        return ParseShowOutput(processResult.StandardOutput, packageId);
    }

    private async Task<OperationResult> RunWinGetOperationAsync(
        List<string> arguments,
        CancellationToken cancellationToken,
        TimeSpan? timeout = null)
    {
        var processResult = await _processService.RunAsync(
            WinGetExe,
            arguments,
            cancellationToken,
            timeout ?? TimeSpan.FromMinutes(5));

        if (processResult.TimedOut)
            return OperationResult.Failed("Operation timed out");

        if (processResult.Cancelled)
            return OperationResult.Failed("Operation was cancelled");

        if (processResult.ExitCode == 0)
            return OperationResult.Succeeded(processResult.StandardOutput);

        var errorMessage = !string.IsNullOrWhiteSpace(processResult.StandardError)
            ? processResult.StandardError
            : processResult.StandardOutput;

        return OperationResult.Failed(errorMessage, processResult.ExitCode);
    }

    internal static IReadOnlyList<T> ParseTableOutput<T>(string output) where T : class, new()
    {
        if (string.IsNullOrWhiteSpace(output))
            return Array.Empty<T>();

        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2)
            return Array.Empty<T>();

        var headerLine = lines[0];
        var separatorIndex = -1;

        for (int i = 1; i < lines.Length; i++)
        {
            if (lines[i].TrimStart().StartsWith("---"))
            {
                separatorIndex = i;
                break;
            }
        }

        if (separatorIndex < 0)
            return Array.Empty<T>();

        var columns = ParseColumnPositions(headerLine);
        var results = new List<T>();

        for (int i = separatorIndex + 1; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var values = ExtractColumnValues(line, columns);

            var item = new T();
            var type = typeof(T);

            if (typeof(T) == typeof(Package))
            {
                var pkg = item as Package;
                if (pkg != null && values.Count >= 3)
                {
                    pkg.Name = values[0].Trim();
                    pkg.Id = values[1].Trim();
                    pkg.Version = values[2].Trim();
                    if (values.Count >= 4)
                        pkg.AvailableVersion = string.IsNullOrWhiteSpace(values[3].Trim()) ? null : values[3].Trim();
                }
            }
            else if (typeof(T) == typeof(PackageUpdate))
            {
                var upd = item as PackageUpdate;
                if (upd != null && values.Count >= 3)
                {
                    upd.Name = values[0].Trim();
                    upd.Id = values[1].Trim();
                    upd.CurrentVersion = values[2].Trim();
                    if (values.Count >= 4)
                        upd.AvailableVersion = values[3].Trim();
                }
            }

            if (type.GetProperty("Name")?.GetValue(item) is string name && !string.IsNullOrWhiteSpace(name))
            {
                results.Add(item);
            }
        }

        return results;
    }

    private static List<PackageUpdate> ParseUpgradeOutput(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
            return new List<PackageUpdate>();

        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var results = new List<PackageUpdate>();

        int separatorIndex = -1;
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].TrimStart().StartsWith("---"))
            {
                separatorIndex = i;
                break;
            }
        }

        if (separatorIndex < 0)
            return results;

        var headerLine = lines[separatorIndex - 1];
        var columns = ParseColumnPositions(headerLine);

        for (int i = separatorIndex + 1; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // Skip summary lines like "3 upgrades available."
            if (line.Contains("upgrade") && line.Contains("available"))
                continue;

            var values = ExtractColumnValues(line, columns);
            if (values.Count >= 4)
            {
                results.Add(new PackageUpdate
                {
                    Name = values[0].Trim(),
                    Id = values[1].Trim(),
                    CurrentVersion = values[2].Trim(),
                    AvailableVersion = values[3].Trim()
                });
            }
        }

        return results;
    }

    private static PackageDetails? ParseShowOutput(string output, string packageId)
    {
        if (string.IsNullOrWhiteSpace(output))
            return null;

        var details = new PackageDetails { Id = packageId };
        var lines = output.Split('\n');

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed))
                continue;

            var colonIndex = trimmed.IndexOf(':');
            if (colonIndex <= 0)
                continue;

            var key = trimmed[..colonIndex].Trim();
            var value = trimmed[(colonIndex + 1)..].Trim();

            switch (key)
            {
                case "Name":
                    details.Name = value;
                    break;
                case "Version":
                    details.Version = value;
                    break;
                case "Publisher":
                    details.Publisher = value;
                    break;
                case "Publisher Url":
                case "Publisher Support Url":
                    // Could store these separately if needed
                    break;
                case "Author":
                    // Could store if needed
                    break;
                case "Moniker":
                    details.Moniker = value;
                    break;
                case "Description":
                    details.Description = value;
                    break;
                case "Homepage":
                    details.HomeUrl = value;
                    break;
                case "License":
                    details.License = value;
                    break;
                case "Installer Url":
                    details.InstallerUrl = value;
                    break;
                case "Installer Type":
                    // Could store if needed
                    break;
            }
        }

        // Parse tags
        var tagLines = output.Split('\n');
        bool inTags = false;
        var tags = new List<string>();
        foreach (var line in tagLines)
        {
            var trimmed = line.Trim();
            if (trimmed == "Tags:")
            {
                inTags = true;
                continue;
            }
            if (inTags && !string.IsNullOrEmpty(trimmed) && !trimmed.Contains(':'))
            {
                tags.Add(trimmed);
            }
            else if (inTags && trimmed.Contains(':'))
            {
                inTags = false;
            }
        }
        details.Tags = tags;

        return details;
    }

    private static List<int> ParseColumnPositions(string headerLine)
    {
        var positions = new List<int>();
        bool inWord = false;

        for (int i = 0; i < headerLine.Length; i++)
        {
            if (!char.IsWhiteSpace(headerLine[i]))
            {
                if (!inWord)
                {
                    positions.Add(i);
                    inWord = true;
                }
            }
            else
            {
                inWord = false;
            }
        }

        return positions;
    }

    private static List<string> ExtractColumnValues(string line, List<int> columnStarts)
    {
        var values = new List<string>();

        for (int i = 0; i < columnStarts.Count; i++)
        {
            int start = columnStarts[i];
            int end = i + 1 < columnStarts.Count
                ? columnStarts[i + 1]
                : line.Length;

            if (start >= line.Length)
            {
                values.Add(string.Empty);
                continue;
            }

            if (end > line.Length)
                end = line.Length;

            values.Add(line[start..end]);
        }

        return values;
    }
}
