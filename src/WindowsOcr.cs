using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;

namespace WinAppMCP;

/// A recognised word. The box is in the pixels of the image given to the engine.
internal sealed record OcrWordResult(string Text, double X, double Y, double Width, double Height);

internal sealed record OcrLineResult(string Text, IReadOnlyList<OcrWordResult> Words);

/// <summary>
/// Windows' own OCR engine. It runs on the local PC and sends nothing out.
/// It gives no confidence score, so a caller cannot tell a clean read from a misread.
/// </summary>
internal static class WindowsOcr
{
    public static uint MaxImageDimension => OcrEngine.MaxImageDimension;

    /// Null when the language can be used, otherwise a message that lists what can.
    /// A language needs its Windows language pack with OCR, so en-US can be missing on an Italian Windows.
    public static string? LanguageError(string languageTag)
    {
        if (TryCreateEngine(languageTag) is not null)
            return null;

        string installed = string.Join(", ", OcrEngine.AvailableRecognizerLanguages.Select(l => l.LanguageTag));
        return $"OCR_LANGUAGE_NOT_AVAILABLE: '{languageTag}'. Available: {(installed.Length > 0 ? installed : "none")}";
    }

    public static IReadOnlyList<OcrLineResult> Recognize(Bitmap image, string languageTag, int scale)
    {
        OcrEngine engine = TryCreateEngine(languageTag)
            ?? throw new InvalidOperationException($"OCR_LANGUAGE_NOT_AVAILABLE: '{languageTag}'");

        using SoftwareBitmap bitmap = ToSoftwareBitmap(image, scale);
        OcrResult result = engine.RecognizeAsync(bitmap).AsTask().GetAwaiter().GetResult();

        List<OcrLineResult> lines = new();
        foreach (OcrLine line in result.Lines)
        {
            List<OcrWordResult> words = line.Words
                .Select(w => new OcrWordResult(w.Text, w.BoundingRect.X, w.BoundingRect.Y, w.BoundingRect.Width, w.BoundingRect.Height))
                .ToList();
            lines.Add(new OcrLineResult(line.Text, words));
        }
        return lines;
    }

    private static OcrEngine? TryCreateEngine(string languageTag)
    {
        try
        {
            return OcrEngine.TryCreateFromLanguage(new Language(languageTag));
        }
        catch (ArgumentException)
        {
            return null; // Not a valid language tag.
        }
    }

    private static SoftwareBitmap ToSoftwareBitmap(Bitmap image, int scale)
    {
        using Bitmap scaled = Upscale(image, scale);
        using MemoryStream png = new();
        scaled.Save(png, ImageFormat.Png);
        png.Position = 0;

        BitmapDecoder decoder = BitmapDecoder.CreateAsync(png.AsRandomAccessStream()).AsTask().GetAwaiter().GetResult();
        return decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied).AsTask().GetAwaiter().GetResult();
    }

    /// Windows OCR reads small UI text better at a larger size.
    private static Bitmap Upscale(Bitmap image, int scale)
    {
        Bitmap result = new(image.Width * scale, image.Height * scale, PixelFormat.Format32bppArgb);
        using Graphics graphics = Graphics.FromImage(result);
        graphics.InterpolationMode = scale == 1 ? InterpolationMode.NearestNeighbor : InterpolationMode.HighQualityBicubic;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.DrawImage(image, 0, 0, result.Width, result.Height);
        return result;
    }
}
