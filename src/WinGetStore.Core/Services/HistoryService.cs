using System.Text.Json;
using WinGetStore.Models;

namespace WinGetStore.Services;

public class HistoryService
{
    private static readonly string HistoryFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WinGetStore",
        "history.json");

    private List<PackageHistoryEntry> _entries = new();

    public IReadOnlyList<PackageHistoryEntry> Entries => _entries.AsReadOnly();

    public HistoryService()
    {
        Load();
    }

    public void RecordInstall(string packageId, string name, string version, bool success, string? error = null)
    {
        _entries.Insert(0, new PackageHistoryEntry
        {
            Name = name,
            Id = packageId,
            Version = version,
            Operation = "Install",
            Timestamp = DateTime.Now,
            Success = success,
            ErrorMessage = error
        });
        Save();
    }

    public void RecordUninstall(string packageId, string name, string version, bool success, string? error = null)
    {
        _entries.Insert(0, new PackageHistoryEntry
        {
            Name = name,
            Id = packageId,
            Version = version,
            Operation = "Uninstall",
            Timestamp = DateTime.Now,
            Success = success,
            ErrorMessage = error
        });
        Save();
    }

    public void RecordUpdate(string packageId, string name, string version, bool success, string? error = null)
    {
        _entries.Insert(0, new PackageHistoryEntry
        {
            Name = name,
            Id = packageId,
            Version = version,
            Operation = "Update",
            Timestamp = DateTime.Now,
            Success = success,
            ErrorMessage = error
        });
        Save();
    }

    public void Clear()
    {
        _entries.Clear();
        Save();
    }

    private void Load()
    {
        try
        {
            if (File.Exists(HistoryFilePath))
            {
                var json = File.ReadAllText(HistoryFilePath);
                _entries = JsonSerializer.Deserialize<List<PackageHistoryEntry>>(json) ?? new();
            }
        }
        catch
        {
            _entries = new();
        }
    }

    private void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(HistoryFilePath);
            if (dir != null && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(_entries, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(HistoryFilePath, json);
        }
        catch
        {
            // Silently fail on write errors
        }
    }
}
