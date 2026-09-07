using WinGetStore.Models;
using Xunit;

namespace WinGetStore.Tests;

public class ModelTests
{
    [Fact]
    public void Package_DefaultValues_AreEmpty()
    {
        var pkg = new Package();
        Assert.Equal(string.Empty, pkg.Name);
        Assert.Equal(string.Empty, pkg.Id);
        Assert.Equal(string.Empty, pkg.Version);
        Assert.Null(pkg.AvailableVersion);
    }

    [Fact]
    public void PackageUpdate_DefaultValues_AreEmpty()
    {
        var update = new PackageUpdate();
        Assert.Equal(string.Empty, update.Name);
        Assert.Equal(string.Empty, update.Id);
        Assert.Equal(string.Empty, update.CurrentVersion);
        Assert.Equal(string.Empty, update.AvailableVersion);
    }

    [Fact]
    public void PackageDetails_DefaultValues_AreEmpty()
    {
        var details = new PackageDetails();
        Assert.Equal(string.Empty, details.Name);
        Assert.Equal(string.Empty, details.Id);
        Assert.Null(details.Description);
        Assert.Null(details.License);
        Assert.Empty(details.Tags);
    }

    [Fact]
    public void WinGetResult_DefaultValues()
    {
        var result = new WinGetResult();
        Assert.Empty(result.Packages);
        Assert.Null(result.Error);
    }

    [Fact]
    public void WinGetVersionInfo_DefaultValues()
    {
        var info = new WinGetVersionInfo();
        Assert.False(info.IsAvailable);
        Assert.Null(info.Version);
        Assert.Null(info.ErrorMessage);
    }

    [Fact]
    public void OperationResult_Succeeded_HasCorrectValues()
    {
        var result = OperationResult.Succeeded("output text");
        Assert.True(result.Success);
        Assert.Equal("output text", result.Output);
        Assert.Equal(0, result.ExitCode);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void OperationResult_Failed_HasCorrectValues()
    {
        var result = OperationResult.Failed("something broke", 42);
        Assert.False(result.Success);
        Assert.Equal("something broke", result.ErrorMessage);
        Assert.Equal(42, result.ExitCode);
    }

    [Fact]
    public void OperationResult_Failed_DefaultExitCode()
    {
        var result = OperationResult.Failed("error");
        Assert.Equal(-1, result.ExitCode);
    }

    [Fact]
    public void PackageHistoryEntry_DefaultValues()
    {
        var entry = new PackageHistoryEntry();
        Assert.Equal(string.Empty, entry.Name);
        Assert.Equal(string.Empty, entry.Operation);
        Assert.False(entry.Success);
    }

    [Fact]
    public void WinGetResult_WithPackages()
    {
        var packages = new List<Package>
        {
            new() { Name = "A", Id = "a.id", Version = "1.0" },
            new() { Name = "B", Id = "b.id", Version = "2.0" }
        };

        var result = new WinGetResult { Packages = packages };
        Assert.Equal(2, result.Packages.Count);
    }
}
