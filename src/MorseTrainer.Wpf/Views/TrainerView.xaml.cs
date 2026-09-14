using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using MorseTrainer.Wpf.ViewModels;

namespace MorseTrainer.Wpf.Views;

public partial class TrainerView : UserControl
{
    private TrainerViewModel? _viewModel;

    public TrainerView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_viewModel != null)
            _viewModel.TaskShown -= OnTaskShown;

        _viewModel = DataContext as TrainerViewModel;
        if (_viewModel != null)
            _viewModel.TaskShown += OnTaskShown;
    }

    // Новое задание – ставим курсор в поле ответа
    private void OnTaskShown(object? sender, EventArgs e)
    {
        Dispatcher.BeginInvoke(DispatcherPriority.Input, () =>
        {
            if (AnswerBox.IsVisible)
                AnswerBox.Focus();
            else
                Focus();
        });
    }
}
