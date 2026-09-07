using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinGetStore.Models;
using WinGetStore.Services;

namespace WinGetStore.ViewModels;

public partial class HistoryPageViewModel : ObservableObject
{
    private readonly HistoryService _historyService;
    private List<PackageHistoryEntry> _allEntries = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private int _entryCount;

    public ObservableCollection<PackageHistoryEntry> Entries { get; } = new();

    public HistoryPageViewModel(HistoryService historyService)
    {
        _historyService = historyService;
    }

    [RelayCommand]
    private void LoadHistory()
    {
        _allEntries = _historyService.Entries.ToList();
        EntryCount = _allEntries.Count;
        ApplyFilter();
    }

    [RelayCommand]
    private void ApplyFilter()
    {
        var filtered = _allEntries.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var query = SearchText.Trim();
            filtered = filtered.Where(e =>
                e.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                e.Id.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        Entries.Clear();
        foreach (var entry in filtered)
        {
            Entries.Add(entry);
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    [RelayCommand]
    private void ClearHistory()
    {
        _historyService.Clear();
        _allEntries.Clear();
        Entries.Clear();
        EntryCount = 0;
    }
}
