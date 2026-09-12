using System.Windows.Controls;
using System.Windows.Input;
using MorseTrainer.Wpf.ViewModels;

namespace MorseTrainer.Wpf.Views;

public partial class ReferenceView : UserControl
{
    public ReferenceView()
    {
        InitializeComponent();
    }

    // Клик по строке – проиграть символ (даже если строка уже выбрана)
    private void OnTableClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is ReferenceViewModel viewModel)
            viewModel.PlaySelectedCommand.Execute(null);
    }
}
