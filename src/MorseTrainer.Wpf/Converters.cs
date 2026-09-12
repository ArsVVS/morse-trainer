using System.Globalization;
using System.Windows.Data;
using MorseTrainer.Core;

namespace MorseTrainer.Wpf;

/// <summary>Показывает значения перечислений по-русски.</summary>
public class EnumNameConverter : IValueConverter
{
    private static readonly Dictionary<object, string> Names = new()
    {
        [AlphabetKind.Latin] = "Латиница",
        [AlphabetKind.Cyrillic] = "Кириллица",
        [AppTheme.Light] = "Светлая",
        [AppTheme.Dark] = "Тёмная",
        [OutputMode.Sound] = "Звук",
        [OutputMode.Light] = "Свет",
        [OutputMode.Both] = "Звук и свет",
        [TrainingMode.Receive] = "Приём",
        [TrainingMode.Send] = "Передача",
        [TaskKind.Symbols] = "Отдельные символы",
        [TaskKind.Groups] = "Группы по 5 символов",
        [TaskKind.Words] = "Слова"
    };

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value != null && Names.TryGetValue(value, out var name) ? name : value?.ToString() ?? "";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
