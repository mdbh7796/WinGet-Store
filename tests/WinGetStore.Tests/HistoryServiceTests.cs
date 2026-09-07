using WinGetStore.Models;
using WinGetStore.Services;
using Xunit;

namespace WinGetStore.Tests;

public class HistoryServiceTests
{
    [Fact]
    public void RecordInstall_AddsEntry()
    {
        var service = new HistoryService();
        var beforeCount = service.Entries.Count;

        service.RecordInstall("test_install_" + Guid.NewGuid(), "Test App", "1.0", true);

        Assert.True(service.Entries.Count > beforeCount);
        var last = service.Entries[0];
        Assert.Equal("Install", last.Operation);
        Assert.Equal("Test App", last.Name);
        Assert.True(last.Success);
    }

    [Fact]
    public void RecordUninstall_AddsEntry()
    {
        var service = new HistoryService();
        var beforeCount = service.Entries.Count;

        service.RecordUninstall("test_uninstall_" + Guid.NewGuid(), "Test App", "1.0", true);

        Assert.True(service.Entries.Count > beforeCount);
        Assert.Equal("Uninstall", service.Entries[0].Operation);
    }

    [Fact]
    public void RecordUpdate_AddsEntry()
    {
        var service = new HistoryService();
        var beforeCount = service.Entries.Count;

        service.RecordUpdate("test_update_" + Guid.NewGuid(), "Test App", "2.0", true);

        Assert.True(service.Entries.Count > beforeCount);
        Assert.Equal("Update", service.Entries[0].Operation);
        Assert.Equal("2.0", service.Entries[0].Version);
    }

    [Fact]
    public void RecordFailure_HasError()
    {
        var service = new HistoryService();

        service.RecordInstall("test_fail_" + Guid.NewGuid(), "Test App", "1.0", false, "Access denied");

        Assert.False(service.Entries[0].Success);
        Assert.Equal("Access denied", service.Entries[0].ErrorMessage);
    }

    [Fact]
    public void NewEntriesAppearFirst()
    {
        var service = new HistoryService();
        var id1 = "first_" + Guid.NewGuid();
        var id2 = "second_" + Guid.NewGuid();

        service.RecordInstall(id1, "First", "1.0", true);
        service.RecordInstall(id2, "Second", "2.0", true);

        Assert.Equal(id2, service.Entries[0].Id);
        Assert.Equal(id1, service.Entries[1].Id);
    }

    [Fact]
    public void Entries_AreReadOnly()
    {
        var service = new HistoryService();
        Assert.IsAssignableFrom<IReadOnlyList<PackageHistoryEntry>>(service.Entries);
    }
}
