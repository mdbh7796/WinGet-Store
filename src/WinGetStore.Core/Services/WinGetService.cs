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
                result.ErrorMessage = GetFailureMessage("WinGet detection", processResult);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
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
                Error = GetFailureMessage("Search", processResult)
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

        EnsureSuccess("List installed packages", processResult, cancellationToken);

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

        EnsureSuccess("Find package updates", processResult, cancellationToken);

        return ParseUpgradeOutput(processResult.StandardOutput);
    }

    public async Task<OperationResult> InstallPackageAsync(
        string packageId,
        string? source = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(packageId))
            return OperationResult.Failed("Package ID cannot be empty");

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
        if (string.IsNullOrWhiteSpace(packageId))
            return OperationResult.Failed("Package ID cannot be empty");

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
        if (string.IsNullOrWhiteSpace(packageId))
            return OperationResult.Failed("Package ID cannot be empty");

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
        if (string.IsNullOrWhiteSpace(packageId))
            return null;

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

        EnsureSuccess("Show package details", processResult, cancellationToken);

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
            return OperationResult.Failed("WinGet operation timed out.");

        if (processResult.Cancelled)
            return OperationResult.Failed("WinGet operation was cancelled.");

        if (processResult.ExitCode == 0)
            return OperationResult.Succeeded(processResult.StandardOutput);

        var errorMessage = GetFailureMessage("WinGet operation", processResult);

        return OperationResult.Failed(errorMessage, processResult.ExitCode);
    }

    private static void EnsureSuccess(
        string operation,
        ProcessResult processResult,
        CancellationToken cancellationToken)
    {
        if (processResult.Success)
            return;

        if (processResult.Cancelled)
            throw new OperationCanceledException(cancellationToken);

        throw new WinGetServiceException(operation, GetFailureMessage(operation, processResult),
            processResult.ExitCode, processResult.TimedOut);
    }

    private static string GetFailureMessage(string operation, ProcessResult processResult)
    {
        if (processResult.TimedOut)
            return $"{operation} timed out.";
        if (processResult.Cancelled)
            return $"{operation} was cancelled.";
        if (!string.IsNullOrWhiteSpace(processResult.StandardError))
            return processResult.StandardError.Trim();
        if (!string.IsNullOrWhiteSpace(processResult.StandardOutput))
            return processResult.StandardOutput.Trim();
        return $"{operation} failed with exit code {processResult.ExitCode}.";
    }

    internal static IReadOnlyList<T> ParseTableOutput<T>(string output) where T : class, new()
    {
        if (string.IsNullOrWhiteSpace(output))
            return Array.Empty<T>();

        var lines = output.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2)
            return Array.Empty<T>();

        var separatorIndex = -1;

        for (int i = 1; i < lines.Length; i++)
        {
            if (IsSeparator(lines[i]))
            {
                separatorIndex = i;
                break;
            }
        }

        if (separatorIndex < 0)
            return Array.Empty<T>();

        // WinGet can print source notices before the table. The header is the
        // line immediately preceding the separator, not necessarily line zero.
        var headerLine = lines[separatorIndex - 1];
        var columns = ParseColumnPositions(headerLine);
        var results = new List<T>();

        for (int i = separatorIndex + 1; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
                continue;
            if (IsSummaryLine(line))
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

            if (type.GetProperty("Name")?.GetValue(item) is string name &&
                !string.IsNullOrWhiteSpace(name) &&
                IsValidTableRecord(item))
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

        var lines = output.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var results = new List<PackageUpdate>();

        int separatorIndex = -1;
        for (int i = 0; i < lines.Length; i++)
        {
            if (IsSeparator(lines[i]))
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
            if (IsSummaryLine(line))
                continue;

            var values = ExtractColumnValues(line, columns);
            if (values.Count >= 4 &&
                !string.IsNullOrWhiteSpace(values[1]) &&
                !string.IsNullOrWhiteSpace(values[2]) &&
                !string.IsNullOrWhiteSpace(values[3]))
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
        var lines = output.Replace("\r\n", "\n").Split('\n');
        string? currentKey = null;
        var tags = new List<string>();

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed))
                continue;

            var colonIndex = trimmed.IndexOf(':');
            if (colonIndex <= 0)
            {
                if (currentKey == "Tags" && !trimmed.StartsWith("-", StringComparison.Ordinal))
                    tags.Add(trimmed);
                continue;
            }

            var key = trimmed[..colonIndex].Trim();
            var value = trimmed[(colonIndex + 1)..].Trim();
            currentKey = key;

            switch (key)
            {
                case "Name":
                    details.Name = value;
                    break;
                case "Version":
                    details.Version = value;
                    break;
                case "Id":
                    details.Id = value;
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
                case "Source":
                    details.Source = value;
                    break;
                case "Package Family Name":
                    details.PackageFamilyName = value;
                    break;
                case "License":
                    details.License = value;
                    break;
                case "Installer Url":
                    details.InstallerUrl = value;
                    break;
                case "Tags":
                    if (!string.IsNullOrWhiteSpace(value))
                        tags.AddRange(value.Split(',', StringSplitOptions.TrimEntries |
                            StringSplitOptions.RemoveEmptyEntries));
                    break;
                case "Installer Type":
                    // Could store if needed
                    break;
            }
        }

        details.Tags = tags.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        return details;
    }

    private static bool IsSeparator(string line) =>
        Regex.IsMatch(line.Trim(), @"^-{3,}$");

    private static bool IsSummaryLine(string line) =>
        Regex.IsMatch(line.Trim(), @"^\d+\s+(upgrade|package)s?\s+available\.?$",
            RegexOptions.IgnoreCase);

    private static bool IsValidTableRecord<T>(T item) where T : class
    {
        if (item is Package package)
            return !string.IsNullOrWhiteSpace(package.Id) &&
                   !string.IsNullOrWhiteSpace(package.Version);
        if (item is PackageUpdate update)
            return !string.IsNullOrWhiteSpace(update.Id) &&
                   !string.IsNullOrWhiteSpace(update.CurrentVersion) &&
                   !string.IsNullOrWhiteSpace(update.AvailableVersion);
        return true;
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
