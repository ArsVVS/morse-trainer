using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using MorseTrainer.Core;
using MorseTrainer.Wpf.ViewModels;

namespace MorseTrainer.Wpf.Views;

public partial class StatisticsView : UserControl
{
    private StatisticsViewModel? _viewModel;

    public StatisticsView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.Refreshed -= OnRefreshed;
            _viewModel.Settings.PropertyChanged -= OnSettingsChanged;
        }

        _viewModel = DataContext as StatisticsViewModel;
        if (_viewModel != null)
        {
            _viewModel.Refreshed += OnRefreshed;
            _viewModel.Settings.PropertyChanged += OnSettingsChanged;
            DrawPlot();
        }
    }

    private void OnRefreshed(object? sender, EventArgs e) => DrawPlot();

    // При смене темы перекрашиваем график
    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsViewModel.Theme))
            DrawPlot();
    }

    private void DrawPlot()
    {
        if (_viewModel == null)
            return;

        var plot = AccuracyPlot.Plot;
        plot.Clear();

        var days = _viewModel.Daily;
        if (days.Count > 0)
        {
            double[] xs = days.Select(d => d.Day.ToOADate()).ToArray();
            double[] ys = days.Select(d => d.Accuracy * 100).ToArray();
            var line = plot.Add.Scatter(xs, ys);
            line.LineWidth = 2;
            line.MarkerSize = 8;
            // Подписи по оси X – только даты дней с тренировками
            string[] labels = days.Select(d => d.Day.ToString("dd.MM")).ToArray();
            plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(xs, labels);
            plot.Axes.SetLimitsX(xs.Min() - 1, xs.Max() + 1);
        }

        plot.Axes.SetLimitsY(0, 105);
        plot.YLabel("Точность, %");

        // Цвета под тему
        bool dark = _viewModel.Settings.Theme == AppTheme.Dark;
        var background = ScottPlot.Color.FromHex(dark ? "#202020" : "#FFFFFF");
        var foreground = ScottPlot.Color.FromHex(dark ? "#E0E0E0" : "#202020");
        plot.FigureBackground.Color = background;
        plot.DataBackground.Color = background;
        plot.Axes.Color(foreground);
        plot.Grid.MajorLineColor = foreground.WithAlpha(0.15);

        plot.Font.Automatic();
        AccuracyPlot.Refresh();
    }
}
