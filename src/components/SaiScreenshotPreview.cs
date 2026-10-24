using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using S2CE.Tools;

namespace S2CE.Components;

// Real 2024.08.14 SAI2 capture, with the Sakura palette. Keep artwork and the color wheel intact.
internal sealed class SaiScreenshotPreview
{
    private readonly byte[] original;
    private readonly sbyte[] roles;
    private readonly WriteableBitmap bitmap;
    private readonly byte[] output;
    private readonly int width, height;
    public ImageSource Image => bitmap;

    public SaiScreenshotPreview()
    {
        var source = new BitmapImage(new Uri("pack://application:,,,/S2CE;component/res/sai2-preview.png"));
        var crop = new CroppedBitmap(source, new Int32Rect(0, 25, source.PixelWidth, source.PixelHeight - 25));
        var pixels = new FormatConvertedBitmap(crop, PixelFormats.Bgra32, null, 0);
        width = pixels.PixelWidth; height = pixels.PixelHeight;
        original = new byte[width * height * 4];
        pixels.CopyPixels(original, width * 4, 0);
        output = new byte[original.Length];
        roles = new sbyte[width * height];
        Color[] colors = [Parse("#F8EEF2"), Parse("#F1E0E8"), Parse("#FFF8FB"), Parse("#160B12"), Parse("#AB6684"), Parse("#B77894")];
        for (int i = 0; i < roles.Length; i++)
        {
            roles[i] = -1;
            int x = i % width, y = i / width + 25;
            // Canvas, navigator, and color wheel are content, not theme colors.
            if (x >= 825 && x <= 1083 && y >= 283 && y <= 541 ||
                x < 166 && y >= 53 && y <= 201 ||
                Math.Pow(x - 277, 2) + Math.Pow(y - 154, 2) <= 83 * 83 || y < 25) continue;
            int best = 1200;
            for (sbyte role = 0; role < colors.Length; role++)
            {
                var c = colors[role];
                int d = Square(original[i * 4] - c.B) + Square(original[i * 4 + 1] - c.G) + Square(original[i * 4 + 2] - c.R);
                if (d < best) { best = d; roles[i] = role; }
            }
        }
        bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
    }

    private static int Square(int n) => n * n;
    private static Color Parse(string hex) => (Color)ColorConverter.ConvertFromString(hex);

    public void Update(ThemePaletteS2CE palette)
    {
        var colors = ThemePaletteS2CE.Keys.Select(key => Parse(palette.Colors[key])).ToArray();
        Array.Copy(original, output, original.Length);
        for (int i = 0; i < roles.Length; i++)
        {
            int role = roles[i];
            if (role < 0) continue;
            var color = colors[role];
            output[i * 4] = color.B; output[i * 4 + 1] = color.G; output[i * 4 + 2] = color.R;
        }
        bitmap.WritePixels(new Int32Rect(0, 0, width, height), output, width * 4, 0);
    }
}
