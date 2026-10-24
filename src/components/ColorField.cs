using System.ComponentModel;
using System.Windows.Media;
using S2CE.Tools;

namespace S2CE.Components;

internal sealed class ColorField(string key, string name, string hint, string initial) : INotifyPropertyChanged
{
    private string hexText = initial;
    private Brush brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(initial));
    public string Key { get; } = key;
    public string Name { get; } = name;
    public string Hint { get; } = hint;
    public Brush PreviewBrush => brush;
    public bool IsValid => ThemePaletteS2CE.TryNormalize(hexText, out _);
    public string HexText
    {
        get => hexText;
        set
        {
            hexText = value;
            if (ThemePaletteS2CE.TryNormalize(value, out string hex))
                brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            foreach (string property in new[] { nameof(HexText), nameof(PreviewBrush), nameof(IsValid) })
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
            Changed?.Invoke();
        }
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? Changed;
}
