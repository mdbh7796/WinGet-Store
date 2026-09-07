using WinGetStore.Models;
using WinGetStore.Services.Interfaces;
using Xunit;

namespace WinGetStore.Tests;

public class BasicTests
{
    [Fact]
    public void ThemeType_HasExpectedValues()
    {
        var themeType = typeof(ThemeType);
        Assert.NotNull(themeType);

        var names = Enum.GetNames(themeType);
        Assert.Equal(3, names.Length);
        Assert.Contains("System", names);
        Assert.Contains("Light", names);
        Assert.Contains("Dark", names);
    }

    [Fact]
    public void Package_HasExpectedProperties()
    {
        var pkg = new Package
        {
            Name = "Test",
            Id = "test.id",
            Version = "1.0",
            Publisher = "Test Publisher"
        };

        Assert.Equal("Test", pkg.Name);
        Assert.Equal("test.id", pkg.Id);
        Assert.Equal("1.0", pkg.Version);
    }

    [Fact]
    public void OperationResult_Succeeded_HasCorrectState()
    {
        var result = OperationResult.Succeeded("output");
        Assert.True(result.Success);
        Assert.Equal("output", result.Output);
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public void OperationResult_Failed_HasCorrectState()
    {
        var result = OperationResult.Failed("error", 1);
        Assert.False(result.Success);
        Assert.Equal("error", result.ErrorMessage);
        Assert.Equal(1, result.ExitCode);
    }
}
