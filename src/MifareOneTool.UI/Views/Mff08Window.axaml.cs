using Avalonia.Controls;
using MifareOneTool.UI.ViewModels;

namespace MifareOneTool.UI.Views;

public partial class Mff08Window : Window
{
    public Mff08Window()
    {
        InitializeComponent();
        var vm = new Mff08WindowViewModel();
        vm.Owner = this;
        DataContext = vm;
    }
}
