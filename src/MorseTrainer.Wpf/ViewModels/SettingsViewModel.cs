using CommunityToolkit.Mvvm.ComponentModel;
using MorseTrainer.Core;

namespace MorseTrainer.Wpf.ViewModels;

/// <summary>Настройки, общие для всех вкладок. Изменения применяются сразу.</summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly AppSettings _settings;

    public SettingsViewModel(AppSettings settings)
    {
        _settings = settings;
        Theme = settings.Theme;
        Alphabet = settings.Alphabet;
        Wpm = settings.Wpm;
        FrequencyHz = settings.FrequencyHz;
        Volume = settings.Volume;
        OutputMode = settings.OutputMode;
        TaskCount = settings.TaskCount;
        LampColor = settings.LampColor;
    }

    public int MinWpm => MorseTiming.MinWpm;
    public int MaxWpm => MorseTiming.MaxWpm;
    public int MinFrequency => WavGenerator.MinFrequency;
    public int MaxFrequency => WavGenerator.MaxFrequency;

    public AppTheme[] Themes { get; } = Enum.GetValues<AppTheme>();
    public AlphabetKind[] Alphabets { get; } = Enum.GetValues<AlphabetKind>();
    public OutputMode[] OutputModes { get; } = Enum.GetValues<OutputMode>();
    public string[] LampColors { get; } = ["#FFC107", "#F44336", "#4CAF50", "#2196F3", "#FFFFFF"];

    [ObservableProperty]
    public partial AppTheme Theme { get; set; }

    [ObservableProperty]
    public partial AlphabetKind Alphabet { get; set; }

    [ObservableProperty]
    public partial int Wpm { get; set; }

    [ObservableProperty]
    public partial int FrequencyHz { get; set; }

    [ObservableProperty]
    public partial double Volume { get; set; }

    [ObservableProperty]
    public partial OutputMode OutputMode { get; set; }

    [ObservableProperty]
    public partial int TaskCount { get; set; }

    [ObservableProperty]
    public partial string LampColor { get; set; }

    // Каждое изменение сразу записываем в объект настроек
    partial void OnThemeChanged(AppTheme value)
    {
        _settings.Theme = value;
        App.ApplyTheme(value);
    }

    partial void OnAlphabetChanged(AlphabetKind value) => _settings.Alphabet = value;

    partial void OnWpmChanged(int value) => _settings.Wpm = value;

    partial void OnFrequencyHzChanged(int value) => _settings.FrequencyHz = value;

    partial void OnVolumeChanged(double value) => _settings.Volume = value;

    partial void OnOutputModeChanged(OutputMode value) => _settings.OutputMode = value;

    partial void OnTaskCountChanged(int value) => _settings.TaskCount = value;

    partial void OnLampColorChanged(string value) => _settings.LampColor = value;

    /// <summary>Изменить скорость на delta WPM (горячие клавиши Ctrl+↑ / Ctrl+↓).</summary>
    public void ChangeWpm(int delta) => Wpm = Math.Clamp(Wpm + delta, MinWpm, MaxWpm);

    public void Save() => _settings.Save();
}
