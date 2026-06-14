using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MifareOneTool.Core.Services;
using MifareOneTool.UI.Services;
using MifareOneTool.UI.Views;

namespace MifareOneTool.UI;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        // Apply saved language before any window is created
        var settings = AppSettings.Load();
        LocalizationService.Instance.Apply(settings.Language);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }
        base.OnFrameworkInitializationCompleted();
    }
}
