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
        Translator = new TranslatorViewModel(Settings, Player);
        Reference = new ReferenceViewModel(Settings, Player);
    }

    public SettingsViewModel Settings { get; }

    public MorsePlayer Player { get; }

    public TranslatorViewModel Translator { get; }

    public ReferenceViewModel Reference { get; }

    [ObservableProperty]
    public partial int SelectedTab { get; set; }
}
