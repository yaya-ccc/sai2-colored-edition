using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Threading;
using S2CE.Tools;

namespace S2CE.Components;

public partial class ThemeEditorWindow : Window
{
    private readonly List<ColorField> fields = [];
    private string? editingFile;
    private ColorField? activeField;
    private bool syncing;
    private SaiScreenshotPreview? screenshot;
    private readonly DispatcherTimer previewTimer = new() { Interval = TimeSpan.FromMilliseconds(40) };
    public string? SavedFile { get; private set; }
    public bool UpdatedExisting { get; private set; }

    internal ThemeEditorWindow(ThemeOption seed)
    {
        InitializeComponent();
        screenshot = new SaiScreenshotPreview();
        previewImage.Source = screenshot.Image;
        previewTimer.Tick += (_, _) => { previewTimer.Stop(); if (fields.All(f => f.IsValid)) screenshot.Update(CreatePalette()); };
        Closed += (_, _) => previewTimer.Stop();
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
        SelectColor(fields[0]);
    }

    private void Name_Changed(object sender, TextChangedEventArgs e) => UpdatePreview();
    private void Circle_Changed(object sender, RoutedEventArgs e) => UpdatePreview();

    private void UpdatePreview()
    {
        if (fields.Count != 6 || screenshot is null) return;
        bool valid = fields.All(field => field.IsValid);
        saveThemeButton.IsEnabled = valid && !string.IsNullOrWhiteSpace(nameInput.Text);
        validationHint.Text = !valid ? "请输入完整的六位十六进制色值，例如 #263342。" :
            CreatePalette().MinimumTextContrast() < 3 ? "文字和部分背景比较接近，可能不易阅读；可以调整文字或背景色。" : "";
        if (valid && !previewTimer.IsEnabled) previewTimer.Start();
        if (!syncing && activeField?.IsValid == true) SelectColor(activeField);
    }

    private ThemePaletteS2CE CreatePalette() => new(fields.ToDictionary(field => field.Key, field => field.HexText), colorCircle.IsChecked == true);

    private void PickColor_Clicked(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not ColorField field) return;
        SelectColor(field);
    }

    private void SelectColor(ColorField field)
    {
        activeField = field;
        activeColorLabel.Text = "正在调整 · " + field.Name;
        var c = ((SolidColorBrush)field.PreviewBrush).Color;
        double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
        double max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b)), d = max - min;
        syncing = true;
        if (d > 0) hueSlider.Value = ((max == r ? (g-b)/d : max == g ? (b-r)/d+2 : (r-g)/d+4) * 60 + 360) % 360;
        saturationSlider.Value = max == 0 ? 0 : d / max;
        valueSlider.Value = max;
        syncing = false;
        RefreshPicker();
    }

    private static Color Hsv(double h, double s, double v)
    {
        double c = v*s, x = c*(1-Math.Abs(h/60%2-1)), m=v-c;
        (double r,double g,double b) = h < 60 ? (c,x,0d) : h < 120 ? (x,c,0d) : h < 180 ? (0d,c,x) : h < 240 ? (0d,x,c) : h < 300 ? (x,0d,c) : (c,0d,x);
        return Color.FromRgb((byte)Math.Round((r+m)*255),(byte)Math.Round((g+m)*255),(byte)Math.Round((b+m)*255));
    }

    private void RefreshPicker()
    {
        hueBase.Background = new SolidColorBrush(Hsv(hueSlider.Value,1,1));
        Canvas.SetLeft(colorMarker, saturationSlider.Value * colorPad.ActualWidth - 6);
        Canvas.SetTop(colorMarker, (1-valueSlider.Value) * colorPad.ActualHeight - 6);
    }

    private void CommitPicker()
    {
        if (syncing || activeField is null) return;
        RefreshPicker();
        var c = Hsv(hueSlider.Value,saturationSlider.Value,valueSlider.Value);
        syncing = true;
        activeField.HexText = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
        syncing = false;
    }
    private void Hue_Changed(object sender, RoutedPropertyChangedEventArgs<double> e) => CommitPicker();
    private void Sv_Changed(object sender, RoutedPropertyChangedEventArgs<double> e) => CommitPicker();
    private void Pad_SizeChanged(object sender, SizeChangedEventArgs e) { if (activeField is not null) RefreshPicker(); }
    private void SetPad(MouseEventArgs e)
    {
        var p = e.GetPosition(colorPad);
        syncing = true;
        saturationSlider.Value = Math.Clamp(p.X / Math.Max(1,colorPad.ActualWidth),0,1);
        valueSlider.Value = 1-Math.Clamp(p.Y / Math.Max(1,colorPad.ActualHeight),0,1);
        syncing = false;
        CommitPicker();
    }
    private void Pad_Down(object sender, MouseButtonEventArgs e) { colorPad.CaptureMouse(); SetPad(e); }
    private void Pad_Move(object sender, MouseEventArgs e) { if (colorPad.IsMouseCaptured && e.LeftButton == MouseButtonState.Pressed) SetPad(e); }
    private void Pad_Up(object sender, MouseButtonEventArgs e) { SetPad(e); colorPad.ReleaseMouseCapture(); }
    private void Zoom_Changed(object sender, RoutedEventArgs e)
    {
        if (previewImage is null) return;
        previewImage.Stretch = actualSize.IsChecked == true ? Stretch.None : Stretch.Uniform;
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
