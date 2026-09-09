using System.Text.Json;
using System.Text.Json.Serialization;

namespace MorseTrainer.Core;

/// <summary>Запись об одной сессии.</summary>
public class SessionRecord
{
    public DateTime Date { get; set; }
    public TrainingMode Mode { get; set; }
    public AlphabetKind Alphabet { get; set; }
    public int Level { get; set; }
    public int Wpm { get; set; }
    public double Accuracy { get; set; }
    public double DurationSeconds { get; set; }
    public int Repeats { get; set; }
}

/// <summary>Статистика по одному символу.</summary>
public class SymbolStat
{
    public int Seen { get; set; }
    public int Errors { get; set; }
    // С какими символами путали и сколько раз
    public Dictionary<string, int> ConfusedWith { get; set; } = [];
}

/// <summary>Всё, что хранится в stats.json.</summary>
public class StatisticsData
{
    public List<SessionRecord> Sessions { get; set; } = [];
    public Dictionary<string, SymbolStat> Symbols { get; set; } = [];
    // Текущий уровень, ключ вида "Latin-Receive"
    public Dictionary<string, int> Levels { get; set; } = [];
}

/// <summary>Проблемный символ для таблицы статистики.</summary>
public record ProblemSymbol(char Symbol, string Code, int Seen, int Errors, double ErrorRate, string ConfusedWith);

/// <summary>Точность за день.</summary>
public record DailyAccuracy(DateTime Day, double Accuracy);

/// <summary>Хранит историю тренировок в JSON-файле.</summary>
public class StatisticsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public StatisticsStore(string? path = null)
    {
        FilePath = path ?? Path.Combine(AppSettings.DataFolder, "stats.json");
        Data = Load();
    }

    public string FilePath { get; }

    public StatisticsData Data { get; private set; }

    /// <summary>Сохраняет итоги сессии и статистику по символам.</summary>
    public void AddSession(TrainingSession session)
    {
        var summary = session.GetSummary();
        Data.Sessions.Add(new SessionRecord
        {
            Date = DateTime.Now,
            Mode = session.Options.Mode,
            Alphabet = session.Options.Alphabet,
            Level = session.Options.Level,
            Wpm = session.Options.Wpm,
            Accuracy = summary.Accuracy,
            DurationSeconds = Math.Round(summary.Duration.TotalSeconds, 1),
            Repeats = summary.Repeats
        });

        foreach (var result in session.Results)
        {
            // Считаем каждый символ задания
            for (int i = 0; i < result.Text.Length; i++)
            {
                string expected = char.ToUpperInvariant(result.Text[i]).ToString();
                if (string.IsNullOrWhiteSpace(expected))
                    continue;

                var stat = GetSymbolStat(expected);
                stat.Seen++;

                string actual = i < result.AnswerText.Length ? result.AnswerText[i].ToString() : "";
                if (actual != expected)
                {
                    stat.Errors++;
                    if (actual != "")
                        stat.ConfusedWith[actual] = stat.ConfusedWith.GetValueOrDefault(actual) + 1;
                }
            }
        }

        Data.Levels[LevelKey(session.Options.Alphabet, session.Options.Mode)] = session.Options.Level;
        Save();
    }

    /// <summary>Средняя точность по дням.</summary>
    public List<DailyAccuracy> GetDailyAccuracy() =>
        Data.Sessions
            .GroupBy(s => s.Date.Date)
            .OrderBy(g => g.Key)
            .Select(g => new DailyAccuracy(g.Key, g.Average(s => s.Accuracy)))
            .ToList();

    /// <summary>Символы, в которых чаще всего ошибались.</summary>
    public List<ProblemSymbol> GetProblemSymbols(int count = 10) =>
        Data.Symbols
            .Where(p => p.Value.Errors > 0)
            .Select(p => new ProblemSymbol(
                p.Key[0],
                MorseAlphabet.GetCode(p.Key[0]) ?? "",
                p.Value.Seen,
                p.Value.Errors,
                (double)p.Value.Errors / p.Value.Seen,
                p.Value.ConfusedWith.OrderByDescending(c => c.Value).Select(c => c.Key).FirstOrDefault() ?? ""))
            .OrderByDescending(p => p.ErrorRate)
            .ThenByDescending(p => p.Errors)
            .Take(count)
            .ToList();

    /// <summary>Текущий уровень для алфавита и режима (по умолчанию 1).</summary>
    public int GetLevel(AlphabetKind alphabet, TrainingMode mode) =>
        Data.Levels.GetValueOrDefault(LevelKey(alphabet, mode), 1);

    public void SetLevel(AlphabetKind alphabet, TrainingMode mode, int level)
    {
        Data.Levels[LevelKey(alphabet, mode)] = level;
        Save();
    }

    /// <summary>Удаляет всю статистику.</summary>
    public void Reset()
    {
        Data = new StatisticsData();
        Save();
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(FilePath))!);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(Data, JsonOptions));
    }

    private StatisticsData Load()
    {
        if (!File.Exists(FilePath))
            return new StatisticsData();

        try
        {
            return JsonSerializer.Deserialize<StatisticsData>(File.ReadAllText(FilePath), JsonOptions) ?? new StatisticsData();
        }
        catch (JsonException)
        {
            // Файл испорчен – начинаем заново
            return new StatisticsData();
        }
    }

    private SymbolStat GetSymbolStat(string symbol)
    {
        if (!Data.Symbols.TryGetValue(symbol, out var stat))
        {
            stat = new SymbolStat();
            Data.Symbols[symbol] = stat;
        }
        return stat;
    }

    private static string LevelKey(AlphabetKind alphabet, TrainingMode mode) => $"{alphabet}-{mode}";
}
