using System.Text.Json;
using System.Text.Json.Serialization;

namespace MorseTrainer.Core;

/// <summary>Тема оформления.</summary>
public enum AppTheme
{
    Light,
    Dark
}

/// <summary>Способ вывода сигнала.</summary>
public enum OutputMode
{
    Sound,
    Light,
    Both
}

/// <summary>Настройки приложения, хранятся в settings.json.</summary>
public class AppSettings
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>Папка %APPDATA%/MorseTrainer.</summary>
    public static string DataFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MorseTrainer");

    public static string DefaultPath => Path.Combine(DataFolder, "settings.json");

    public AppTheme Theme { get; set; } = AppTheme.Light;
    public AlphabetKind Alphabet { get; set; } = AlphabetKind.Latin;
    public int Wpm { get; set; } = 20;
    public int FrequencyHz { get; set; } = 700;
    public double Volume { get; set; } = 0.7;
    public OutputMode OutputMode { get; set; } = OutputMode.Both;
    public int TaskCount { get; set; } = 20;
    public string LampColor { get; set; } = "#FFC107";

    /// <summary>Загружает настройки из файла. Если файла нет – настройки по умолчанию.</summary>
    public static AppSettings Load(string? path = null)
    {
        path ??= DefaultPath;
        if (!File.Exists(path))
            return new AppSettings();

        try
        {
            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path), JsonOptions) ?? new AppSettings();
            settings.Fix();
            return settings;
        }
        catch (JsonException)
        {
            return new AppSettings();
        }
    }

    /// <summary>Сохраняет настройки в файл.</summary>
    public void Save(string? path = null)
    {
        path ??= DefaultPath;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, JsonSerializer.Serialize(this, JsonOptions));
    }

    // Возвращает значения в допустимые границы, если файл правили руками
    private void Fix()
    {
        Wpm = Math.Clamp(Wpm, MorseTiming.MinWpm, MorseTiming.MaxWpm);
        FrequencyHz = Math.Clamp(FrequencyHz, WavGenerator.MinFrequency, WavGenerator.MaxFrequency);
        Volume = Math.Clamp(Volume, 0, 1);
        TaskCount = Math.Clamp(TaskCount, 1, 100);
    }
}
