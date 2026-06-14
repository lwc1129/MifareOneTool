using Avalonia.Controls;
using Avalonia.Media;
using MifareOneTool.UI.Views;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MifareOneTool.Core.Models;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MifareOneTool.UI.ViewModels;

public partial class HexToolWindowViewModel : ObservableObject
{
    private readonly Window _window;
    private S50 _s50 = new();
    private string _currentFile = "";

    [ObservableProperty] private string _logText = "";
    [ObservableProperty] private string _currentSectorLabel = "Current sector: —";
    [ObservableProperty] private int _selectedSectorIndex = -1;

    // Block / key text fields
    [ObservableProperty] private string _block0Text = "";
    [ObservableProperty] private string _block1Text = "";
    [ObservableProperty] private string _block2Text = "";
    [ObservableProperty] private string _keyAText = "";
    [ObservableProperty] private string _keyBText = "";

    // Validation colours (green = valid, red = invalid)
    [ObservableProperty] private IBrush _block0Color = Brushes.White;
    [ObservableProperty] private IBrush _block1Color = Brushes.White;
    [ObservableProperty] private IBrush _block2Color = Brushes.White;
    [ObservableProperty] private IBrush _keyAColor  = Brushes.White;
    [ObservableProperty] private IBrush _keyBColor  = Brushes.White;

    // Access-bits combo indices
    [ObservableProperty] private int _acBit0 = 0;
    [ObservableProperty] private int _acBit1 = 0;
    [ObservableProperty] private int _acBit2 = 0;
    [ObservableProperty] private int _acBit3 = 1;

    public ObservableCollection<string> SectorInfos { get; } = new();

    public ObservableCollection<string> AcDataOptions { get; } = new()
    {
        "KeyAB R/W + Inc/Dec",
        "[RO] KeyAB R+Dec / no W+Inc",
        "[RO] KeyAB R / no W+Inc/Dec",
        "KeyB R/W / no Inc/Dec",
        "KeyAB R / KeyB W / no Inc/Dec",
        "[RO] KeyB R / no W+Inc/Dec",
        "KeyAB R+Dec / KeyB W+Inc",
        "[RO] Locked",
    };

    public ObservableCollection<string> AcTrailerOptions { get; } = new()
    {
        "[Irrev] KeyA: A-write / AC: A-RO / KeyB: A-RW",
        "KeyA: A-write / AC: A-RW / KeyB: A-RW",
        "[Irrev] KeyA: none / AC: A-RO / KeyB: A-R",
        "KeyA: B-write / AC: A-RO,B-RW / KeyB: B-write",
        "[Irrev] KeyA: B-write / AC: AB-RO / KeyB: B-write",
        "KeyA: none / AC: A-RO,B-RW / KeyB: none",
        "[Irrev] KeyA: none / AC: AB-RO / KeyB: none",
        "[Irrev] KeyA: none / AC: AB-RO / KeyB: none (dup)",
    };

    public HexToolWindowViewModel(Window window)
    {
        _window = window;
        NewCard();
    }

    partial void OnSelectedSectorIndexChanged(int value)
    {
        if (value >= 0 && value < 16) LoadSectorEditor(value);
    }

    private void AppendLog(string line) => LogText += line + "\n";

    private static string Hex(byte[] b) => Utils.Hex2Str(b);

    private static IBrush ValidBrush  => new SolidColorBrush(Color.Parse("#C8F7C5")); // light green
    private static IBrush InvalidBrush => new SolidColorBrush(Color.Parse("#F7C5C5")); // light red

    private void ReloadList()
    {
        SectorInfos.Clear();
        for (int i = 0; i < 16; i++)
            SectorInfos.Add(_s50.Sectors[i].Info(i, "Sector ", " [empty]", " [data]", " [error]"));
    }

    private void LoadSectorEditor(int idx)
    {
        CurrentSectorLabel = $"Current sector: {idx}";
        Block0Text = Hex(_s50.Sectors[idx].Block[0]);
        Block1Text = Hex(_s50.Sectors[idx].Block[1]);
        Block2Text = Hex(_s50.Sectors[idx].Block[2]);
        KeyAText   = Hex(_s50.Sectors[idx].Block[3].Take(6).ToArray());
        KeyBText   = Hex(_s50.Sectors[idx].Block[3].Skip(10).Take(6).ToArray());

        byte[] acbits = Utils.ReadAC(_s50.Sectors[idx].Block[3].Skip(6).Take(4).ToArray());
        AcBit0 = acbits[0] & 0x07;
        AcBit1 = acbits[1] & 0x07;
        AcBit2 = acbits[2] & 0x07;
        AcBit3 = acbits[3] & 0x07;

        ValidateAll();

        int res = _s50.Sectors[idx].Verify();
        if ((res & 0x01) != 0)
        {
            // Auto-fix BCC
            _s50.Sectors[idx].Block[0][4] = (byte)(
                _s50.Sectors[idx].Block[0][0] ^ _s50.Sectors[idx].Block[0][1] ^
                _s50.Sectors[idx].Block[0][2] ^ _s50.Sectors[idx].Block[0][3]);
            Block0Text = Hex(_s50.Sectors[idx].Block[0]);
            AppendLog($"Sector {idx}: BCC error auto-corrected.");
        }
        if ((res & 0x06) != 0)
        {
            AcBit0 = 0; AcBit1 = 0; AcBit2 = 0; AcBit3 = 1;
            AppendLog($"Sector {idx}: Invalid AC bits reset to default.");
        }
    }

