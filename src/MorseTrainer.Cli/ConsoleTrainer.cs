using MorseTrainer.Core;

namespace MorseTrainer.Cli;

/// <summary>Тренировка приёма и передачи в консоли.</summary>
public class ConsoleTrainer
{
    private readonly AppSettings _settings;
    private readonly StatisticsStore _store = new();

    public ConsoleTrainer(AppSettings settings)
    {
        _settings = settings;
    }

    public int Run(TrainingOptions options, int? level)
    {
        options.Level = level ?? _store.GetLevel(options.Alphabet, options.Mode);
        options.Level = Math.Clamp(options.Level, 1, KochLevels.GetMaxLevel(options.Alphabet));

        var session = new TrainingSession(options);
        bool receive = options.Mode == TrainingMode.Receive;

        Console.WriteLine($"Режим: {(receive ? "приём" : "передача")}, уровень {options.Level}, символы: {session.Symbols}");
        Console.WriteLine(receive
            ? "Слушайте и вводите текст. Пустая строка – повторить сигнал."
            : "Вводите код точками и тире, буквы через пробел.");
        Console.WriteLine();

        while (!session.IsFinished)
        {
            Console.Write($"[{session.CurrentIndex + 1}/{session.TaskCount}] ");
            string? answer;

            if (receive)
            {
                Console.WriteLine("Слушайте...");
                Play(session.CurrentSignal);
                answer = ReadLine("Ответ: ");

                // Пустая строка – повтор сигнала
                while (answer == "")
                {
                    session.RegisterRepeat();
                    Play(session.CurrentSignal);
                    answer = ReadLine("Ответ: ");
                }
            }
            else
            {
                Console.WriteLine($"Передайте: {session.CurrentText}");
                answer = ReadLine("Код: ");
            }

            // Ввод закончился (например, Ctrl+Z) – прерываем тренировку
            if (answer == null)
            {
                Console.Error.WriteLine("Тренировка прервана, результаты не сохранены.");
                return 2;
            }

            var result = session.SubmitAnswer(answer);
            if (result.IsCorrect)
                Console.WriteLine("Правильно!");
            else
                Console.WriteLine($"Ошибка. Нужно: {result.Text} ({result.Code}), введено: {result.AnswerText}");
            Console.WriteLine();
        }

        var summary = session.GetSummary();
        Console.WriteLine("Итоги:");
        Console.WriteLine($"  Точность: {summary.Accuracy:P0}");
        Console.WriteLine($"  Правильных заданий: {summary.CorrectTasks} из {summary.TotalTasks}");
        Console.WriteLine($"  Время: {summary.Duration:mm\\:ss}, в среднем на ответ {summary.AverageAnswerTime.TotalSeconds:F1} с");
        if (summary.Repeats > 0)
            Console.WriteLine($"  Повторов: {summary.Repeats}");

        _store.AddSession(session);

        if (summary.CanLevelUp)
        {
            string yes = ReadLine($"Точность больше 90%. Перейти на уровень {options.Level + 1}? (y/n): ") ?? "";
            if (yes.Equals("y", StringComparison.OrdinalIgnoreCase) || yes.Equals("д", StringComparison.OrdinalIgnoreCase))
            {
                _store.SetLevel(options.Alphabet, options.Mode, options.Level + 1);
                Console.WriteLine($"Новый уровень: {options.Level + 1}");
            }
        }

        return 0;
    }

    /// <summary>Выводит краткую статистику.</summary>
    public static void PrintStatistics(StatisticsStore store)
    {
        var sessions = store.Data.Sessions;
        if (sessions.Count == 0)
        {
            Console.WriteLine("Статистики пока нет. Запустите morse train.");
            return;
        }

        Console.WriteLine($"Сессий: {sessions.Count}, средняя точность: {sessions.Average(s => s.Accuracy):P0}");
        Console.WriteLine();
        Console.WriteLine("Последние сессии:");
        foreach (var s in sessions.TakeLast(5).Reverse())
            Console.WriteLine($"  {s.Date:dd.MM.yyyy HH:mm}  {ModeName(s.Mode),-8} {s.Alphabet,-8} ур. {s.Level,-3} {s.Wpm} WPM  {s.Accuracy:P0}");

        var problems = store.GetProblemSymbols();
        if (problems.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Проблемные символы:");
            foreach (var p in problems)
            {
                string confused = p.ConfusedWith.Length > 0 ? $", путается с {p.ConfusedWith}" : "";
                Console.WriteLine($"  {p.Symbol}  {p.Code,-7} ошибок {p.ErrorRate:P0} ({p.Errors} из {p.Seen}){confused}");
            }
        }

        Console.WriteLine();
        Console.WriteLine("Текущие уровни:");
        foreach (var alphabet in Enum.GetValues<AlphabetKind>())
        {
            foreach (var mode in Enum.GetValues<TrainingMode>())
                Console.WriteLine($"  {alphabet,-8} {ModeName(mode),-8} {store.GetLevel(alphabet, mode)}");
        }
    }

    private void Play(MorseSignal signal)
    {
        try
        {
            ConsolePlayer.Play(signal, _settings.FrequencyHz, _settings.Volume);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Не удалось воспроизвести звук: {ex.Message}");
        }
    }

    private static string? ReadLine(string prompt)
    {
        Console.Write(prompt);
        return Console.ReadLine()?.Trim();
    }

    private static string ModeName(TrainingMode mode) => mode == TrainingMode.Receive ? "приём" : "передача";
}
