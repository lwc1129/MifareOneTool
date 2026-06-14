using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MifareOneTool.UI.Views;

public partial class UidInputDialog : Window
{
    public string? Result { get; private set; }

    public UidInputDialog(string initialValue = "")
    {
        InitializeComponent();
        UidBox.Text = initialValue;
    }

    private void OnOk(object? sender, RoutedEventArgs e)
    {
        Result = UidBox.Text?.Trim();
        Close();
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Result = null;
        Close();
    }
}
