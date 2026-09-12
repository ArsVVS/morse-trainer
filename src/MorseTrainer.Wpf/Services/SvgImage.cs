using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;

namespace MorseTrainer.Wpf.Services;

/// <summary>
/// Превращает простой SVG из MorseImageRenderer (rect, circle, text) в картинку WPF.
/// WPF сам SVG не показывает, а другие элементы нам не нужны.
/// </summary>
public static class SvgImage
{
    public static DrawingImage FromSvg(string svg)
    {
        var root = XDocument.Parse(svg).Root!;
        var group = new DrawingGroup();

        foreach (var element in root.Elements())
        {
            var brush = (Brush)new BrushConverter().ConvertFromString(Attr(element, "fill", "#000000"))!;
            Geometry geometry;

            switch (element.Name.LocalName)
            {
                case "rect":
                    double rx = Num(element, "rx");
                    geometry = new RectangleGeometry(
                        new Rect(Num(element, "x"), Num(element, "y"), Num(element, "width"), Num(element, "height")), rx, rx);
                    break;
                case "circle":
                    double r = Num(element, "r");
                    geometry = new EllipseGeometry(new Point(Num(element, "cx"), Num(element, "cy")), r, r);
                    break;
                case "text":
                    var text = new FormattedText(element.Value, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                        new Typeface(Attr(element, "font-family", "Arial")), Num(element, "font-size"), brush, 1.0);
                    // x – середина текста, y – базовая линия
                    var origin = new Point(Num(element, "x") - text.Width / 2, Num(element, "y") - text.Baseline);
                    geometry = text.BuildGeometry(origin);
                    break;
                default:
                    continue;
            }

            group.Children.Add(new GeometryDrawing(brush, null, geometry));
        }

        var image = new DrawingImage(group);
        image.Freeze();
        return image;
    }

    /// <summary>Сохраняет картинку в PNG.</summary>
    public static void SavePng(DrawingImage image, string path)
    {
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
            dc.DrawImage(image, new Rect(0, 0, image.Width, image.Height));

        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(image.Width), (int)Math.Ceiling(image.Height),
            96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var file = File.Create(path);
        encoder.Save(file);
    }

    private static string Attr(XElement element, string name, string fallback) =>
        element.Attribute(name)?.Value ?? fallback;

    private static double Num(XElement element, string name) =>
        double.Parse(Attr(element, name, "0"), CultureInfo.InvariantCulture);
}
