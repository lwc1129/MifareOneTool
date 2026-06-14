using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Text.RegularExpressions;

namespace MifareOneTool.UI.ViewModels;

public partial class HardNesDialogViewModel : ObservableObject
{
    private readonly Window _window;

    [ObservableProperty] private string _knownKey = "FFFFFFFFFFFF";
    [ObservableProperty] private string _sector1 = "0";
    [ObservableProperty] private string _sector2 = "1";
    [ObservableProperty] private bool _key1IsA = true;
    [ObservableProperty] private bool _key1IsB = false;
    [ObservableProperty] private bool _key2IsA = true;
    [ObservableProperty] private bool _key2IsB = false;
    [ObservableProperty] private bool _collectOnly = false;

    // Set to true when user clicks OK with valid input
    public bool Confirmed { get; private set; } = false;

    public HardNesDialogViewModel(Window window)
    {
        _window = window;
    }

    [RelayCommand]
    private void Ok()
    {
        if (!Regex.IsMatch(KnownKey.Trim(), @"^[0-9A-Fa-f]{12}$"))
        {
            // Could show inline error — for now just return without confirming
            return;
        }
        if (!int.TryParse(Sector1, out int s1) || s1 < 0) return;
        if (!int.TryParse(Sector2, out int s2) || s2 < 0) return;

        Confirmed = true;
        _window.Close();
    }

    [RelayCommand]
    private void Cancel()
    {
        Confirmed = false;
        _window.Close();
    }

    // Helpers for the caller to build mfhard arguments
    public string GetArg()
    {
        int s1 = int.Parse(Sector1);
        int s2 = int.Parse(Sector2);
        return $"{KnownKey.ToUpper()} {GetTrailerBlock(s1)} {(Key1IsA ? "A" : "B")} {GetTrailerBlock(s2)} {(Key2IsA ? "A" : "B")}";
    }

    public string GetFileSuffix()
    {
        int s2 = int.Parse(Sector2);
        return $"_{GetTrailerBlock(s2):D3}{(Key2IsA ? "A" : "B")}.txt";
    }

    private static int GetTrailerBlock(int sector) =>
        sector < 32 ? sector * 4 + 3 : 128 + 16 * (sector - 32) + 15;
}
