using System;
using System.IO;
using System.Runtime.InteropServices;

namespace MifareOneTool.Core.Services
{
    public static class PlatformInfo
    {
        public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public static bool IsMacOS => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

        /// <summary>
        /// Finds an NFC tool binary: checks bundled path first, then system PATH.
        /// </summary>
        public static string ResolveToolPath(string toolName, string bundledBinDir)
        {
            string suffix = IsWindows ? ".exe" : "";
            string bundled = Path.Combine(bundledBinDir, toolName + suffix);
            if (File.Exists(bundled)) return bundled;

            // Fall back to system PATH (Homebrew on macOS, apt on Linux)
            string brewIntel = $"/usr/local/bin/{toolName}";
            string brewArm = $"/opt/homebrew/bin/{toolName}";
            if (File.Exists(brewArm)) return brewArm;
            if (File.Exists(brewIntel)) return brewIntel;
            string usrBin = $"/usr/bin/{toolName}";
            if (File.Exists(usrBin)) return usrBin;

            return toolName; // rely on PATH resolution at process launch
        }

        public static string DefaultBundledBinDir
        {
            get
            {
                string baseDir = AppContext.BaseDirectory;
                return IsWindows
                    ? Path.Combine(baseDir, "nfc-bin")
                    : Path.Combine(baseDir, "nfc-bin-macos");
            }
        }

        public static string DeviceConnstringPattern =>
            IsWindows
                ? @"pn532_uart:COM\d+(:\d+)?"
                : @"pn532_uart:/dev/tty[\w.]+(:\d+)?";
    }
}
