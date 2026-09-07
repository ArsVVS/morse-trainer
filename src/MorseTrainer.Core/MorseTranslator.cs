using System.Text;

namespace MorseTrainer.Core;

/// <summary>Результат перевода: строка, предупреждения и индексы букв в тексте.</summary>
/// <param name="Result">Код (для Encode) или текст (для Decode).</param>
/// <param name="Warnings">Сообщения о неизвестных символах.</param>
/// <param name="LetterIndexes">Для каждой буквы кода – индекс её символа в тексте.</param>
public record TranslationResult(string Result, IReadOnlyList<string> Warnings, IReadOnlyList<int> LetterIndexes);

/// <summary>Перевод текста в код Морзе и обратно.</summary>
public class MorseTranslator
{
    // Одна буква кода: сама комбинация, позиция в строке и номер слова
    internal record CodeLetter(string Code, int Position, int Word);

    public MorseTranslator(AlphabetKind alphabet = AlphabetKind.Latin)
    {
        Alphabet = alphabet;
    }

    /// <summary>Алфавит, который используется при расшифровке.</summary>
    public AlphabetKind Alphabet { get; set; }

    /// <summary>Переводит текст в код Морзе.</summary>
    public TranslationResult Encode(string text)
    {
        var words = new List<string>();
        var letters = new List<string>();
        var warnings = new List<string>();
        var indexes = new List<int>();

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (char.IsWhiteSpace(c))
            {
                // Конец слова
                if (letters.Count > 0)
                {
                    words.Add(string.Join(" ", letters));
                    letters.Clear();
                }
                continue;
            }

            string? code = MorseAlphabet.GetCode(c);
            if (code == null)
            {
                warnings.Add($"Неизвестный символ «{c}» (позиция {i + 1})");
                continue;
            }

            letters.Add(code);
            indexes.Add(i);
        }

        if (letters.Count > 0)
            words.Add(string.Join(" ", letters));

        return new TranslationResult(string.Join(" / ", words), warnings, indexes);
    }

    /// <summary>Переводит код Морзе в текст.</summary>
    public TranslationResult Decode(string code)
    {
        var text = new StringBuilder();
        var warnings = new List<string>();
        var indexes = new List<int>();
        int lastWord = 0;

        foreach (var letter in SplitCode(code))
        {
            // Между словами ставим пробел
            if (text.Length > 0 && letter.Word != lastWord)
                text.Append(' ');
            lastWord = letter.Word;

            char? symbol = null;
            if (letter.Code.All(c => c is '.' or '-'))
                symbol = MorseAlphabet.GetSymbol(letter.Code, Alphabet);

            if (symbol == null)
            {
                warnings.Add($"Неизвестная комбинация «{letter.Code}» (позиция {letter.Position + 1})");
                symbol = '?';
            }

            indexes.Add(text.Length);
            text.Append(symbol.Value);
        }

        return new TranslationResult(text.ToString(), warnings, indexes);
    }

    /// <summary>Проверяет, похожа ли строка на код Морзе (только точки, тире, пробелы и /).</summary>
    public static bool LooksLikeCode(string input)
    {
        string s = NormalizeCode(input);
        return s.Any(c => c is '.' or '-')
            && s.All(c => c is '.' or '-' or '/' || char.IsWhiteSpace(c));
    }

    /// <summary>Заменяет похожие символы (·, •, _, –, —) на точку и тире.</summary>
    public static string NormalizeCode(string code)
    {
        var sb = new StringBuilder(code.Length);
        foreach (char c in code)
        {
            sb.Append(c switch
            {
                '·' or '•' or '∙' => '.',
                '_' or '–' or '—' or '−' => '-',
                _ => c
            });
        }
        return sb.ToString();
    }

    // Делит код на буквы. Слова разделяются "/" или тремя и более пробелами
    internal static List<CodeLetter> SplitCode(string code)
    {
        var result = new List<CodeLetter>();
        string s = NormalizeCode(code);
        int word = 0;
        bool wordHasLetters = false;
        int i = 0;

        while (i < s.Length)
        {
            if (s[i] == '/')
            {
                if (wordHasLetters)
                {
                    word++;
                    wordHasLetters = false;
                }
                i++;
            }
            else if (char.IsWhiteSpace(s[i]))
            {
                int start = i;
                while (i < s.Length && char.IsWhiteSpace(s[i]))
                    i++;

                if (i - start >= 3 && wordHasLetters)
                {
                    word++;
                    wordHasLetters = false;
                }
            }
            else
            {
                int start = i;
                while (i < s.Length && s[i] != '/' && !char.IsWhiteSpace(s[i]))
                    i++;

                result.Add(new CodeLetter(s[start..i], start, word));
                wordHasLetters = true;
            }
        }

        return result;
    }
}
