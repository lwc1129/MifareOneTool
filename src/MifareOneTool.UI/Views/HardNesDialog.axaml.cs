using Avalonia.Controls;
using MifareOneTool.UI.ViewModels;

namespace MifareOneTool.UI.Views;

public partial class HardNesDialog : Window
{
    public HardNesDialog()
    {
        InitializeComponent();
        DataContext = new HardNesDialogViewModel(this);
    }
}
