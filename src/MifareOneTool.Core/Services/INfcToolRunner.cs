namespace MifareOneTool.Core.Services
{
    public interface INfcToolRunner
    {
        string BinPath { get; }
        string ExeSuffix { get; }
        string ShellExe { get; }
        string ShellArgs { get; }
        string DeviceConnstringPattern { get; }
        bool IsWindows { get; }
    }
}
