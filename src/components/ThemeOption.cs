using System.IO;
using System.Windows.Media;
using S2CE.Tools;

namespace S2CE.Components;

internal sealed class ThemeOption
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string FilePath { get; init; }
    public bool IsUserTheme { get; init; }
    public required ThemePaletteS2CE Palette { get; init; }
    public required Brush Primary { get; init; }
    public required Brush Secondary { get; init; }
    public required Brush Ternary { get; init; }
    public required Brush Selection { get; init; }

    public static ThemeOption Load(string file, bool userTheme = false)
    {
        var palette = ThemePaletteS2CE.Load(file);
        var colors = palette.Colors;
        string id = Path.GetFileNameWithoutExtension(file);
        if (id == "classic_sai2") throw new InvalidDataException("classic_sai2 is reserved for restoring the original.");
        Brush Color(string key) => new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString(colors[key]));
        var (name, description) = (userTheme ? "" : id) switch
        {
            "example_cappuccino_theme" => ("卡布奇诺", "温暖的深棕色，搭配柔和灰色文字。"),
            "example_retro_theme" => ("薄荷复古", "清新的薄荷绿，搭配深色文字。"),
            "example_sakura_theme" => ("樱花浅粉", "轻柔的樱花粉，搭配清晰的深色文字。"),
            _ => (userTheme ? id : id.Replace('_', ' '), "自定义配色 · 6 组界面颜色")
        };
        return new() { Id = userTheme ? "user:" + id : id, Name = name, Description = description,
            FilePath = Path.GetFullPath(file), IsUserTheme = userTheme, Palette = palette,
            Primary = Color("Primary"), Secondary = Color("Secondary"), Ternary = Color("Ternary"), Selection = Color("SelectablePrimary") };
    }
}
