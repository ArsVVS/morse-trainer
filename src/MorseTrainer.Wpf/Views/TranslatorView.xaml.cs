using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using MorseTrainer.Wpf.ViewModels;

namespace MorseTrainer.Wpf.Views;

public partial class TranslatorView : UserControl
{
    private TranslatorViewModel? _viewModel;

    public TranslatorView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_viewModel != null)
            _viewModel.PropertyChanged -= OnViewModelChanged;

        _viewModel = DataContext as TranslatorViewModel;
        if (_viewModel != null)
            _viewModel.PropertyChanged += OnViewModelChanged;
    }

    // Подсвечиваем текущую букву в поле текста выделением
    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(TranslatorViewModel.HighlightIndex) || _viewModel == null)
            return;

        int index = _viewModel.HighlightIndex;
        if (index >= 0 && index < SourceTextBox.Text.Length)
        {
            // Без фокуса выделение в поле не видно
            SourceTextBox.Focus();
            SourceTextBox.Select(index, 1);
        }
        else
            SourceTextBox.Select(0, 0);
    }
}
