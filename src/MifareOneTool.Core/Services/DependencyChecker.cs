using System.Collections.Generic;
using System.IO;

namespace MifareOneTool.Core.Services
{
    public record ToolStatus(string Name, string ResolvedPath, bool Found);

    public static class DependencyChecker
    {
        private static readonly string[] RequiredTools =
        {
            "nfc-scan-device",
            "nfc-mfclassic",
            "nfc-list",
            "mfoc",
            "nfc-mfsetuid",
        };

        /// <summary>
        /// Checks each required NFC tool and returns its resolved path + found status.
        /// </summary>
        public static List<ToolStatus> Check()
        {
            var runner = new DesktopNfcToolRunner();
            var results = new List<ToolStatus>();
            foreach (var tool in RequiredTools)
            {
                string path = runner.ResolveTool(tool);
                results.Add(new ToolStatus(tool, path, File.Exists(path)));
            }
            return results;
        }

        /// <summary>
        /// Returns a human-readable install hint when tools are missing.
        /// </summary>
        public static string InstallHint()
        {
            if (PlatformInfo.IsWindows)
                return "Place nfc-scan-device.exe, nfc-mfclassic.exe, mfoc.exe, nfc-list.exe, " +
                       "and nfc-mfsetuid.exe in the nfc-bin\\ folder next to this application.\n" +
                       "Download from: https://github.com/nfc-tools/libnfc/releases";

            if (PlatformInfo.IsMacOS)
                return "Install libnfc tools via Homebrew:\n" +
                       "  brew install libnfc mfoc\n\n" +
                       "Apple Silicon: /opt/homebrew/bin/\n" +
                       "Intel Mac:     /usr/local/bin/";

            return "Install libnfc tools:\n" +
                   "  sudo apt install libnfc-bin mfoc   # Debian/Ubuntu\n" +
                   "  sudo dnf install libnfc mfoc        # Fedora";
        }
    }
}
