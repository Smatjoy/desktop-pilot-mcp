using System.Drawing;

namespace WinAppMCP;

/// A box in screen pixels, the same space as get_element_bounds.
public readonly record struct OcrBox(int X, int Y, int Width, int Height);

/// <summary>
/// The pixel arithmetic of read_text_ocr. It has no Windows dependency, so it can be tested anywhere.
/// </summary>
public static class OcrGeometry
{
    /// The part of the window image that shows the element, plus a margin. Null when none of it is in the image.
    public static Rectangle? CropForElement(Rectangle element, Rectangle window, int margin, Size image)
    {
        Rectangle crop = new(element.X - window.X - margin, element.Y - window.Y - margin,
            element.Width + 2 * margin, element.Height + 2 * margin);
        crop.Intersect(new Rectangle(Point.Empty, image));
        return crop.Width > 0 && crop.Height > 0 ? crop : null;
    }

    /// The largest whole scale, up to the requested one, that keeps the image within the OCR engine's limit.
    /// Returns 0 when even scale 1 is too large.
    public static int EffectiveScale(int width, int height, int scale, uint maxDimension)
    {
        for (int s = scale; s >= 1; s--)
        {
            if ((long)width * s <= maxDimension && (long)height * s <= maxDimension)
                return s;
        }
        return 0;
    }

    /// Maps a box from the scaled image back to the screen. It rounds outward, so the box never cuts the text.
    public static OcrBox ToScreen(double x, double y, double width, double height, int scale, Point origin)
    {
        int left = (int)Math.Floor(x / scale);
        int top = (int)Math.Floor(y / scale);
        int right = (int)Math.Ceiling((x + width) / scale);
        int bottom = (int)Math.Ceiling((y + height) / scale);
        return new OcrBox(origin.X + left, origin.Y + top, right - left, bottom - top);
    }

    /// The box around all the given boxes. Windows OCR gives boxes for words, not for lines.
    public static OcrBox Union(IReadOnlyCollection<OcrBox> boxes)
    {
        if (boxes.Count == 0)
            return new OcrBox(0, 0, 0, 0);

        int left = boxes.Min(b => b.X);
        int top = boxes.Min(b => b.Y);
        int right = boxes.Max(b => b.X + b.Width);
        int bottom = boxes.Max(b => b.Y + b.Height);
        return new OcrBox(left, top, right - left, bottom - top);
    }
}
