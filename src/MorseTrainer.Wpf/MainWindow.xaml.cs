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

    private void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        var window = new SettingsWindow { Owner = this, DataContext = ViewModel.Settings };
        window.ShowDialog();
    }

    private void OnHelpClick(object sender, RoutedEventArgs e) => ShowHelp();

    private void ShowHelp()
    {
        new HelpWindow { Owner = this }.ShowDialog();
    }

    // Горячие клавиши. Ловим их до того, как их обработает поле ввода
    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        var vm = ViewModel;
        bool ctrl = Keyboard.Modifiers == ModifierKeys.Control;

        // Пробел работает как телеграфный ключ в режиме передачи
        if (e.Key == Key.Space && vm.SelectedTab == MainViewModel.TrainerTab && vm.Trainer.IsKeyMode)
        {
            if (!e.IsRepeat)
                vm.Trainer.KeyDown();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.F1)
            ShowHelp();
        else if (e.Key == Key.Escape)
            vm.Player.Stop();
        else if (ctrl && e.Key >= Key.D1 && e.Key <= Key.D4)
            vm.SelectedTab = e.Key - Key.D1;
        else if (ctrl && e.Key == Key.Enter)
            vm.PlayCurrent();
        else if (ctrl && e.Key == Key.S)
            vm.SaveWav();
        else if (ctrl && e.Key == Key.R)
            vm.Repeat();
        else if (ctrl && e.Key == Key.Up)
            vm.Settings.ChangeWpm(1);
        else if (ctrl && e.Key == Key.Down)
            vm.Settings.ChangeWpm(-1);
        else
            return;

        e.Handled = true;
    }

    private void OnPreviewKeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space && ViewModel.SelectedTab == MainViewModel.TrainerTab && ViewModel.Trainer.IsKeyMode)
        {
            ViewModel.Trainer.KeyUp();
            e.Handled = true;
        }
    }
}
