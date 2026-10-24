using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using S2CE.Tools;

namespace S2CE.Components;

public partial class ThemeEditorWindow : Window
{
    private readonly List<ColorField> fields = [];
    private string? editingFile;
    public string? SavedFile { get; private set; }
    public bool UpdatedExisting { get; private set; }

    internal ThemeEditorWindow(ThemeOption seed)
    {
        InitializeComponent();
        foreach (var (key, name, hint) in new[] {
            ("Primary", "主背景", "工作区及界面的主要背景"),
            ("Secondary", "次背景", "次级控件和背景区域"),
            ("Ternary", "面板底色", "工具、图层等面板的底色"),
            ("Text", "文字", "已适配的界面文字"),
            ("SelectablePrimary", "强调色", "选中与高亮区域的主要颜色"),
            ("SelectableSecondary", "辅助强调色", "另一组高亮和选中区域的颜色")
        })
        {
            var field = new ColorField(key, name, hint, seed.Palette.Colors[key]);
            field.Changed += UpdatePreview;
            fields.Add(field);
        }
        colorFields.ItemsSource = fields;
        if (seed.IsUserTheme)
        {
            editingFile = seed.FilePath;
            Title = "编辑主题配色";
            nameInput.Text = seed.Name;
            nameInput.IsReadOnly = true;
            saveThemeButton.Content = "保存修改";
            saveAsButton.Visibility = Visibility.Visible;
            saveHint.Text = "保存到当前主题。应用到 SAI 时，请重新生成主题副本。";
        }
        else nameInput.Text = seed.Name + " 副本";
        colorCircle.IsChecked = seed.Palette.AffectColorCircle;
        UpdatePreview();
    }

    private void Name_Changed(object sender, TextChangedEventArgs e) => UpdatePreview();
    private void Circle_Changed(object sender, RoutedEventArgs e) => UpdatePreview();

    private void UpdatePreview()
    {
        if (fields.Count != 6 || previewRoot is null) return;
        previewRoot.DataContext = fields.ToDictionary(field => field.Key, field => field.PreviewBrush);
        bool valid = fields.All(field => field.IsValid);
        saveThemeButton.IsEnabled = valid && !string.IsNullOrWhiteSpace(nameInput.Text);
        validationHint.Text = !valid ? "请输入完整的六位十六进制色值，例如 #263342。" :
            CreatePalette().MinimumTextContrast() < 3 ? "文字和部分背景比较接近，可能不易阅读；可以调整文字或背景色。" : "";
    }

    private ThemePaletteS2CE CreatePalette() => new(fields.ToDictionary(field => field.Key, field => field.HexText), colorCircle.IsChecked == true);

    private void PickColor_Clicked(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not ColorField field) return;
        var initial = ((SolidColorBrush)field.PreviewBrush).Color;
        if (NativeColorPicker.Show(this, initial) is Color color)
            field.HexText = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    private void SaveTheme_Clicked(object sender, RoutedEventArgs e)
    {
        try
        {
            SavedFile = editingFile is null
                ? UserThemesS2CE.SaveNew(nameInput.Text, CreatePalette())
                : UserThemesS2CE.SaveChanges(editingFile, CreatePalette());
            UpdatedExisting = editingFile is not null;
            DialogResult = true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
        { validationHint.Text = exception.Message; }
    }

    private void SaveAs_Clicked(object sender, RoutedEventArgs e)
    {
        editingFile = null;
        Title = "另存主题配色";
        nameInput.IsReadOnly = false;
        nameInput.Text += " 副本";
        saveAsButton.Visibility = Visibility.Collapsed;
        saveThemeButton.Content = "保存新主题";
        saveHint.Text = "另存为新主题，当前主题会保留。";
        nameInput.Focus();
        nameInput.SelectAll();
        UpdatePreview();
    }
}
