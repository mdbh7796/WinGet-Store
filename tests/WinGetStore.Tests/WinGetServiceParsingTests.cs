using WinGetStore.Models;
using WinGetStore.Services;
using Xunit;

namespace WinGetStore.Tests;

public class WinGetServiceParsingTests
{
    [Fact]
    public void ParseTableOutput_SearchResults()
    {
        var output = @"Name                               Id                        Version         Match
------------------------------------------------------------------------------------------
7-Zip                              7zip.7zip                 26.02           Moniker: 7zip
NanaZip                            M2Team.NanaZip            7.0.1832.0      Tag: 7zip";

        var results = WinGetService.ParseTableOutput<Package>(output);

        Assert.Equal(2, results.Count);
        Assert.Equal("7-Zip", results[0].Name);
        Assert.Equal("7zip.7zip", results[0].Id);
        Assert.Equal("26.02", results[0].Version);
        Assert.Equal("NanaZip", results[1].Name);
        Assert.Equal("M2Team.NanaZip", results[1].Id);
    }

    [Fact]
    public void ParseTableOutput_EmptyOutput()
    {
        var results = WinGetService.ParseTableOutput<Package>("");
        Assert.Empty(results);
    }

    [Fact]
    public void ParseTableOutput_WhitespaceOnly()
    {
        var results = WinGetService.ParseTableOutput<Package>("   \n  \n  ");
        Assert.Empty(results);
    }

    [Fact]
    public void ParseTableOutput_NoSeparator()
    {
        var output = @"Name  Id  Version
Some random text";
        var results = WinGetService.ParseTableOutput<Package>(output);
        Assert.Empty(results);
    }

    [Fact]
    public void ParseTableOutput_HeaderOnly()
    {
        var output = @"Name  Id  Version
------------------";
        var results = WinGetService.ParseTableOutput<Package>(output);
        Assert.Empty(results);
    }

    [Fact]
    public void ParseTableOutput_SinglePackage()
    {
        var output = @"Name                               Id                        Version
--------------------------------------------------------------------
Visual Studio Code                 Microsoft.VisualStudioCode  1.105.0";

        var results = WinGetService.ParseTableOutput<Package>(output);

        Assert.Single(results);
        Assert.Equal("Visual Studio Code", results[0].Name);
        Assert.Equal("Microsoft.VisualStudioCode", results[0].Id);
        Assert.Equal("1.105.0", results[0].Version);
    }

    [Fact]
    public void ParseTableOutput_PackageWithAvailableVersion()
    {
        var output = @"Name                               Id                        Version              Available
-----------------------------------------------------------------------------------------------
Firefox                            Mozilla.Firefox           141.0                 142.0
7-Zip                              7zip.7zip                 26.02";

        var results = WinGetService.ParseTableOutput<Package>(output);

        Assert.Equal(2, results.Count);
        Assert.Equal("142.0", results[0].AvailableVersion);
        Assert.Null(results[1].AvailableVersion);
    }

    [Fact]
    public void ParseTableOutput_EmptyLinesIgnored()
    {
        var output = @"Name                               Id                        Version
------------------------------------------------------------------------------------------

7-Zip                              7zip.7zip                 26.02

";

        var results = WinGetService.ParseTableOutput<Package>(output);
        Assert.Single(results);
        Assert.Equal("7-Zip", results[0].Name);
    }

    [Fact]
    public void ParseTableOutput_UpdateResults()
    {
        var output = @"Name                               Id                        Version              Available
-----------------------------------------------------------------------------------------------
Firefox                            Mozilla.Firefox           141.0                 142.0
7-Zip                              7zip.7zip                 25.00                25.01";

        var results = WinGetService.ParseTableOutput<PackageUpdate>(output);

        Assert.Equal(2, results.Count);
        Assert.Equal("Firefox", results[0].Name);
        Assert.Equal("Mozilla.Firefox", results[0].Id);
        Assert.Equal("141.0", results[0].CurrentVersion);
        Assert.Equal("142.0", results[0].AvailableVersion);
    }

    [Fact]
    public void ParseTableOutput_WithSummaryLine()
    {
        var output = @"Name                               Id                        Version              Available
-----------------------------------------------------------------------------------------------
Firefox                            Mozilla.Firefox           141.0                 142.0
3 upgrades available.";

        var results = WinGetService.ParseTableOutput<PackageUpdate>(output);
        Assert.Equal(2, results.Count);
        Assert.Equal("Firefox", results[0].Name);
        Assert.Equal("3 upgrades available.", results[1].Name);
    }

    [Fact]
    public void ParseTableOutput_TrimWhitespace()
    {
        var output = @"Name          Id               Version
-----------------------------------------
  7-Zip       7zip.7zip        26.02  ";

        var results = WinGetService.ParseTableOutput<Package>(output);
        Assert.Single(results);
        Assert.Equal("7-Zip", results[0].Name);
        Assert.Equal("7zip.7zip", results[0].Id);
    }

    [Fact]
    public void ParseTableOutput_LongNames()
    {
        var output = @"Name                                                              Id                                    Version
-------------------------------------------------------------------------------------------
Visual Studio Code                                                Microsoft.VisualStudioCode            1.105.0
Advanced Archive Password Recovery                                Elcomsoft.ArchivePassword             4.66.266.6965";

        var results = WinGetService.ParseTableOutput<Package>(output);
        Assert.Equal(2, results.Count);
        Assert.Equal("Visual Studio Code", results[0].Name);
        Assert.Equal("Advanced Archive Password Recovery", results[1].Name);
    }
}
