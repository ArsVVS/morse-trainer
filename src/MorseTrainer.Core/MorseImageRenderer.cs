using System.Security;
using System.Text;

namespace MorseTrainer.Core;

/// <summary>Рисует код Морзе в виде SVG-картинки.</summary>
public class MorseImageRenderer
{
    // Размер одной единицы (точки) в пикселях
    private const int Unit = 12;
    private const int Padding = 20;
    private const int RowHeight = 60;
    private const int LetterGap = Unit * 3;
    private const int WordGap = Unit * 6;

    public string Color { get; set; } = "#263238";

    public string HighlightColor { get; set; } = "#E53935";

    public string Background { get; set; } = "#FFFFFF";

    /// <summary>Возвращает SVG-строку. highlightLetter – номер буквы, которую нужно выделить (-1 – никакую).</summary>
    public string Render(string code, AlphabetKind alphabet = AlphabetKind.Latin, int highlightLetter = -1, int maxWidth = 800)
    {
        var letters = MorseTranslator.SplitCode(code);
        var body = new StringBuilder();
        int x = Padding;
        int y = Padding;
        int usedWidth = Padding;

        // Группируем буквы по словам, чтобы переносить целые слова
        foreach (var word in letters.Select((l, n) => (Letter: l, Number: n)).GroupBy(p => p.Letter.Word))
        {
            int wordWidth = word.Sum(p => LetterWidth(p.Letter.Code)) + LetterGap * (word.Count() - 1);
            if (x > Padding && x + wordWidth > maxWidth - Padding)
            {
                x = Padding;
                y += RowHeight;
            }

            foreach (var (letter, number) in word)
            {
                string fill = number == highlightLetter ? HighlightColor : Color;
                int width = LetterWidth(letter.Code);
                DrawLetter(body, letter.Code, x, y, fill);

                // Подпись под буквой
                char symbol = MorseAlphabet.GetSymbol(letter.Code, alphabet) ?? '?';
                string label = SecurityElement.Escape(symbol.ToString());
                body.Append($"<text x=\"{x + width / 2}\" y=\"{y + Unit + 24}\" font-family=\"Arial\" font-size=\"16\" text-anchor=\"middle\" fill=\"{fill}\">{label}</text>");
                body.AppendLine();

                x += width + LetterGap;
            }

            x += WordGap - LetterGap;
            usedWidth = Math.Max(usedWidth, x - WordGap + Padding);
        }

        int svgWidth = Math.Max(usedWidth, Padding * 2);
        int svgHeight = letters.Count == 0 ? Padding * 2 : y + RowHeight;

        var svg = new StringBuilder();
        svg.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{svgWidth}\" height=\"{svgHeight}\" viewBox=\"0 0 {svgWidth} {svgHeight}\">");
        svg.AppendLine($"<rect x=\"0\" y=\"0\" width=\"{svgWidth}\" height=\"{svgHeight}\" fill=\"{Background}\"/>");
        svg.Append(body);
        svg.AppendLine("</svg>");
        return svg.ToString();
    }

    // Точки – кружки, тире – скруглённые прямоугольники
    private static void DrawLetter(StringBuilder svg, string code, int x, int y, string fill)
    {
        foreach (char c in code)
        {
            if (c == '.')
            {
                svg.Append($"<circle cx=\"{x + Unit / 2}\" cy=\"{y + Unit / 2}\" r=\"{Unit / 2}\" fill=\"{fill}\"/>");
                x += Unit * 2;
            }
            else if (c == '-')
            {
                svg.Append($"<rect x=\"{x}\" y=\"{y}\" width=\"{Unit * 3}\" height=\"{Unit}\" rx=\"{Unit / 2}\" fill=\"{fill}\"/>");
                x += Unit * 4;
            }
        }
        svg.AppendLine();
    }

    // Ширина буквы: точка – 1 единица, тире – 3, между ними пауза в 1 единицу
    private static int LetterWidth(string code)
    {
        int width = 0;
        foreach (char c in code)
        {
            if (c == '.')
                width += Unit * 2;
            else if (c == '-')
                width += Unit * 4;
        }
        // Последняя пауза не нужна, но буква не должна быть уже подписи
        return Math.Max(width - Unit, Unit);
    }
}
