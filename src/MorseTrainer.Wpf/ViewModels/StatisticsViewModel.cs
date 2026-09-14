using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MorseTrainer.Core;

namespace MorseTrainer.Wpf.ViewModels;

/// <summary>Строка таблицы текущих уровней.</summary>
public record LevelRow(AlphabetKind Alphabet, TrainingMode Mode, int Level);

/// <summary>Вкладка «Статистика».</summary>
public partial class StatisticsViewModel : ObservableObject
{
    private readonly StatisticsStore _store;

    public StatisticsViewModel(SettingsViewModel settings, StatisticsStore store)
    {
        Settings = settings;
        _store = store;
        Refresh();
    }

    /// <summary>Данные обновились – окно перерисовывает график.</summary>
    public event EventHandler? Refreshed;

    public SettingsViewModel Settings { get; }

    [ObservableProperty]
    public partial List<SessionRecord> Sessions { get; set; } = [];

    [ObservableProperty]
    public partial List<ProblemSymbol> Problems { get; set; } = [];

    [ObservableProperty]
    public partial List<LevelRow> Levels { get; set; } = [];

    [ObservableProperty]
    public partial List<DailyAccuracy> Daily { get; set; } = [];

    [ObservableProperty]
    public partial string Summary { get; set; } = "";

    public void Refresh()
    {
        var sessions = _store.Data.Sessions;

        // Последние сессии – сверху
        Sessions = sessions.AsEnumerable().Reverse().Take(30).ToList();
        Problems = _store.GetProblemSymbols();
        Daily = _store.GetDailyAccuracy();
        Levels =
        [
            .. from alphabet in Enum.GetValues<AlphabetKind>()
               from mode in Enum.GetValues<TrainingMode>()
               select new LevelRow(alphabet, mode, _store.GetLevel(alphabet, mode))
        ];
        Summary = sessions.Count == 0
            ? "Пока нет ни одной сессии"
            : $"Сессий: {sessions.Count}, средняя точность: {sessions.Average(s => s.Accuracy):P0}";

        Refreshed?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Reset()
    {
        var answer = MessageBox.Show("Удалить всю статистику и уровни? Это нельзя отменить.",
            "Сброс статистики", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (answer != MessageBoxResult.Yes)
            return;

        _store.Reset();
        Refresh();
    }
}
