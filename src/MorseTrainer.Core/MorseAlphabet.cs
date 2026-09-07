namespace MorseTrainer.Core;

/// <summary>Алфавит: латиница или кириллица.</summary>
public enum AlphabetKind
{
    Latin,
    Cyrillic
}

/// <summary>Таблицы кодов Морзе для букв, цифр и знаков.</summary>
public static class MorseAlphabet
{
    public static readonly IReadOnlyDictionary<char, string> Latin = new Dictionary<char, string>
    {
        ['A'] = ".-", ['B'] = "-...", ['C'] = "-.-.", ['D'] = "-..", ['E'] = ".",
        ['F'] = "..-.", ['G'] = "--.", ['H'] = "....", ['I'] = "..", ['J'] = ".---",
        ['K'] = "-.-", ['L'] = ".-..", ['M'] = "--", ['N'] = "-.", ['O'] = "---",
        ['P'] = ".--.", ['Q'] = "--.-", ['R'] = ".-.", ['S'] = "...", ['T'] = "-",
        ['U'] = "..-", ['V'] = "...-", ['W'] = ".--", ['X'] = "-..-", ['Y'] = "-.--",
        ['Z'] = "--.."
    };

    public static readonly IReadOnlyDictionary<char, string> Cyrillic = new Dictionary<char, string>
    {
        ['А'] = ".-", ['Б'] = "-...", ['В'] = ".--", ['Г'] = "--.", ['Д'] = "-..",
        ['Е'] = ".", ['Ж'] = "...-", ['З'] = "--..", ['И'] = "..", ['Й'] = ".---",
        ['К'] = "-.-", ['Л'] = ".-..", ['М'] = "--", ['Н'] = "-.", ['О'] = "---",
        ['П'] = ".--.", ['Р'] = ".-.", ['С'] = "...", ['Т'] = "-", ['У'] = "..-",
        ['Ф'] = "..-.", ['Х'] = "....", ['Ц'] = "-.-.", ['Ч'] = "---.", ['Ш'] = "----",
        ['Щ'] = "--.-", ['Ъ'] = "--.--", ['Ы'] = "-.--", ['Ь'] = "-..-", ['Э'] = "..-..",
        ['Ю'] = "..--", ['Я'] = ".-.-"
    };

    public static readonly IReadOnlyDictionary<char, string> Digits = new Dictionary<char, string>
    {
        ['0'] = "-----", ['1'] = ".----", ['2'] = "..---", ['3'] = "...--", ['4'] = "....-",
        ['5'] = ".....", ['6'] = "-....", ['7'] = "--...", ['8'] = "---..", ['9'] = "----."
    };

    public static readonly IReadOnlyDictionary<char, string> Punctuation = new Dictionary<char, string>
    {
        ['.'] = ".-.-.-", [','] = "--..--", ['?'] = "..--..", ['!'] = "-.-.--",
        ['/'] = "-..-.", ['-'] = "-....-", ['('] = "-.--.", [')'] = "-.--.-",
        [':'] = "---...", [';'] = "-.-.-.", ['='] = "-...-", ['+'] = ".-.-.",
        ['@'] = ".--.-.", ['"'] = ".-..-."
    };

    /// <summary>Возвращает код символа или null, если символ неизвестен.</summary>
    public static string? GetCode(char symbol)
    {
        char c = char.ToUpperInvariant(symbol);
        // Ё передаётся так же, как Е
        if (c == 'Ё')
            c = 'Е';

        if (Latin.TryGetValue(c, out var code) || Cyrillic.TryGetValue(c, out code)
            || Digits.TryGetValue(c, out code) || Punctuation.TryGetValue(c, out code))
            return code;

        return null;
    }

    /// <summary>Возвращает символ по коду или null, если такого кода нет.</summary>
    public static char? GetSymbol(string code, AlphabetKind alphabet)
    {
        // Сначала ищем в выбранном алфавите, потом цифры и знаки, потом в другом алфавите
        var other = alphabet == AlphabetKind.Latin ? Cyrillic : Latin;
        foreach (var table in new[] { GetLetters(alphabet), Digits, Punctuation, other })
        {
            foreach (var pair in table)
            {
                if (pair.Value == code)
                    return pair.Key;
            }
        }
        return null;
    }

    /// <summary>Буквы выбранного алфавита.</summary>
    public static IReadOnlyDictionary<char, string> GetLetters(AlphabetKind alphabet) =>
        alphabet == AlphabetKind.Latin ? Latin : Cyrillic;

    /// <summary>Полная таблица для справочника: буквы, цифры и знаки.</summary>
    public static IEnumerable<KeyValuePair<char, string>> GetTable(AlphabetKind alphabet) =>
        GetLetters(alphabet).Concat(Digits).Concat(Punctuation);
}
