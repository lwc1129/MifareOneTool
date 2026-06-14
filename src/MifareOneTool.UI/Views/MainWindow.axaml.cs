using Avalonia.Controls;
using MifareOneTool.UI.ViewModels;

namespace MifareOneTool.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }
}
