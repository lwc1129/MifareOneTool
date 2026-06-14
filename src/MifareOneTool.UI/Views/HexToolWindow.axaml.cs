using Avalonia.Controls;
using MifareOneTool.UI.ViewModels;

namespace MifareOneTool.UI.Views;

public partial class HexToolWindow : Window
{
    public HexToolWindow()
    {
        InitializeComponent();
        DataContext = new HexToolWindowViewModel(this);
    }
}
