namespace MorseTrainer.Core;

/// <summary>Небольшой встроенный словарь для заданий «слова».</summary>
public static class WordList
{
    private static readonly string[] Latin =
    [
        "SUM", "MUST", "TRUST", "STORM", "ROOM", "MORE", "RUST", "MAST", "SMART", "STAR",
        "ARM", "PART", "TRAP", "SPORT", "PAST", "ATOM", "LOST", "SLOW", "WALL", "TALL",
        "WORLD", "POWER", "RADIO", "SIGNAL", "MORSE", "TOWER", "PILOT", "PLAIN", "TRAIN", "WATER",
        "FIELD", "LIGHT", "NIGHT", "SOUND", "HELLO", "QUIET", "ZERO", "BEACON", "CODE", "KEY",
        "LEVEL", "SPEED", "OCEAN", "SHIP", "STATION", "ANTENNA", "CALL", "NAME", "WEATHER", "TIME"
    ];

    private static readonly string[] Cyrillic =
    [
        "ТИНА", "НЕТ", "ТЕМА", "СЕТИ", "МАСТ", "ТАНК", "САНИ", "КИНО", "НОС", "ОСТРОВ",
        "МИР", "РАМА", "СОН", "ТОН", "ВЕТЕР", "ДОМ", "ДОРОГА", "УТРО", "ЛЕС", "ПОЛЕ",
        "ГОРОД", "РЕКА", "ЗВУК", "БЕРЕГ", "РАДИО", "СИГНАЛ", "МОРЗЕ", "КОД", "КЛЮЧ", "СВЕТ",
        "НОЧЬ", "ДЕНЬ", "ЛАМПА", "ВОЛНА", "ПРИВЕТ", "ЧАСЫ", "ШКОЛА", "ЮГ", "ЖУК", "ХЛЕБ",
        "СЕВЕР", "МОРЕ", "СТАНЦИЯ", "ЭФИР", "ЩИТ", "ПОГОДА", "СКОРОСТЬ", "УРОВЕНЬ", "АНТЕННА", "ВРЕМЯ"
    ];

    public static IReadOnlyList<string> GetWords(AlphabetKind alphabet) =>
        alphabet == AlphabetKind.Latin ? Latin : Cyrillic;
}
