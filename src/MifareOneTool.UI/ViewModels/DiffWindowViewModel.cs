using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MifareOneTool.Core.Models;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MifareOneTool.UI.ViewModels;

public partial class DiffWindowViewModel : ObservableObject
{
    [ObservableProperty] private string _btnALabel = "Load A...";
    [ObservableProperty] private string _btnBLabel = "Load B...";
    [ObservableProperty] private string _resultText = "";

    private S50 _sa = new();
    private S50 _sb = new();
    private string _fileA = "";
    private string _fileB = "";

    // Window reference injected from code-behind via property
    public Window? Owner { get; set; }

    [RelayCommand]
    private async Task LoadAAsync()
    {
        var path = await PickMfdAsync("Select dump file A");
        if (path == null) return;
        _fileA = path;
        _sa = new S50();
        _sa.LoadFromMfd(_fileA);
        BtnALabel = "A=" + Path.GetFileName(_fileA);
    }

    [RelayCommand]
    private async Task LoadBAsync()
    {
        var path = await PickMfdAsync("Select dump file B");
        if (path == null) return;
        _fileB = path;
        _sb = new S50();
        _sb.LoadFromMfd(_fileB);
        BtnBLabel = "B=" + Path.GetFileName(_fileB);
    }

    [RelayCommand]
    private void Compare()
    {
        if (!File.Exists(_fileA) || !File.Exists(_fileB))
        {
            ResultText = "One or both files are not loaded.";
            return;
        }
        ResultText = BuildDiff();
    }

    private string BuildDiff()
    {
        var sb = new StringBuilder();
        int diffCount = 0;
        for (int i = 0; i < 16; i++)
        {
            sb.AppendLine("%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%");
            sb.AppendLine($"Sector {i}");
            for (int a = 0; a < 4; a++)
            {
                string res = "";
                for (int b = 0; b < 16; b++)
                {
                    res += _sa.Sectors[i].Block[a][b] == _sb.Sectors[i].Block[a][b] ? "-- " : "## ";
                }
                sb.AppendLine("A: " + Utils.Hex2StrWithSpan(_sa.Sectors[i].Block[a]));
                sb.AppendLine("B: " + Utils.Hex2StrWithSpan(_sb.Sectors[i].Block[a]));
                sb.AppendLine("   " + res);
                if (res.Contains("##")) diffCount++;
            }
        }
        return $"Found {diffCount} different block(s).\n" + sb.ToString();
    }

    private async Task<string?> PickMfdAsync(string title)
    {
        if (Owner == null) return null;
        var files = await Owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("MFD dump") { Patterns = new[] { "*.mfd", "*.dump" } },
                FilePickerFileTypes.All,
            }
        });
        return files.Count > 0 ? files[0].TryGetLocalPath() : null;
    }
}
