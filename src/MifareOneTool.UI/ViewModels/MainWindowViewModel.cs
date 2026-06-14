using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MifareOneTool.Core.Models;
using MifareOneTool.Core.Services;
using MifareOneTool.UI.Services;
using MifareOneTool.UI.Views;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MifareOneTool.UI.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly AppSettings _settings;
    private readonly NfcService _nfc;
    private bool _suppressLanguageChange = true;
    private string _keyMfd = "";                 // selected key dump
    private string _lastDumpPath = "";           // last read/write mfd path
    private CancellationTokenSource? _cts;

    [ObservableProperty] private string _statusText = "Ready";
    [ObservableProperty] private string _logText = "";
    [ObservableProperty] private string _uidText = "";
    [ObservableProperty] private bool _autoABN = true;
    [ObservableProperty] private bool _writeCheck = true;
    [ObservableProperty] private bool _isBusy = false;
    [ObservableProperty] private string? _selectedDevice;
    [ObservableProperty] private string? _selectedLanguage;

    public Window? Owner { get; set; }
    public ObservableCollection<string> Devices { get; } = new();
    public ObservableCollection<string> Languages { get; } = new()
    {
        "English", "中文", "繁體中文", "Русский"
    };

    public MainWindowViewModel()
    {
        _settings = AppSettings.Load();
        _nfc = new NfcService();
        AutoABN = _settings.AutoABN;
        WriteCheck = _settings.WriteCheck;

        _suppressLanguageChange = true;
        SelectedLanguage = LocalizationService.CultureToDisplay.TryGetValue(_settings.Language, out var display)
            ? display : "English";
        _suppressLanguageChange = false;
    }

    // ── Language / settings persistence ───────────────────────────────────

    partial void OnSelectedLanguageChanged(string? value)
    {
        if (_suppressLanguageChange || value == null) return;
        if (!LocalizationService.DisplayToCulture.TryGetValue(value, out var culture)) culture = "";
        if (culture == _settings.Language) return;
        _settings.Language = culture;
        _settings.Save();
        LocalizationService.Instance.Apply(culture);
        StatusText = LocalizationService.Instance.Get("Status.Ready");
        AppendLog(LocalizationService.Instance.Get("Msg.RestartRequired"));
    }

    partial void OnAutoABNChanged(bool value)
    {
        if (_suppressLanguageChange) return;
        _settings.AutoABN = value; _settings.Save();
    }

    partial void OnWriteCheckChanged(bool value)
    {
        if (_suppressLanguageChange) return;
        _settings.WriteCheck = value; _settings.Save();
    }

    // ── Logging helpers ───────────────────────────────────────────────────

    private void AppendLog(string? line)
    {
        if (line == null) return;
        Dispatcher.UIThread.Post(() => LogText += line + "\n");
    }

    private IProgress<string> MakeProgress() => new Progress<string>(AppendLog);

    private void SetBusy(bool busy)
    {
        Dispatcher.UIThread.Post(() =>
        {
            IsBusy = busy;
            StatusText = busy
                ? "Running..."
                : LocalizationService.Instance.Get("Status.Ready");
        });
    }

    // ── Device scanning ───────────────────────────────────────────────────

    [RelayCommand]
    private async Task ScanDevicesAsync()
    {
        if (IsBusy) return;
        SetBusy(true);
        Devices.Clear();
        _cts = new CancellationTokenSource();
        try
        {
            var found = await _nfc.ScanDevicesAsync(MakeProgress(), _cts.Token);
            Dispatcher.UIThread.Post(() =>
            {
                foreach (var d in found) Devices.Add(d);
                if (found.Count == 0) AppendLog("No devices found.");
                else SelectedDevice = found[0];
            });
        }
        catch (Exception ex) { AppendLog($"Error: {ex.Message}"); }
        finally { SetBusy(false); }
    }

    // ── Read card ─────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task ReadMfocAsync()
    {
        if (IsBusy) return;
        var outPath = await PickSaveAsync("Save dump as", "mfd", "*.mfd");
        if (outPath == null) return;

        string keyMode = AutoABN && !string.IsNullOrEmpty(_keyMfd) ? "C" : "A";
        SetBusy(true);
        _cts = new CancellationTokenSource();
        try
        {
            AppendLog("Starting mfoc crack...");
            int code = await _nfc.MfocCrackAsync(outPath, "", MakeProgress(), _cts.Token);
            if (code == 0) { _lastDumpPath = outPath; AppendLog($"Done → {outPath}"); }
            else { AppendLog("mfoc failed — card may need known keys."); File.Delete(outPath); }
        }
        catch (Exception ex) { AppendLog($"Error: {ex.Message}"); }
        finally { SetBusy(false); }
    }

    [RelayCommand]
    private void ReadMfcuk()
    {
        if (IsBusy) return;
        AppendLog("mfcuk (hard crack) — opens a terminal window on desktop platforms.");
        // mfcuk is interactive; launch in shell window
        var runner = new DesktopNfcToolRunner();
        string tool = runner.ResolveTool("mfcuk");
        if (!File.Exists(tool)) { AppendLog($"mfcuk not found at: {tool}"); return; }
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo(runner.ShellExe)
            {
                Arguments = $"{runner.ShellArgs} \"{tool}\" -C -R 0:A",
                UseShellExecute = true,
            };
            System.Diagnostics.Process.Start(psi);
        }
        catch (Exception ex) { AppendLog($"Error: {ex.Message}"); }
    }

    // ── Write card ────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task WriteBlock0Async()
    {
        if (IsBusy) return;
        var srcPath = await PickOpenAsync("Select dump to write (Block 0 only)", "*.mfd", "*.dump");
        if (srcPath == null) return;
        if (!ValidateDump(srcPath)) return;

        SetBusy(true);
        _cts = new CancellationTokenSource();
        try
        {
            string keyMode = AutoABN && !string.IsNullOrEmpty(_keyMfd) ? "C" : "N";
            AppendLog("Writing block 0...");
            int code = await _nfc.WriteCardAsync(srcPath, keyMode,
                string.IsNullOrEmpty(_keyMfd) ? null : _keyMfd,
                MakeProgress(), _cts.Token);
            AppendLog(code == 0 ? "Write OK." : "Write failed.");
        }
        catch (Exception ex) { AppendLog($"Error: {ex.Message}"); }
        finally { SetBusy(false); }
    }

    [RelayCommand]
    private async Task WriteFullAsync()
    {
        if (IsBusy) return;
        var srcPath = await PickOpenAsync("Select dump to write", "*.mfd", "*.dump");
        if (srcPath == null) return;
        if (!ValidateDump(srcPath)) return;

        SetBusy(true);
        _cts = new CancellationTokenSource();
        try
        {
            string keyMode = AutoABN && !string.IsNullOrEmpty(_keyMfd) ? "C" : "A";
            AppendLog("Writing full card...");
            int code = await _nfc.WriteCardAsync(srcPath, keyMode,
                string.IsNullOrEmpty(_keyMfd) ? null : _keyMfd,
                MakeProgress(), _cts.Token);
            AppendLog(code == 0 ? "Write OK." : "Write failed.");
        }
        catch (Exception ex) { AppendLog($"Error: {ex.Message}"); }
        finally { SetBusy(false); }
    }

    // ── Write UID ─────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task WriteUidAsync()
    {
        if (IsBusy) return;
        string uid = UidText.Trim();
        if (!Regex.IsMatch(uid, @"^[0-9A-Fa-f]{8}$"))
        {
            AppendLog("Invalid UID — must be exactly 8 hex characters.");
            return;
        }
        SetBusy(true);
        _cts = new CancellationTokenSource();
        try
        {
            AppendLog($"Writing UID {uid.ToUpper()}...");
            int code = await _nfc.WriteUidAsync(uid, MakeProgress(), _cts.Token);
            AppendLog(code == 0 ? "UID write OK." : "UID write failed.");
        }
        catch (Exception ex) { AppendLog($"Error: {ex.Message}"); }
        finally { SetBusy(false); }
    }

    // ── File operations ───────────────────────────────────────────────────

    [RelayCommand]
    private async Task OpenFileAsync()
    {
        var path = await PickOpenAsync("Open MFD", "*.mfd", "*.dump");
        if (path == null) return;
        _lastDumpPath = path;
        AppendLog($"Loaded: {path}");
    }

    [RelayCommand]
    private void SaveFile()
    {
        if (string.IsNullOrEmpty(_lastDumpPath)) { AppendLog("No file loaded."); return; }
        AppendLog($"File path: {_lastDumpPath}");
    }

    [RelayCommand]
    private async Task SaveAsFileAsync()
    {
        var path = await PickSaveAsync("Save As", "mfd", "*.mfd");
        if (path == null) return;
        if (!string.IsNullOrEmpty(_lastDumpPath))
            File.Copy(_lastDumpPath, path, overwrite: true);
        AppendLog($"Saved to: {path}");
    }

    [RelayCommand]
    private async Task ImportMctAsync()
    {
        var path = await PickOpenAsync("Import MCT", "*.txt");
        if (path == null) return;
        try
        {
            var s50 = new S50();
            s50.LoadFromMctTxt(path);
            string tmp = Path.GetTempFileName() + ".mfd";
            s50.ExportToMfd(tmp);
            _lastDumpPath = tmp;
            AppendLog($"MCT imported: {path}");
        }
        catch (Exception ex) { AppendLog($"Import error: {ex.Message}"); }
    }

    [RelayCommand]
    private async Task ExportMctAsync()
    {
        if (string.IsNullOrEmpty(_lastDumpPath)) { AppendLog("No dump loaded."); return; }
        var path = await PickSaveAsync("Export MCT", "txt", "*.txt");
        if (path == null) return;
        try
        {
            var s50 = new S50();
            s50.LoadFromMfd(_lastDumpPath);
            s50.ExportToMctTxt(path);
            AppendLog($"MCT exported: {path}");
        }
        catch (Exception ex) { AppendLog($"Export error: {ex.Message}"); }
    }

    [RelayCommand]
    private async Task ExportKeyDictAsync()
    {
        if (string.IsNullOrEmpty(_lastDumpPath)) { AppendLog("No dump loaded."); return; }
        var path = await PickSaveAsync("Export Key Dict", "dic", "*.dic");
        if (path == null) return;
        try
        {
            var s50 = new S50();
            s50.LoadFromMfd(_lastDumpPath);
            File.WriteAllLines(path, s50.KeyListStr());
            AppendLog($"Key dict exported: {path}");
        }
        catch (Exception ex) { AppendLog($"Export error: {ex.Message}"); }
    }

    // ── Secondary windows ────────────────────────────────────────────────

    [RelayCommand]
    private void OpenHexTool() => new HexToolWindow().Show(Owner!);

    [RelayCommand]
    private void OpenDiff() => new DiffWindow().Show(Owner!);

    [RelayCommand]
    private async Task OpenHardNesAsync()
    {
        var dlg = new HardNesDialog();
        await dlg.ShowDialog(Owner!);
        var vm = (HardNesDialogViewModel)dlg.DataContext!;
        if (!vm.Confirmed) return;
        AppendLog($"Hard Nested — args: {vm.GetArg()}");
        AppendLog("(mfhard execution not yet wired — use the terminal CLI for now)");
    }

    [RelayCommand]
    private void OpenMff08() => new Mff08Window().Show(Owner!);

    // ── Menu / misc ──────────────────────────────────────────────────────

    [RelayCommand]
    private void ClearLog() => LogText = "";

    [RelayCommand]
    private void Exit() => Environment.Exit(0);

    [RelayCommand]
    private void About() => AppendLog("MifareOneTool — cross-platform Avalonia UI (dev branch)");

    [RelayCommand]
    private async Task CheckUpdateAsync()
    {
        AppendLog("Checking for updates...");
        var svc = new MifareOneTool.Core.Update.GitHubUpdateService();
        await svc.CheckAsync("iceman1001/mfoc");
        AppendLog($"Local: {svc.LocalVersion}  Remote: {svc.RemoteVersion}  HasUpdate: {svc.HasUpdate}");
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private bool ValidateDump(string path)
    {
        if (!WriteCheck) return true;
        try
        {
            var s50 = new S50();
            s50.LoadFromMfd(path);
            if (s50.Verify()[16] != 0)
            {
                AppendLog("Dump has errors — open in HexTool to fix before writing.");
                return false;
            }
            return true;
        }
        catch (Exception ex) { AppendLog($"Cannot open dump: {ex.Message}"); return false; }
    }

    private async Task<string?> PickOpenAsync(string title, params string[] patterns)
    {
        if (Owner == null) return null;
        var files = await Owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Data file") { Patterns = patterns },
                FilePickerFileTypes.All,
            }
        });
        return files.Count > 0 ? files[0].TryGetLocalPath() : null;
    }

    private async Task<string?> PickSaveAsync(string title, string ext, string pattern)
    {
        if (Owner == null) return null;
        var file = await Owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            DefaultExtension = ext,
            FileTypeChoices = new[] { new FilePickerFileType("File") { Patterns = new[] { pattern } } }
        });
        return file?.TryGetLocalPath();
    }
}