    private void ValidateAll()
    {
        Block0Color = ValidateHex32(Block0Text) ? ValidBrush : InvalidBrush;
        Block1Color = ValidateHex32(Block1Text) ? ValidBrush : InvalidBrush;
        Block2Color = ValidateHex32(Block2Text) ? ValidBrush : InvalidBrush;
        KeyAColor   = ValidateHex12(KeyAText)   ? ValidBrush : InvalidBrush;
        KeyBColor   = ValidateHex12(KeyBText)   ? ValidBrush : InvalidBrush;
    }

    private static bool ValidateHex32(string s) =>
        s.Length == 32 && Regex.IsMatch(s, @"^[0-9A-Fa-f]{32}$");
    private static bool ValidateHex12(string s) =>
        s.Length == 12 && Regex.IsMatch(s, @"^[0-9A-Fa-f]{12}$");

    [RelayCommand]
    private void ApplyChanges()
    {
        int idx = SelectedSectorIndex;
        if (idx < 0 || idx > 15) return;
        ValidateAll();
        if (Block0Color == InvalidBrush || Block1Color == InvalidBrush ||
            Block2Color == InvalidBrush || KeyAColor == InvalidBrush || KeyBColor == InvalidBrush)
        {
            AppendLog("Cannot apply: one or more fields contain invalid hex data.");
            return;
        }
        _s50.Sectors[idx].Block[0] = Utils.Hex2Block(Block0Text, 16);
        _s50.Sectors[idx].Block[1] = Utils.Hex2Block(Block1Text, 16);
        _s50.Sectors[idx].Block[2] = Utils.Hex2Block(Block2Text, 16);

        byte[] kA = Utils.Hex2Block(KeyAText, 6);
        byte[] kB = Utils.Hex2Block(KeyBText, 6);
        byte[] ac = { (byte)AcBit0, (byte)AcBit1, (byte)AcBit2, (byte)AcBit3 };
        byte[] kC = Utils.GenAC(ac);

        byte lastUC = _s50.Sectors[idx].Block[3][9];
        var block3 = kA.Concat(kC).Concat(kB).Take(16).ToArray();
        _s50.Sectors[idx].Block[3] = block3;
        _s50.Sectors[idx].Block[3][9] = lastUC;

        ReloadList();
        AppendLog($"Sector {idx} updated.");
    }

    [RelayCommand]
    private void NewCard()
    {
        _s50 = new S50();
        _currentFile = "";
        CurrentSectorLabel = "Current sector: —";
        Block0Text = ""; Block1Text = ""; Block2Text = "";
        KeyAText = ""; KeyBText = "";
        ReloadList();
        AppendLog("New card created.");
    }

    [RelayCommand]
    private async Task OpenAsync()
    {
        var path = await PickOpenAsync("Open MFD", "*.mfd", "*.dump");
        if (path == null) return;
        try
        {
            _s50 = new S50();
            _s50.LoadFromMfd(path);
            _currentFile = path;
            ReloadList();
            AppendLog($"Opened: {path}");
        }
        catch (Exception ex) { AppendLog($"Open error: {ex.Message}"); }
    }

    [RelayCommand]
    private void Save()
    {
        if (string.IsNullOrEmpty(_currentFile)) { AppendLog("No file — use Save As."); return; }
        try { _s50.ExportToMfd(_currentFile); AppendLog($"Saved to {_currentFile}"); }
        catch (Exception ex) { AppendLog($"Save error: {ex.Message}"); }
    }

    [RelayCommand]
    private async Task SaveAsAsync()
    {
        var path = await PickSaveAsync("Save MFD As", "mfd", "*.mfd");
        if (path == null) return;
        try { _s50.ExportToMfd(path); _currentFile = path; AppendLog($"Saved to {path}"); }
        catch (Exception ex) { AppendLog($"Save error: {ex.Message}"); }
    }

