using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MifareOneTool.Core.Services;
using MifareOneTool.UI.Services;
using MifareOneTool.UI.Views;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace MifareOneTool.UI.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly AppSettings _settings;
    private bool _suppressLanguageChange = true;

    [ObservableProperty] private string _statusText = "Ready";
    [ObservableProperty] private string _logText = "";
    [ObservableProperty] private string _uidText = "";
    [ObservableProperty] private bool _autoABN = true;
    [ObservableProperty] private bool _writeCheck = true;
    [ObservableProperty] private string? _selectedDevice;
    [ObservableProperty] private string? _selectedLanguage;

    public Window? Owner { get; set; }
    public ObservableCollection<string> Devices { get; } = new();

    // Display names in their own script so the combo is always readable
    public ObservableCollection<string> Languages { get; } = new()
    {
        "English", "中文", "繁體中文", "Русский"
    };

    public MainWindowViewModel()
    {
        _settings = AppSettings.Load();
        AutoABN = _settings.AutoABN;
        WriteCheck = _settings.WriteCheck;

        // Map saved culture code → display name
        _suppressLanguageChange = true;
        SelectedLanguage = LocalizationService.CultureToDisplay.TryGetValue(_settings.Language, out var display)
            ? display
            : "English";
        _suppressLanguageChange = false;
    }

    partial void OnSelectedLanguageChanged(string? value)
    {
        if (_suppressLanguageChange || value == null) return;

        if (!LocalizationService.DisplayToCulture.TryGetValue(value, out var culture))
            culture = "";

        if (culture == _settings.Language) return;

        _settings.Language = culture;
        _settings.Save();

        // Apply immediately so UI updates without needing a restart
        LocalizationService.Instance.Apply(culture);

        // Refresh observable status text from new locale
        StatusText = LocalizationService.Instance.Get("Status.Ready");

        AppendLog(LocalizationService.Instance.Get("Msg.RestartRequired"));
    }

    partial void OnAutoABNChanged(bool value)
    {
        if (_suppressLanguageChange) return;
        _settings.AutoABN = value;
        _settings.Save();
    }

    partial void OnWriteCheckChanged(bool value)
    {
        if (_suppressLanguageChange) return;
        _settings.WriteCheck = value;
        _settings.Save();
    }

    private void AppendLog(string line)
    {
        LogText += line + "\n";
    }

    [RelayCommand]
    private async Task ScanDevicesAsync()
    {
        StatusText = LocalizationService.Instance.Get("Status.Scanning");
        Devices.Clear();
        AppendLog(LocalizationService.Instance.Get("Msg.ScanNotImpl"));
        StatusText = LocalizationService.Instance.Get("Status.Ready");
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
    private void Exit() => Environment.Exit(0);

    [RelayCommand]
    private void OpenHexTool()
    {
        var win = new HexToolWindow();
        win.Show(Owner!);
    }

    [RelayCommand]
    private void OpenDiff()
    {
        var win = new DiffWindow();
        win.Show(Owner!);
    }

    [RelayCommand]
    private async Task OpenHardNesAsync()
    {
        var dlg = new HardNesDialog();
        await dlg.ShowDialog(Owner!);
        var vm = (HardNesDialogViewModel)dlg.DataContext!;
        if (vm.Confirmed)
            AppendLog($"Hard Nested args: {vm.GetArg()}");
    }

    [RelayCommand]
    private void OpenMff08()
    {
        var win = new Mff08Window();
        win.Show(Owner!);
    }

    [RelayCommand]
    private void About() => AppendLog("MifareOneTool — cross-platform Avalonia UI");

    [RelayCommand]
    private async Task CheckUpdateAsync()
    {
        AppendLog("Checking for updates...");
        var svc = new MifareOneTool.Core.Update.GitHubUpdateService();
        await svc.CheckAsync("iceman1001/mfoc");
        AppendLog($"Local: {svc.LocalVersion}  Remote: {svc.RemoteVersion}");
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
