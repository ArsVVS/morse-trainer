using System.Windows;
using MorseTrainer.Core;
using MorseTrainer.Wpf.ViewModels;

namespace MorseTrainer.Wpf;

public partial class App : Application
{
    private MainViewModel? _main;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var settings = AppSettings.Load();
        _main = new MainViewModel(settings);
        ApplyTheme(settings.Theme);

        var window = new MainWindow { DataContext = _main };
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _main?.Settings.Save();
        base.OnExit(e);
    }

    /// <summary>Включает светлую или тёмную тему без перезапуска.</summary>
    public static void ApplyTheme(AppTheme theme)
    {
        Current.ThemeMode = theme == AppTheme.Dark ? ThemeMode.Dark : ThemeMode.Light;
    }
}
