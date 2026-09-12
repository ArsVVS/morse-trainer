using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using MorseTrainer.Core;
using MorseTrainer.Wpf.Services;

namespace MorseTrainer.Wpf.ViewModels;

/// <summary>Вкладка «Переводчик».</summary>
public partial class TranslatorViewModel : ObservableObject
{
    private readonly MorsePlayer _player;
    private readonly MorseImageRenderer _renderer = new();
    private IReadOnlyList<int> _letterIndexes = [];
    private bool _updating;
    private bool _lastEditWasCode;

    public TranslatorViewModel(SettingsViewModel settings, MorsePlayer player)
    {
        Settings = settings;
        _player = player;
        Settings.PropertyChanged += OnSettingsChanged;
        Text = "SOS";
    }

    public SettingsViewModel Settings { get; }

    [ObservableProperty]
    public partial string Text { get; set; } = "";

    [ObservableProperty]
    public partial string Code { get; set; } = "";

    [ObservableProperty]
    public partial string Warnings { get; set; } = "";

    [ObservableProperty]
    public partial DrawingImage? Image { get; set; }

    [ObservableProperty]
    public partial bool IsLampOn { get; set; }

    // Индекс подсвеченного символа в поле «Текст» (-1 – ничего)
    [ObservableProperty]
    public partial int HighlightIndex { get; set; } = -1;

    [ObservableProperty]
    public partial string SoundInfo { get; set; } = "";

    partial void OnTextChanged(string value)
    {
        if (_updating)
            return;

        // Если в поле текста вставили код Морзе – переводим его как код
        if (MorseTranslator.LooksLikeCode(value) && value.Trim().Contains(' '))
        {
            SetCode(value);
            UpdateFromCode();
        }
        else
        {
            UpdateFromText();
        }
    }

    partial void OnCodeChanged(string value)
    {
        if (!_updating)
            UpdateFromCode();
    }

    private void UpdateFromText()
    {
        _lastEditWasCode = false;
        var result = new MorseTranslator(Settings.Alphabet).Encode(Text);
        SetCode(result.Result);
        ShowResult(result);
    }

    private void UpdateFromCode()
    {
        _lastEditWasCode = true;
        var result = new MorseTranslator(Settings.Alphabet).Decode(Code);
        _updating = true;
        Text = result.Result;
        _updating = false;
        ShowResult(result);
    }

    private void SetCode(string code)
    {
        _updating = true;
        Code = code;
        _updating = false;
    }

    private void ShowResult(TranslationResult result)
    {
        _letterIndexes = result.LetterIndexes;
        Warnings = string.Join(Environment.NewLine, result.Warnings);
        RenderImage(-1);
        UpdateSoundInfo();
    }

    private void RenderImage(int highlightLetter)
    {
        Image = SvgImage.FromSvg(_renderer.Render(Code, Settings.Alphabet, highlightLetter));
    }

    private void UpdateSoundInfo()
    {
        var signal = CreateSignal();
        SoundInfo = $"Тон {Settings.FrequencyHz} Гц, скорость {Settings.Wpm} WPM, длительность {signal.TotalMs / 1000.0:F1} с";
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Алфавит влияет на расшифровку и подписи на картинке
        if (e.PropertyName == nameof(SettingsViewModel.Alphabet))
        {
            if (_lastEditWasCode)
                UpdateFromCode();
            else
                RenderImage(-1);
        }
        UpdateSoundInfo();
    }

    private MorseSignal CreateSignal() =>
        MorseSignal.FromCode(Code, new MorseTiming(Settings.Wpm), _letterIndexes);

    [RelayCommand]
    private void InsertCode(string symbol)
    {
        Code += symbol;
    }

    [RelayCommand]
    private Task PlaySound() => Play(true, false);

    [RelayCommand]
    private Task PlayLight() => Play(false, true);

    [RelayCommand]
    private Task PlayBoth() => Play(true, true);

    [RelayCommand]
    private void Stop() => _player.Stop();

    private async Task Play(bool sound, bool light)
    {
        if (string.IsNullOrWhiteSpace(Code))
            return;

        try
        {
            await _player.PlayAsync(CreateSignal(), sound, light, Settings.FrequencyHz, Settings.Volume, OnPlayerChanged);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Не удалось воспроизвести звук: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // Лампа и подсветка текущей буквы в тексте и на картинке
    private void OnPlayerChanged(bool lampOn, int charIndex)
    {
        IsLampOn = lampOn;
        if (charIndex == HighlightIndex)
            return;

        HighlightIndex = charIndex;
        RenderImage(charIndex < 0 ? -1 : _letterIndexes.ToList().IndexOf(charIndex));
    }

    [RelayCommand]
    private void SaveWav()
    {
        if (string.IsNullOrWhiteSpace(Code))
            return;

        string? path = AskFileName("WAV-файл (*.wav)|*.wav", "morse.wav");
        if (path != null)
            SaveFile(() => new WavGenerator(Settings.FrequencyHz, Settings.Volume).Save(CreateSignal(), path));
    }

    [RelayCommand]
    private void SaveSvg()
    {
        string? path = AskFileName("SVG-картинка (*.svg)|*.svg", "morse.svg");
        if (path != null)
            SaveFile(() => File.WriteAllText(path, _renderer.Render(Code, Settings.Alphabet)));
    }

    [RelayCommand]
    private void SavePng()
    {
        string? path = AskFileName("PNG-картинка (*.png)|*.png", "morse.png");
        if (path != null)
            SaveFile(() => SvgImage.SavePng(SvgImage.FromSvg(_renderer.Render(Code, Settings.Alphabet)), path));
    }

    private static string? AskFileName(string filter, string fileName)
    {
        var dialog = new SaveFileDialog { Filter = filter, FileName = fileName };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    private static void SaveFile(Action save)
    {
        try
        {
            save();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show($"Не удалось сохранить файл: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