    [RelayCommand]
    private async Task ImportMctAsync()
    {
        var path = await PickOpenAsync("Import MCT", "*.txt");
        if (path == null) return;
        try
        {
            _s50 = new S50();
            _s50.LoadFromMctTxt(path);
            ReloadList();
            AppendLog($"Imported MCT: {path}");
        }
        catch (Exception ex) { AppendLog($"Import error: {ex.Message}"); }
    }

    [RelayCommand]
    private async Task ExportMctAsync()
    {
        var path = await PickSaveAsync("Export MCT", "txt", "*.txt");
        if (path == null) return;
        try { _s50.ExportToMctTxt(path); AppendLog($"MCT exported: {path}"); }
        catch (Exception ex) { AppendLog($"Export error: {ex.Message}"); }
    }

    [RelayCommand]
    private async Task ExportKeyDictAsync()
    {
        var path = await PickSaveAsync("Export Key Dictionary", "dic", "*.dic");
        if (path == null) return;
        File.WriteAllLines(path, _s50.KeyListStr());
        AppendLog($"Key dict exported: {path}");
    }

    [RelayCommand]
    private async Task ModifyUidAsync()
    {
        byte[] rnd = new byte[4];
        RandomNumberGenerator.Fill(rnd);
        string suggested = Utils.Hex2Str(rnd);

        // Simple input dialog via a small sub-window
        var dialog = new UidInputDialog(suggested);
        await dialog.ShowDialog(_window);
        string? uid = dialog.Result;

        if (uid == null || !Regex.IsMatch(uid, @"^[0-9A-Fa-f]{8}$"))
        {
            AppendLog("UID modification cancelled or invalid input.");
            return;
        }
        byte[] buid = Utils.Hex2Block(uid, 4);
        byte bcc = (byte)(buid[0] ^ buid[1] ^ buid[2] ^ buid[3]);
        for (int i = 0; i < 4; i++) _s50.Sectors[0].Block[0][i] = buid[i];
        _s50.Sectors[0].Block[0][4] = bcc;
        AppendLog($"UID changed to {uid.ToUpper()}, BCC={bcc:X2}");
        if (SelectedSectorIndex == 0) LoadSectorEditor(0);
    }

    [RelayCommand]
    private void ListKeys()
    {
        var sb = new StringBuilder();
        for (int i = 0; i < 16; i++)
        {
            sb.AppendLine($"Sector {i}:");
            sb.AppendLine($"  [A] {Utils.Hex2Str(_s50.Sectors[i].KeyA)}");
            sb.AppendLine($"  [B] {Utils.Hex2Str(_s50.Sectors[i].KeyB)}");
        }
        LogText = sb.ToString();
    }

    [RelayCommand]
    private void CheckCard()
    {
        int[] res = _s50.Verify();
        if (res[16] == 0) { AppendLog("Card OK — no errors found."); return; }
        var sb = new StringBuilder("Errors found:\n");
        for (int i = 0; i < 16; i++)
        {
            if (res[i] == 0) continue;
            sb.AppendLine($"Sector {i}:");
            if ((res[i] & 0x01) != 0) sb.AppendLine("  BCC error");
            if ((res[i] & 0x02) != 0) sb.AppendLine("  Invalid AC bits");
            if ((res[i] & 0x04) != 0) sb.AppendLine("  Corrupt AC bits");
        }
        LogText = sb.ToString();
    }

    [RelayCommand]
    private void FixCard()
    {
        byte[] defaultAC = { 0xFF, 0x07, 0x80, 0x69 };
        int[] res = _s50.Verify();
        if (res[16] == 0) { AppendLog("Card OK — nothing to fix."); return; }
        for (int i = 0; i < 16; i++)
        {
            if ((res[i] & 0x01) != 0)
            {
                _s50.Sectors[i].Block[0][4] = (byte)(
                    _s50.Sectors[i].Block[0][0] ^ _s50.Sectors[i].Block[0][1] ^
                    _s50.Sectors[i].Block[0][2] ^ _s50.Sectors[i].Block[0][3]);
                AppendLog($"Sector {i}: BCC fixed.");
            }
            if ((res[i] & 0x06) != 0)
            {
                for (int j = 6; j < 10; j++) _s50.Sectors[i].Block[3][j] = defaultAC[j - 6];
                AppendLog($"Sector {i}: AC bits reset to default.");
            }
        }
        ReloadList();
    }

    [RelayCommand]
    private void Close() => _window.Close();

    private async Task<string?> PickOpenAsync(string title, params string[] patterns)
    {
        var files = await _window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
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
        var file = await _window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            DefaultExtension = ext,
            FileTypeChoices = new[]
            {
                new FilePickerFileType("File") { Patterns = new[] { pattern } },
            }
        });
        return file?.TryGetLocalPath();
    }
}
