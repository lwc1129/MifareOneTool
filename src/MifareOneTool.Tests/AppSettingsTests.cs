using MifareOneTool.Core.Services;
using System;
using System.IO;
using Xunit;

namespace MifareOneTool.Tests;

public class AppSettingsTests
{
    [Fact]
    public void NewAppSettings_HasSensibleDefaults()
    {
        var s = new AppSettings();
        Assert.True(s.AutoABN);
        Assert.True(s.WriteCheck);
        Assert.Equal("", s.Language);
    }

    [Fact]
    public void SaveAndLoad_RoundTrip()
    {
        string path = Path.Combine(Path.GetTempPath(), $"settings_{Guid.NewGuid()}.json");
        try
        {
            var original = new AppSettings
            {
                AutoABN = false,
                WriteCheck = false,
                Language = "zh-TW",
                LastKeyFile = "/some/path.mfd"
            };
            original.SaveTo(path);

            var loaded = AppSettings.LoadFrom(path);
            Assert.False(loaded.AutoABN);
            Assert.False(loaded.WriteCheck);
            Assert.Equal("zh-TW", loaded.Language);
            Assert.Equal("/some/path.mfd", loaded.LastKeyFile);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public void LoadFrom_MissingFile_ReturnsDefaults()
    {
        var s = AppSettings.LoadFrom("/nonexistent/path/settings.json");
        Assert.True(s.AutoABN);
        Assert.True(s.WriteCheck);
    }
}
