using System.Diagnostics;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MorseTrainer.Core;
using MorseTrainer.Wpf.Services;

namespace MorseTrainer.Wpf.ViewModels;

/// <summary>Вкладка «Тренажёр».</summary>
public partial class TrainerViewModel : ObservableObject
{
    private readonly MorsePlayer _player;
    private readonly StatisticsStore _store;
    private readonly KeyingDecoder _decoder = new(20);
    private readonly Stopwatch _keyClock = Stopwatch.StartNew();
    private readonly DispatcherTimer _keyTimer;
    private TrainingSession? _session;
    private bool _waitingNext;

    public TrainerViewModel(SettingsViewModel settings, MorsePlayer player, StatisticsStore store)
    {
        Settings = settings;
        _player = player;
        _store = store;

        Alphabet = settings.Alphabet;
        OutputMode = settings.OutputMode;
        Level = _store.GetLevel(Alphabet, Mode);
        UpdateLevelInfo();

        // Таймер нужен, чтобы замечать конец буквы и слова во время паузы
        _keyTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(20) };
        _keyTimer.Tick += (_, _) => _decoder.Tick(_keyClock.Elapsed);
        _decoder.SymbolDecoded += (_, _) => LiveCode = _decoder.Code;
    }

    /// <summary>Сессия закончена и сохранена в статистику.</summary>
    public event EventHandler? SessionSaved;

    /// <summary>Показано новое задание (окно ставит фокус в поле ответа).</summary>
    public event EventHandler? TaskShown;

    public SettingsViewModel Settings { get; }

    public TrainingMode[] Modes { get; } = Enum.GetValues<TrainingMode>();
    public AlphabetKind[] Alphabets { get; } = Enum.GetValues<AlphabetKind>();
    public TaskKind[] TaskKinds { get; } = Enum.GetValues<TaskKind>();
    public OutputMode[] OutputModes { get; } = Enum.GetValues<OutputMode>();

    // ---- Параметры ----

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsReceive), nameof(IsKeyMode), nameof(IsTypingMode))]
    public partial TrainingMode Mode { get; set; }

    [ObservableProperty]
    public partial AlphabetKind Alphabet { get; set; }

    [ObservableProperty]
    public partial int Level { get; set; } = 1;

    [ObservableProperty]
    public partial int MaxLevel { get; set; }

    [ObservableProperty]
    public partial string LevelSymbols { get; set; } = "";

    [ObservableProperty]
    public partial TaskKind TaskKind { get; set; } = TaskKind.Groups;

    [ObservableProperty]
    public partial OutputMode OutputMode { get; set; }

    // Ввод в передаче: false – печатать точки и тире, true – ключ пробелом
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsKeyMode), nameof(IsTypingMode))]
    public partial bool UseKey { get; set; }

    // ---- Состояние сессии ----

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle), nameof(IsKeyMode), nameof(IsTypingMode))]
    public partial bool IsRunning { get; set; }

    [ObservableProperty]
    public partial bool ShowSummary { get; set; }

    [ObservableProperty]
    public partial string Progress { get; set; } = "";

    [ObservableProperty]
    public partial double ProgressValue { get; set; }

    [ObservableProperty]
    public partial string AccuracyText { get; set; } = "";

    [ObservableProperty]
    public partial string Prompt { get; set; } = "Выберите параметры и нажмите «Начать»";

    [ObservableProperty]
    public partial string Answer { get; set; } = "";

    [ObservableProperty]
    public partial string Feedback { get; set; } = "";

    [ObservableProperty]
    public partial bool FeedbackIsCorrect { get; set; }

    [ObservableProperty]
    public partial bool IsLampOn { get; set; }

    [ObservableProperty]
    public partial bool IsKeyDown { get; set; }

    [ObservableProperty]
    public partial string LiveCode { get; set; } = "";

    [ObservableProperty]
    public partial string SummaryText { get; set; } = "";

    [ObservableProperty]
    public partial List<string> SummaryErrors { get; set; } = [];

    [ObservableProperty]
    public partial bool CanLevelUp { get; set; }

    public bool IsIdle => !IsRunning;

    public bool IsReceive => Mode == TrainingMode.Receive;

    public bool IsKeyMode => IsRunning && Mode == TrainingMode.Send && UseKey;

    public bool IsTypingMode => IsRunning && !IsKeyMode;

    partial void OnModeChanged(TrainingMode value) => LoadLevel();

    partial void OnAlphabetChanged(AlphabetKind value) => LoadLevel();

    partial void OnLevelChanged(int value) => UpdateLevelInfo();

    // Текущий уровень берём из статистики
    private void LoadLevel()
    {
        MaxLevel = KochLevels.GetMaxLevel(Alphabet);
        Level = Math.Min(_store.GetLevel(Alphabet, Mode), MaxLevel);
        UpdateLevelInfo();
    }

    private void UpdateLevelInfo()
    {
        MaxLevel = KochLevels.GetMaxLevel(Alphabet);
        LevelSymbols = string.Join(" ", KochLevels.GetSymbols(Alphabet, Level).ToCharArray());
    }

    [RelayCommand]
    private void Start()
    {
        var options = new TrainingOptions
        {
            Mode = Mode,
            Alphabet = Alphabet,
            Level = Level,
            TaskKind = TaskKind,
            Wpm = Settings.Wpm,
            Count = Settings.TaskCount
        };
        _session = new TrainingSession(options);
        _decoder.Wpm = Settings.Wpm;

        ShowSummary = false;
        Feedback = "";
        AccuracyText = "";
        IsRunning = true;
        if (_session.ActualTaskKind != TaskKind)
            Feedback = "Слов из букв этого уровня мало – будут группы символов.";

        ShowTask();
    }

    [RelayCommand]
    private void Stop()
    {
        _player.Stop();
        _keyTimer.Stop();
        _session = null;
        IsRunning = false;
        Prompt = "Сессия прервана. Результаты не сохранены.";
    }

    private void ShowTask()
    {
        if (_session == null)
            return;

        Answer = "";
        LiveCode = "";
        _decoder.Reset();
        Progress = $"Задание {_session.CurrentIndex + 1} / {_session.TaskCount}";
        ProgressValue = 100.0 * _session.CurrentIndex / _session.TaskCount;

        if (IsKeyMode)
            _keyTimer.Start();
        else
            _keyTimer.Stop();

        if (Mode == TrainingMode.Receive)
        {
            Prompt = "Слушайте и введите текст";
            _ = PlayCurrent();
        }
        else
        {
            Prompt = _session.CurrentText;
        }

        TaskShown?.Invoke(this, EventArgs.Empty);
    }

    private async Task PlayCurrent()
    {
        if (_session == null || _session.IsFinished)
            return;

        bool sound = OutputMode != OutputMode.Light;
        bool light = OutputMode != OutputMode.Sound;
        await _player.PlayAsync(_session.CurrentSignal, sound, light, Settings.FrequencyHz, Settings.Volume,
            (lampOn, _) => IsLampOn = lampOn);
    }

    /// <summary>Повторить сигнал (только в приёме).</summary>
    [RelayCommand]
    public void Repeat()
    {
        if (_session == null || !IsRunning || Mode != TrainingMode.Receive || _waitingNext)
            return;

        _session.RegisterRepeat();
        _ = PlayCurrent();
    }

    [RelayCommand]
    private async Task Submit()
    {
        if (_session == null || !IsRunning || _waitingNext)
            return;

        _player.Stop();
        string answer = IsKeyMode ? _decoder.Code : Answer;
        var result = _session.SubmitAnswer(answer);

        FeedbackIsCorrect = result.IsCorrect;
        Feedback = result.IsCorrect
            ? $"Правильно: {result.Text}   {result.Code}"
            : $"Неправильно. Нужно: {result.Text}   {result.Code}\nВаш ответ: {(result.AnswerText.Length > 0 ? result.AnswerText : "—")}";
        AccuracyText = $"Точность: {_session.Accuracy:P0}";
        ProgressValue = 100.0 * _session.CurrentIndex / _session.TaskCount;

        if (_session.IsFinished)
        {
            Finish();
            return;
        }

        // Небольшая пауза, чтобы успеть прочитать результат
        _waitingNext = true;
        await Task.Delay(Mode == TrainingMode.Receive ? 800 : 300);
        _waitingNext = false;
        if (IsRunning)
            ShowTask();
    }

    private void Finish()
    {
        if (_session == null)
            return;

        _keyTimer.Stop();
        IsRunning = false;
        _store.AddSession(_session);

        var summary = _session.GetSummary();
        Prompt = "Сессия завершена";
        Progress = $"Готово: {summary.TotalTasks} / {summary.TotalTasks}";
        SummaryText =
            $"Точность: {summary.Accuracy:P0}\n" +
            $"Правильных заданий: {summary.CorrectTasks} из {summary.TotalTasks}\n" +
            $"Время: {summary.Duration:mm\\:ss}, в среднем на ответ {summary.AverageAnswerTime.TotalSeconds:F1} с\n" +
            $"Повторов: {summary.Repeats}";
        SummaryErrors = _session.Results
            .Where(r => !r.IsCorrect)
            .Select(r => $"{r.Text}  ({r.Code})  →  {(r.AnswerText.Length > 0 ? r.AnswerText : "—")}")
            .ToList();
        CanLevelUp = summary.CanLevelUp;
        ShowSummary = true;

        SessionSaved?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Переход на следующий уровень после подтверждения.</summary>
    [RelayCommand]
    private void LevelUp()
    {
        if (!CanLevelUp)
            return;

        _store.SetLevel(Alphabet, Mode, Level + 1);
        Level++;
        CanLevelUp = false;
        SummaryText += $"\nНовый уровень: {Level}";
        SessionSaved?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void ClearKey()
    {
        _decoder.Reset();
        LiveCode = "";
    }

    // Нажатия ключа приходят из окна (пробел)
    public void KeyDown()
    {
        if (!IsKeyMode || IsKeyDown)
            return;
        IsKeyDown = true;
        _decoder.KeyDown(_keyClock.Elapsed);
    }

    public void KeyUp()
    {
        if (!IsKeyDown)
            return;
        IsKeyDown = false;
        _decoder.KeyUp(_keyClock.Elapsed);
    }
}
