using MifareOneTool.Core.Services;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Xunit;

namespace MifareOneTool.Tests;

public class PlatformInfoTests
{
    [Fact]
    public void ExactlyOnePlatformFlagIsTrue()
    {
        int count = 0;
        if (PlatformInfo.IsWindows) count++;
        if (PlatformInfo.IsMacOS) count++;
        if (PlatformInfo.IsLinux) count++;
        Assert.Equal(1, count);
    }

    [Fact]
    public void DeviceConnstringPattern_IsValidRegex()
    {
        var ex = Record.Exception(() => new Regex(PlatformInfo.DeviceConnstringPattern));
        Assert.Null(ex);
    }

    [Fact]
    public void DeviceConnstringPattern_MatchesWindowsFormat()
    {
        // Windows pattern should match pn532_uart:COMx:115200
        string windowsDevice = "pn532_uart:COM3:115200";
        if (PlatformInfo.IsWindows)
            Assert.Matches(PlatformInfo.DeviceConnstringPattern, windowsDevice);
    }

    [Fact]
    public void DeviceConnstringPattern_MatchesMacFormat()
    {
        string macDevice = "pn532_uart:/dev/tty.usbserial-AB12CD:115200";
        if (PlatformInfo.IsMacOS)
            Assert.Matches(PlatformInfo.DeviceConnstringPattern, macDevice);
    }

    [Fact]
    public void DefaultBundledBinDir_NonEmpty()
    {
        Assert.False(string.IsNullOrEmpty(PlatformInfo.DefaultBundledBinDir));
    }
}
