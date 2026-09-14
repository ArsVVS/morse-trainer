using System.Windows;
using System.Windows.Input;
using MorseTrainer.Wpf.ViewModels;

namespace MorseTrainer.Wpf;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private MainViewModel ViewModel => (MainViewModel)DataContext;

    // Пробел работает как телеграфный ключ в режиме передачи
    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space && ViewModel.SelectedTab == 1 && ViewModel.Trainer.IsKeyMode)
        {
            if (!e.IsRepeat)
                ViewModel.Trainer.KeyDown();
            e.Handled = true;
        }
    }

    private void OnPreviewKeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space && ViewModel.SelectedTab == 1 && ViewModel.Trainer.IsKeyMode)
        {
            ViewModel.Trainer.KeyUp();
            e.Handled = true;
        }
    }
}
