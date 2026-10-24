using System.IO;
using System.Text.Json;
using System.Windows.Media;

namespace S2CE.Components;

internal sealed class ThemeOption
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required Brush Primary { get; init; }
    public required Brush Secondary { get; init; }
    public required Brush Ternary { get; init; }
    public required Brush Selection { get; init; }

    public static ThemeOption Load(string file)
    {
        var colors = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(file))
            ?? throw new InvalidDataException("Theme file is empty.");
        string id = Path.GetFileNameWithoutExtension(file);
        if (id == "classic_sai2") throw new InvalidDataException("classic_sai2 is reserved for restoring the original.");
        foreach (string key in new[] { "Primary", "Secondary", "Ternary", "Text", "SelectablePrimary", "SelectableSecondary" })
            if (!colors.TryGetValue(key, out string? value) || string.IsNullOrEmpty(value) || value.Length != 7 || value[0] != '#' ||
                !uint.TryParse(value.AsSpan(1), System.Globalization.NumberStyles.HexNumber, null, out _))
                throw new InvalidDataException($"Invalid theme color: {key}");
        Brush Color(string key) => new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString(colors[key]));
        var (name, description) = id switch
        {
            "example_cappuccino_theme" => ("卡布奇诺", "温暖的深棕色，搭配柔和灰色文字。"),
            "example_retro_theme" => ("薄荷复古", "清新的薄荷绿，搭配深色文字。"),
            _ => (id.Replace('_', ' '), "自定义主题")
        };
        return new() { Id = id, Name = name, Description = description,
            Primary = Color("Primary"), Secondary = Color("Secondary"), Ternary = Color("Ternary"), Selection = Color("SelectablePrimary") };
    }
}
