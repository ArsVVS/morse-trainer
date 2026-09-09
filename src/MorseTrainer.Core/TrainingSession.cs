using System.Text;

namespace MorseTrainer.Core;

/// <summary>Режим тренировки.</summary>
public enum TrainingMode
{
    // Приём: слушаем сигнал, вводим текст
    Receive,
    // Передача: видим текст, вводим код
    Send
}

/// <summary>Тип заданий.</summary>
public enum TaskKind
{
    Symbols,
    Groups,
    Words
}

/// <summary>Параметры тренировки.</summary>
public class TrainingOptions
{
    public TrainingMode Mode { get; set; } = TrainingMode.Receive;
    public AlphabetKind Alphabet { get; set; } = AlphabetKind.Latin;
    public int Level { get; set; } = 1;
    public TaskKind TaskKind { get; set; } = TaskKind.Groups;
    public int Wpm { get; set; } = 20;
    public int Count { get; set; } = 20;
}

/// <summary>Ошибка в одном символе ответа. Пустая строка – символа нет.</summary>
public record CharError(string Expected, string Actual);

/// <summary>Результат одного задания.</summary>
public class TaskResult
{
    public required string Text { get; init; }
    public required string Code { get; init; }
    public required string Answer { get; init; }
    // Ответ в виде текста (в режиме передачи – расшифрованный код)
    public required string AnswerText { get; init; }
    public required IReadOnlyList<CharError> Errors { get; init; }
    public int CorrectChars { get; init; }
    public int TotalChars { get; init; }
    public int Repeats { get; init; }
    public TimeSpan AnswerTime { get; init; }
    public bool IsCorrect => Errors.Count == 0;
}

/// <summary>Итог сессии.</summary>
public record SessionSummary(
    double Accuracy,
    TimeSpan Duration,
    TimeSpan AverageAnswerTime,
    int CorrectTasks,
    int TotalTasks,
    int Repeats,
    bool CanLevelUp);

/// <summary>Одна тренировка: выдаёт задания, принимает ответы и считает точность.</summary>
public class TrainingSession
{
    // Точность, с которой предлагается следующий уровень
    public const double LevelUpAccuracy = 0.9;

    private readonly List<string> _tasks;
    private readonly List<TaskResult> _results = [];
    private readonly MorseTranslator _translator;
    private readonly DateTime _startTime;
    private DateTime _taskStartTime;
    private DateTime? _endTime;
    private int _repeats;

    public TrainingSession(TrainingOptions options, Random? random = null)
    {
        Options = options;
        _translator = new MorseTranslator(options.Alphabet);
        _tasks = CreateTasks(random ?? Random.Shared);
        _startTime = DateTime.Now;
        _taskStartTime = _startTime;
    }

    public TrainingOptions Options { get; }

    /// <summary>Тип заданий на самом деле (если слов мало, вместо слов даются группы).</summary>
    public TaskKind ActualTaskKind { get; private set; }

    /// <summary>Символы текущего уровня.</summary>
    public string Symbols => KochLevels.GetSymbols(Options.Alphabet, Options.Level);

    public int TaskCount => _tasks.Count;

    public int CurrentIndex { get; private set; }

    public bool IsFinished => CurrentIndex >= _tasks.Count;

    public string CurrentText => _tasks[CurrentIndex];

    public string CurrentCode => _translator.Encode(CurrentText).Result;

    public MorseSignal CurrentSignal => MorseSignal.FromCode(CurrentCode, new MorseTiming(Options.Wpm));

    public IReadOnlyList<TaskResult> Results => _results;

    /// <summary>Точность по всем ответам (от 0 до 1).</summary>
    public double Accuracy
    {
        get
        {
            int total = _results.Sum(r => r.TotalChars);
            return total == 0 ? 0 : (double)_results.Sum(r => r.CorrectChars) / total;
        }
    }

    /// <summary>Отмечает повтор воспроизведения в режиме приёма.</summary>
    public void RegisterRepeat() => _repeats++;

    /// <summary>Принимает ответ на текущее задание и переходит к следующему.</summary>
    public TaskResult SubmitAnswer(string answer)
    {
        if (IsFinished)
            throw new InvalidOperationException("Сессия уже закончена");

        // В режиме передачи ответ – это код, переводим его в текст
        string answerText = Options.Mode == TrainingMode.Send
            ? _translator.Decode(answer).Result
            : answer;

        string expected = Normalize(CurrentText);
        string actual = Normalize(answerText);
        var errors = Compare(expected, actual);
        int total = Math.Max(expected.Length, actual.Length);

        var now = DateTime.Now;
        var result = new TaskResult
        {
            Text = CurrentText,
            Code = CurrentCode,
            Answer = answer,
            AnswerText = actual,
            Errors = errors,
            CorrectChars = total - errors.Count,
            TotalChars = total,
            Repeats = _repeats,
            AnswerTime = now - _taskStartTime
        };

        _results.Add(result);
        CurrentIndex++;
        _repeats = 0;
        _taskStartTime = now;
        if (IsFinished)
            _endTime = now;

        return result;
    }

    /// <summary>Итог сессии.</summary>
    public SessionSummary GetSummary()
    {
        var duration = (_endTime ?? DateTime.Now) - _startTime;
        var average = _results.Count == 0
            ? TimeSpan.Zero
            : TimeSpan.FromSeconds(_results.Average(r => r.AnswerTime.TotalSeconds));
        bool canLevelUp = IsFinished
            && Accuracy >= LevelUpAccuracy
            && Options.Level < KochLevels.GetMaxLevel(Options.Alphabet);

        return new SessionSummary(
            Accuracy,
            duration,
            average,
            _results.Count(r => r.IsCorrect),
            _results.Count,
            _results.Sum(r => r.Repeats),
            canLevelUp);
    }

    /// <summary>Сравнивает строки посимвольно и возвращает список ошибок.</summary>
    public static List<CharError> Compare(string expected, string actual)
    {
        var errors = new List<CharError>();
        int length = Math.Max(expected.Length, actual.Length);
        for (int i = 0; i < length; i++)
        {
            string e = i < expected.Length ? expected[i].ToString() : "";
            string a = i < actual.Length ? actual[i].ToString() : "";
            if (e != a)
                errors.Add(new CharError(e, a));
        }
        return errors;
    }

    // Верхний регистр, Ё как Е, лишние пробелы убраны
    private static string Normalize(string s)
    {
        var parts = s.ToUpperInvariant().Replace('Ё', 'Е')
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", parts);
    }

    private List<string> CreateTasks(Random random)
    {
        string symbols = Symbols;
        ActualTaskKind = Options.TaskKind;

        var words = new List<string>();
        if (Options.TaskKind == TaskKind.Words)
        {
            words = WordList.GetWords(Options.Alphabet).Where(w => w.All(symbols.Contains)).ToList();
            // Если подходящих слов мало – даём группы
            if (words.Count < 5)
                ActualTaskKind = TaskKind.Groups;
        }

        var tasks = new List<string>();
        for (int i = 0; i < Options.Count; i++)
        {
            switch (ActualTaskKind)
            {
                case TaskKind.Symbols:
                    tasks.Add(symbols[random.Next(symbols.Length)].ToString());
                    break;
                case TaskKind.Words:
                    tasks.Add(words[random.Next(words.Count)]);
                    break;
                default:
                    var group = new StringBuilder();
                    for (int j = 0; j < 5; j++)
                        group.Append(symbols[random.Next(symbols.Length)]);
                    tasks.Add(group.ToString());
                    break;
            }
        }
        return tasks;
    }
}
