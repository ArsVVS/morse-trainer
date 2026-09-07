namespace MorseTrainer.Core;

/// <summary>Один элемент сигнала: тон или пауза.</summary>
/// <param name="IsOn">true – тон (лампа горит), false – пауза.</param>
/// <param name="DurationMs">Длительность в миллисекундах.</param>
/// <param name="CharIndex">Индекс символа в исходном тексте.</param>
public readonly record struct SignalElement(bool IsOn, int DurationMs, int CharIndex);

/// <summary>Сигнал Морзе – последовательность тонов и пауз. Используется для звука и мерцания.</summary>
public class MorseSignal
{
    private MorseSignal(List<SignalElement> elements)
    {
        Elements = elements;
        TotalMs = elements.Sum(e => e.DurationMs);
    }

    public IReadOnlyList<SignalElement> Elements { get; }

    /// <summary>Общая длительность в миллисекундах.</summary>
    public int TotalMs { get; }

    /// <summary>Строит сигнал из кода. letterIndexes – индексы символов текста для каждой буквы кода.</summary>
    public static MorseSignal FromCode(string code, MorseTiming timing, IReadOnlyList<int>? letterIndexes = null)
    {
        var elements = new List<SignalElement>();
        var letters = MorseTranslator.SplitCode(code);
        int prevIndex = 0;

        for (int n = 0; n < letters.Count; n++)
        {
            var letter = letters[n];

            // Если индексы не переданы, считаем их как в расшифрованном тексте (буквы + пробел между словами)
            int charIndex = letterIndexes != null && n < letterIndexes.Count
                ? letterIndexes[n]
                : n + letter.Word;

            // Пауза перед буквой: между буквами или между словами
            if (n > 0)
            {
                int gap = letter.Word != letters[n - 1].Word ? timing.WordGapMs : timing.LetterGapMs;
                elements.Add(new SignalElement(false, gap, prevIndex));
            }

            bool first = true;
            foreach (char c in letter.Code)
            {
                if (c != '.' && c != '-')
                    continue;

                if (!first)
                    elements.Add(new SignalElement(false, timing.ElementGapMs, charIndex));
                elements.Add(new SignalElement(true, c == '.' ? timing.DotMs : timing.DashMs, charIndex));
                first = false;
            }

            prevIndex = charIndex;
        }

        return new MorseSignal(elements);
    }

    /// <summary>Строит сигнал сразу из текста.</summary>
    public static MorseSignal FromText(string text, MorseTiming timing)
    {
        var result = new MorseTranslator().Encode(text);
        return FromCode(result.Result, timing, result.LetterIndexes);
    }

    /// <summary>Возвращает номер элемента, который звучит в момент timeMs, или -1, если сигнал закончился.</summary>
    public int FindElement(int timeMs)
    {
        int start = 0;
        for (int i = 0; i < Elements.Count; i++)
        {
            start += Elements[i].DurationMs;
            if (timeMs < start)
                return i;
        }
        return -1;
    }
}
