using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MorseTrainer.Core;
using MorseTrainer.Wpf.Services;

namespace MorseTrainer.Wpf.ViewModels;

/// <summary>Строка таблицы справочника.</summary>
public record ReferenceRow(char Symbol, string Code)
{
    // Код покрупнее, с точками посередине строки
    public string PrettyCode => Code.Replace('.', '·').Replace('-', '–');
}

/// <summary>Вкладка «Справочник».</summary>
public partial class ReferenceViewModel : ObservableObject
{
    private readonly MorsePlayer _player;

    public ReferenceViewModel(SettingsViewModel settings, MorsePlayer player)
    {
        Settings = settings;
        _player = player;
        Alphabet = settings.Alphabet;
        OnAlphabetChanged(Alphabet);
    }

    public SettingsViewModel Settings { get; }

    public AlphabetKind[] Alphabets { get; } = Enum.GetValues<AlphabetKind>();

    [ObservableProperty]
    public partial AlphabetKind Alphabet { get; set; }

    [ObservableProperty]
    public partial List<ReferenceRow> Rows { get; set; } = [];

    [ObservableProperty]
    public partial ReferenceRow? SelectedRow { get; set; }

    [ObservableProperty]
    public partial bool IsLampOn { get; set; }

    partial void OnAlphabetChanged(AlphabetKind value)
    {
        Rows = MorseAlphabet.GetTable(value).Select(p => new ReferenceRow(p.Key, p.Value)).ToList();
    }

    /// <summary>Проигрывает выбранный символ способом из настроек.</summary>
    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task PlaySelected()
    {
        if (SelectedRow == null)
            return;

        var signal = MorseSignal.FromCode(SelectedRow.Code, new MorseTiming(Settings.Wpm));
        bool sound = Settings.OutputMode != OutputMode.Light;
        bool light = Settings.OutputMode != OutputMode.Sound;
        await _player.PlayAsync(signal, sound, light, Settings.FrequencyHz, Settings.Volume,
            (lampOn, _) => IsLampOn = lampOn);
    }
}
