using CommunityToolkit.Mvvm.ComponentModel;
using MorseTrainer.Core;
using MorseTrainer.Wpf.Services;

namespace MorseTrainer.Wpf.ViewModels;

/// <summary>Главное окно: вкладки и общие объекты.</summary>
public partial class MainViewModel : ObservableObject
{
    public MainViewModel(AppSettings settings)
    {
        Settings = new SettingsViewModel(settings);
        Player = new MorsePlayer();
        Store = new StatisticsStore();
        Translator = new TranslatorViewModel(Settings, Player);
        Trainer = new TrainerViewModel(Settings, Player, Store);
        Statistics = new StatisticsViewModel(Settings, Store);
        Reference = new ReferenceViewModel(Settings, Player);

        // После тренировки обновляем вкладку статистики
        Trainer.SessionSaved += (_, _) => Statistics.Refresh();
    }

    public SettingsViewModel Settings { get; }

    public MorsePlayer Player { get; }

    public StatisticsStore Store { get; }

    public TranslatorViewModel Translator { get; }

    public TrainerViewModel Trainer { get; }

    public StatisticsViewModel Statistics { get; }

    public ReferenceViewModel Reference { get; }

    [ObservableProperty]
    public partial int SelectedTab { get; set; }
}
