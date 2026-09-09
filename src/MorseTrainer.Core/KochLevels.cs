namespace MorseTrainer.Core;

/// <summary>Уровни по методу Коха: уровень 1 – две буквы, каждый следующий добавляет одну.</summary>
public static class KochLevels
{
    public const string LatinOrder = "KMRSUAPTLOWI.NJEF0YV,G5/Q9ZH38B?427C1D6X";

    // Сначала частые и короткие по коду буквы, потом редкие, в конце цифры и знаки
    public const string CyrillicOrder = "ЕТИАНМСОРКВДУЛПГЯЗБЫЬЙХЖЧШЮЦЩЭФЪ1234567890.,?";

    public static string GetOrder(AlphabetKind alphabet) =>
        alphabet == AlphabetKind.Latin ? LatinOrder : CyrillicOrder;

    /// <summary>Самый большой уровень для алфавита.</summary>
    public static int GetMaxLevel(AlphabetKind alphabet) => GetOrder(alphabet).Length - 1;

    /// <summary>Набор символов для уровня.</summary>
    public static string GetSymbols(AlphabetKind alphabet, int level)
    {
        level = Math.Clamp(level, 1, GetMaxLevel(alphabet));
        return GetOrder(alphabet)[..(level + 1)];
    }
}
