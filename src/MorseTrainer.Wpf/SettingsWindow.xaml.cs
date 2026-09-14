using System.ComponentModel;
using System.Windows;
using MorseTrainer.Wpf.ViewModels;

namespace MorseTrainer.Wpf;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
    }

    private void OnClose(object sender, RoutedEventArgs e) => Close();

    // Сохраняем настройки в файл при закрытии окна
    protected override void OnClosing(CancelEventArgs e)
    {
        (DataContext as SettingsViewModel)?.Save();
        base.OnClosing(e);
    }
}
