using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using S2CE.Tools;

namespace S2CE.Components;

public partial class AppWindow : Window
{
    private readonly LauncherPreferencesS2CE preferences = new();
    private CompatibilityResult? compatibility;
    private string? sourcePath;
    private string? preferenceError;
    private string? operationError;
    private bool busy;

    public AppWindow() => InitializeComponent();

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        versionLabel.Text = "S2CE v" + Assembly.GetExecutingAssembly().GetName().Version?.ToString(3);
        var saved = preferences.Read();
        var themes = new List<ThemeOption>();
        foreach (var (folder, userTheme) in new[] { ("./themes", false), (UserThemesS2CE.DirectoryPath, true) })
        {
            if (userTheme && !Directory.Exists(folder)) continue;
            try
            {
                foreach (string file in Directory.GetFiles(folder, "*.json").Order())
                {
                    try { themes.Add(ThemeOption.Load(file, userTheme)); }
                    catch (Exception exception) when (exception is IOException or JsonException or InvalidDataException or UnauthorizedAccessException)
                    { preferenceError = "未加载主题 " + Path.GetFileName(file) + "：" + exception.Message; }
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            { preferenceError = "无法读取主题目录：" + exception.Message; }
        }
        themeList.ItemsSource = themes;
        themeList.SelectedItem = themes.FirstOrDefault(theme => theme.Id == saved.ThemeId) ?? themes.FirstOrDefault();
        if (themes.Count == 0) themeHint.Text = "未找到可用主题，请检查程序旁的 themes 文件夹。";
        if (!string.IsNullOrWhiteSpace(saved.SourcePath)) SelectSource(saved.SourcePath);
        else UpdateActions();
    }

    private void SelectSai_ButtonClicked(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog {
            Title = "选择你使用的原版 sai2.exe", Filter = "SAI2 程序 (*.exe)|*.exe",
            CheckFileExists = true, Multiselect = false, FileName = "sai2.exe"
        };
        if (sourcePath is not null && Directory.Exists(Path.GetDirectoryName(sourcePath)))
            dialog.InitialDirectory = Path.GetDirectoryName(sourcePath);
        if (dialog.ShowDialog(this) == true) SelectSource(dialog.FileName);
    }

    private void SelectSource(string path)
    {
        try
        {
            PathS2CE.UseSource(path);
            sourcePath = Path.GetFullPath(path);
            operationError = null;
            operationMessage.Text = "先关闭正在运行的 SAI。原版程序和配置会保留。";
            operationMessage.Foreground = (Brush)FindResource("MutedText");
            RefreshCompatibility();
            RememberChoice();
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or ArgumentException)
        { ShowOperationError("请选择原版 sai2.exe。", exception); }
    }

    private void RefreshSai_ButtonClicked(object sender, RoutedEventArgs e) => RefreshCompatibility();

    private bool RefreshCompatibility()
    {
        if (sourcePath is null) return false;
        compatibility = CompatibilityS2CE.CheckCurrent();
        sourcePathLabel.Text = compatibility.Path;
        sourcePathLabel.ToolTip = compatibility.Path;
        selectSaiButton.Content = "更换程序…";
        statusLabel.Text = compatibility.IsSupported ? "可换肤" : "暂不可用";
        statusBadge.Background = new SolidColorBrush(compatibility.IsSupported ? Color.FromRgb(40, 66, 52) : Color.FromRgb(77, 53, 40));
        statusLabel.Foreground = new SolidColorBrush(compatibility.IsSupported ? Color.FromRgb(163, 217, 190) : Color.FromRgb(240, 188, 151));
        buildLabel.Text = compatibility.Build.Replace("(localized)", "· 汉化版").Replace("(bundled)", "· 内置构建");
        sourceHint.Visibility = compatibility.IsSupported ? Visibility.Collapsed : Visibility.Visible;
        sourceHint.Text = compatibility.Sha256.Length > 0
            ? "这个构建暂未适配，请更换 SAI2 程序。"
            : "无法使用此程序或现有主题副本，请展开检测详情查看原因。";
        detectionExpander.Visibility = Visibility.Visible;
        string supported;
        try { supported = string.Join("；", BinaryProfileS2CE.SupportedBuilds()); }
        catch (Exception exception) when (exception is IOException or JsonException) { supported = exception.Message; }
        detectionDetails.Text = compatibility.Details + "\n\n原文件：" + compatibility.Path +
            "\n生成副本：" + Path.GetFullPath(PathS2CE.sai2) + "\nSHA-256：" + compatibility.Sha256 +
            "\n\n已适配：" + supported + (preferenceError is null ? "" : "\n\n" + preferenceError) +
            (operationError is null ? "" : "\n\n本次操作：" + operationError);
        UpdateActions();
        return compatibility.IsSupported;
    }

    private void Theme_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateActions();
        RememberChoice();
    }

