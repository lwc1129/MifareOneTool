using Avalonia.Controls;
using MifareOneTool.UI.ViewModels;

namespace MifareOneTool.UI.Views;

public partial class DiffWindow : Window
{
    public DiffWindow()
    {
        InitializeComponent();
        var vm = new DiffWindowViewModel();
        vm.Owner = this;
        DataContext = vm;
    }
}
