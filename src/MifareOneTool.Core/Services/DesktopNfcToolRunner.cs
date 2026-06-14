using System.IO;

namespace MifareOneTool.Core.Services
{
    public class DesktopNfcToolRunner : INfcToolRunner
    {
        private readonly string _binPath;

        public DesktopNfcToolRunner(string? binPath = null)
        {
            _binPath = binPath ?? PlatformInfo.DefaultBundledBinDir;
        }

        public string BinPath => _binPath;

        public string ExeSuffix => PlatformInfo.IsWindows ? ".exe" : "";

        public string ShellExe => PlatformInfo.IsWindows ? "cmd.exe" : "/bin/bash";

        public string ShellArgs => PlatformInfo.IsWindows ? "/c" : "-c";

        public string DeviceConnstringPattern => PlatformInfo.DeviceConnstringPattern;

        public bool IsWindows => PlatformInfo.IsWindows;

        public string ResolveTool(string toolName)
        {
            return PlatformInfo.ResolveToolPath(toolName, _binPath);
        }
    }
}