    private void UpdateActions()
    {
        bool supported = compatibility?.IsSupported == true;
        selectSaiButton.IsEnabled = refreshSaiButton.IsEnabled = themeList.IsEnabled = !busy;
        customThemeButton.IsEnabled = !busy && themeList.SelectedItem is ThemeOption;
        customThemeButton.Content = (themeList.SelectedItem as ThemeOption)?.IsUserTheme == true ? "编辑配色…" : "自定义配色…";
        launchButton.IsEnabled = !busy && supported && themeList.SelectedItem is ThemeOption;
        launchButton.Content = busy ? "正在处理…" : sourcePath is null ? "请先选择 SAI2" : "生成主题并启动 SAI";
        restoreButton.Visibility = supported && compatibility?.IsThemed == true ? Visibility.Visible : Visibility.Collapsed;
        restoreButton.IsEnabled = !busy;
        busyProgress.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
    }

    private void RememberChoice()
    {
        if (sourcePath is null || compatibility?.IsSupported != true) return;
        try { preferences.Save(new(sourcePath, (themeList.SelectedItem as ThemeOption)?.Id)); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        { preferenceError = "无法记住本次选择：" + exception.Message; }
    }

    private async void Launch_ButtonClicked(object sender, RoutedEventArgs e)
    {
        if (themeList.SelectedItem is ThemeOption theme) await ApplyAndLaunch(theme.Id, theme.Name, false, theme.FilePath);
    }

    private async void Restore_ButtonClicked(object sender, RoutedEventArgs e) =>
        await ApplyAndLaunch("classic_sai2", "原版", true);

    private async Task ApplyAndLaunch(string themeId, string name, bool restore, string? themeFile = null)
    {
        if (busy || !RefreshCompatibility()) return;
        busy = true;
        operationError = null;
        operationMessage.Text = restore ? "正在清理主题副本…" : "正在生成「" + name + "」主题副本…";
        operationMessage.Foreground = (Brush)FindResource("MutedText");
        UpdateActions();
        bool generated = false;
        try
        {
            await Task.Run(() => new MainTheme().Apply(themeId, false, themeFile));
            generated = true;
            string executable = Path.GetFullPath(restore ? PathS2CE.originalSai2 : PathS2CE.sai2);
            Process.Start(new ProcessStartInfo(executable) { UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(executable)! });
            operationMessage.Text = restore ? "已清理主题副本并启动原版，原文件保持不变。" :
                "已启动「" + name + "」。下次可直接运行安装目录中的 sai2-colored.exe。";
            operationMessage.Foreground = (Brush)FindResource("Accent");
            RememberChoice();
        }
        catch (Exception exception)
        {
            ShowOperationError(generated ? "处理完成，但未能启动 SAI。" : "操作未完成，请先关闭 SAI 并检查目录是否可写。", exception);
        }
        finally
        {
            busy = false;
            RefreshCompatibility();
            UpdateActions();
        }
    }

    private void ShowOperationError(string message, Exception exception)
    {
        operationError = exception.Message;
        operationMessage.Text = message;
        operationMessage.Foreground = new SolidColorBrush(Color.FromRgb(240, 188, 151));
        detectionExpander.Visibility = Visibility.Visible;
        detectionExpander.IsExpanded = true;
        detectionDetails.Text = message + "\n" + exception.Message;
    }

    private void Theme_DoubleClicked(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject source && ItemsControl.ContainerFromElement(themeList, source) is ListBoxItem)
            CustomTheme_Clicked(sender, e);
    }

    private void CustomTheme_Clicked(object sender, RoutedEventArgs e)
    {
        if (busy || themeList.SelectedItem is not ThemeOption seed) return;
        EditTheme(seed);
    }

    private void EditTheme_Clicked(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (busy || (sender as FrameworkElement)?.DataContext is not ThemeOption seed || !seed.IsUserTheme) return;
        themeList.SelectedItem = seed;
        EditTheme(seed);
    }

    private void EditTheme(ThemeOption seed)
    {
        var editor = new ThemeEditorWindow(seed) { Owner = this };
        if (editor.ShowDialog() != true || editor.SavedFile is null) return;
        var option = ThemeOption.Load(editor.SavedFile, true);
        var themes = themeList.Items.Cast<ThemeOption>().ToList();
        int existing = themes.FindIndex(theme => theme.Id == option.Id);
        if (existing >= 0) themes[existing] = option;
        else themes.Add(option);
        themeList.ItemsSource = themes;
        themeList.SelectedItem = option;
        themeList.ScrollIntoView(option);
        operationMessage.Text = (editor.UpdatedExisting ? "已更新「" : "已保存「") + option.Name + "」。" +
            (compatibility?.IsSupported == true ? "点击“生成主题并启动 SAI”即可应用。" : "选择 SAI2 程序后即可应用。");
        operationMessage.Foreground = (Brush)FindResource("Accent");
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (busy) e.Cancel = true;
        base.OnClosing(e);
    }
}
