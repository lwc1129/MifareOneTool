using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MifareOneTool.Core.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace MifareOneTool.UI.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly AppSettings _settings;

    [ObservableProperty] private string _statusText = "Ready";
    [ObservableProperty] private string _logText = "";
    [ObservableProperty] private string _uidText = "";
    [ObservableProperty] private bool _autoABN = true;
    [ObservableProperty] private bool _writeCheck = true;
    [ObservableProperty] private string? _selectedDevice;
    [ObservableProperty] private string? _selectedLanguage;

    public ObservableCollection<string> Devices { get; } = new();
    public ObservableCollection<string> Languages { get; } = new()
    {
        "English", "中文", "繁體中文", "Русский"
    };

    public MainWindowViewModel()
    {
        _settings = AppSettings.Load();
        AutoABN = _settings.AutoABN;
        WriteCheck = _settings.WriteCheck;
        SelectedLanguage = _settings.Language;
    }

    private void AppendLog(string line)
    {
        LogText += line + "\n";
    }

    [RelayCommand]
    private async Task ScanDevicesAsync()
    {
        StatusText = "Scanning devices...";
        Devices.Clear();
        AppendLog("Device scan not yet implemented in Avalonia UI.");
        StatusText = "Ready";
        await Task.CompletedTask;
    }

    [RelayCommand]
    private void ClearLog() => LogText = "";

    [RelayCommand]
    private async Task OpenFileAsync()
    {
        AppendLog("Open file — TODO: wire up IStorageProvider");
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task SaveFileAsync()
    {
        AppendLog("Save file — TODO");
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task SaveAsFileAsync()
    {
        AppendLog("Save as — TODO");
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task ImportMctAsync()
    {
        AppendLog("Import MCT — TODO");
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task ExportMctAsync()
    {
        AppendLog("Export MCT — TODO");
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task ExportKeyDictAsync()
    {
        AppendLog("Export key dict — TODO");
        await Task.CompletedTask;
    }

    [RelayCommand]
    private void Exit() => System.Environment.Exit(0);

    [RelayCommand]
    private void OpenHexTool() => AppendLog("S50HTool — TODO: open window");

    [RelayCommand]
    private void OpenDiff() => AppendLog("Diff — TODO: open window");

    [RelayCommand]
    private void OpenHardNes() => AppendLog("Hard Nested — TODO: open window");

    [RelayCommand]
    private void OpenMff08() => AppendLog("MFF08 — TODO: open window");

    [RelayCommand]
    private void About() => AppendLog("MifareOneTool — cross-platform Avalonia UI");

    [RelayCommand]
    private async Task CheckUpdateAsync()
    {
        AppendLog("Checking for updates...");
        var svc = new MifareOneTool.Core.Update.GitHubUpdateService();
        await svc.CheckAsync("iceman1001/mfoc");
        AppendLog($"Local: {svc.LocalVersion}  Remote: {svc.RemoteVersion}");
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task ReadMfocAsync()
    {
        AppendLog("mfoc read — TODO: invoke NFC tool via DesktopNfcToolRunner");
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task ReadMfcukAsync()
    {
        AppendLog("mfcuk read — TODO");
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task WriteBlock0Async()
    {
        AppendLog("Write block 0 — TODO");
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task WriteFullAsync()
    {
        AppendLog("Write full — TODO");
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task WriteUidAsync()
    {
        AppendLog($"Write UID: {UidText} — TODO");
        await Task.CompletedTask;
    }
}
