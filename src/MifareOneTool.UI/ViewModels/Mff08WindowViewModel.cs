using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MifareOneTool.Core.Models;
using MifareOneTool.Core.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MifareOneTool.UI.ViewModels;

public partial class Mff08WindowViewModel : ObservableObject
{
    [ObservableProperty] private string _keyfilePath = "";
    [ObservableProperty] private string _logText = "";
    [ObservableProperty] private bool _isBusy = false;

    public Window? Owner { get; set; }

    private void AppendLog(string line)
    {
        Dispatcher.UIThread.Post(() => LogText += line + "\n");
    }

    [RelayCommand]
    private async Task BrowseKeyfileAsync()
    {
        if (Owner == null) return;
        var files = await Owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select last-write dump file",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("MFD/dump") { Patterns = new[] { "*.mfd", "*.dump" } },
                FilePickerFileTypes.All,
            }
        });
        if (files.Count > 0) KeyfilePath = files[0].TryGetLocalPath() ?? "";
    }

    [RelayCommand]
    private void ClearKeyfile() => KeyfilePath = "";

    [RelayCommand]
    private async Task WriteEmptyAsync()
    {
        if (IsBusy) return;
        var s50 = new S50();
        s50.ExportToMfd("mff08_empty.kmf");
        await RunMff08Async("c A u \"mff08_empty.kmf\"");
    }

    [RelayCommand]
    private async Task WriteWithKeyAsync()
    {
        if (IsBusy) return;
        if (string.IsNullOrWhiteSpace(KeyfilePath))
        {
            AppendLog("Error: no key dump file selected.");
            return;
        }
        var s50 = new S50();
        s50.ExportToMfd("mff08_empty.kmf");
        await RunMff08Async($"c C u \"mff08_empty.kmf\" \"{KeyfilePath}\" f");
    }

    private async Task RunMff08Async(string arguments)
    {
        var runner = new DesktopNfcToolRunner();
        string tool = runner.ResolveTool("mff08");
        if (!File.Exists(tool))
        {
            AppendLog($"Error: mff08 tool not found at '{tool}'");
            return;
        }

        IsBusy = true;
        AppendLog($"Running: {tool} {arguments}");
        try
        {
            var psi = new ProcessStartInfo(tool)
            {
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            using var proc = Process.Start(psi)!;
            proc.OutputDataReceived += (_, e) => { if (e.Data != null) AppendLog(e.Data); };
            proc.ErrorDataReceived  += (_, e) => { if (e.Data != null) AppendLog(e.Data); };
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            await proc.WaitForExitAsync();
            AppendLog("Done.");
        }
        catch (Exception ex)
        {
            AppendLog($"Error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
