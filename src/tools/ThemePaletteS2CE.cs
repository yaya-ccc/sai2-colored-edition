using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace S2CE.Tools;

internal sealed class ThemePaletteS2CE
{
    public static IReadOnlyList<string> Keys { get; } = Array.AsReadOnly(new[] {
        "Primary", "Secondary", "Ternary", "Text", "SelectablePrimary", "SelectableSecondary" });

    public IReadOnlyDictionary<string, string> Colors { get; }
    public bool AffectColorCircle { get; }

    public ThemePaletteS2CE(IReadOnlyDictionary<string, string> colors, bool affectColorCircle)
    {
        var normalized = new Dictionary<string, string>();
        foreach (string key in Keys)
        {
            if (!colors.TryGetValue(key, out string? value) || !TryNormalize(value, out string hex))
                throw new InvalidDataException($"颜色 {key} 需要六位十六进制值，例如 #263342。");
            normalized[key] = hex;
        }
        Colors = new ReadOnlyDictionary<string, string>(normalized);
        AffectColorCircle = affectColorCircle;
    }

    public static bool TryNormalize(string? value, out string hex)
    {
        hex = (value ?? "").Trim().ToUpperInvariant();
        if (!hex.StartsWith('#')) hex = "#" + hex;
        return hex.Length == 7 && uint.TryParse(hex.AsSpan(1), NumberStyles.AllowHexSpecifier,
            CultureInfo.InvariantCulture, out _);
    }

    public static ThemePaletteS2CE Load(string file)
    {
        var values = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(file))
            ?? throw new InvalidDataException("主题文件为空。");
        bool circle = !values.TryGetValue("AffectColorCircle", out string? setting) ||
            !int.TryParse(setting, out int flag) || flag != 0;
        return new(values, circle);
    }

    public string ToJson()
    {
        var values = Colors.ToDictionary(pair => pair.Key, pair => pair.Value);
        values["AffectColorCircle"] = AffectColorCircle ? "1" : "0";
        return JsonSerializer.Serialize(values, new JsonSerializerOptions { WriteIndented = true });
    }

    public double MinimumTextContrast()
    {
        static double Luminance(string hex)
        {
            double Linear(int offset)
            {
                double channel = int.Parse(hex.AsSpan(offset, 2), NumberStyles.HexNumber) / 255.0;
                return channel <= 0.04045 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);
            }
            return 0.2126 * Linear(1) + 0.7152 * Linear(3) + 0.0722 * Linear(5);
        }
        double text = Luminance(Colors["Text"]);
        return new[] { "Primary", "Secondary", "Ternary" }.Min(key => {
            double background = Luminance(Colors[key]);
            return (Math.Max(text, background) + 0.05) / (Math.Min(text, background) + 0.05);
        });
    }
}
