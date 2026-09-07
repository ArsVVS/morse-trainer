namespace MorseTrainer.Core;

/// <summary>Длительности элементов сигнала для заданной скорости.</summary>
public class MorseTiming
{
    public const int MinWpm = 5;
    public const int MaxWpm = 40;

    public MorseTiming(int wpm)
    {
        if (wpm < MinWpm || wpm > MaxWpm)
            throw new ArgumentOutOfRangeException(nameof(wpm), $"Скорость должна быть от {MinWpm} до {MaxWpm} WPM");
        Wpm = wpm;
    }

    /// <summary>Скорость в словах в минуту.</summary>
    public int Wpm { get; }

    // Длина точки в миллисекундах зависит от скорости (стандарт по слову PARIS)
    public int DotMs => 1200 / Wpm;

    public int DashMs => DotMs * 3;

    // Пауза между точками и тире внутри буквы
    public int ElementGapMs => DotMs;

    public int LetterGapMs => DotMs * 3;

    public int WordGapMs => DotMs * 7;
}
