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

    // Номера вкладок
    public const int TranslatorTab = 0;
    public const int TrainerTab = 1;
    public const int StatisticsTab = 2;
    public const int ReferenceTab = 3;

    /// <summary>Ctrl+Enter – воспроизвести звук и свет на текущей вкладке.</summary>
    public void PlayCurrent()
    {
        if (SelectedTab == TranslatorTab)
            Translator.PlayBothCommand.Execute(null);
        else if (SelectedTab == TrainerTab)
            Trainer.Repeat();
        else if (SelectedTab == ReferenceTab)
            Reference.PlaySelectedCommand.Execute(null);
    }

    /// <summary>Ctrl+S – сохранить WAV из переводчика.</summary>
    public void SaveWav()
    {
        if (SelectedTab == TranslatorTab)
            Translator.SaveWavCommand.Execute(null);
    }

    /// <summary>Ctrl+R – повторить задание в режиме приёма.</summary>
    public void Repeat()
    {
        if (SelectedTab == TrainerTab)
            Trainer.Repeat();
    }
}
