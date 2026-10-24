using System.Diagnostics;
using System.IO;
using System.Text;
using System.Security.Principal;
using System.Windows;
using System.Windows.Controls;
using S2CE.Extensions;
using System.Text.Json;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Reflection;
using S2CE.Tools;
using Microsoft.Win32;
using Path = System.IO.Path;

namespace S2CE.Components
{
    public partial class AppWindow : Window
    {
        public AppWindow()
        {
            InitializeComponent();
        }

        public string? selectedTheme { get; private set; }
        MainTheme mainTheme = new();

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            username.Text = $"{WindowsIdentity.GetCurrent().Name.Split('\\')[1]}!";
            try
            {
                changeLog.Text = File.ReadAllText("CHANGELOG.md", Encoding.UTF8);
            } catch
            {
                changeLog.Text = "Кто-то удалил мой CHANGELOG.md...";
            }

            version.Text += AssemblyProductVersion.Substring(0,5);

            AddItem("classic_sai2", true);

            var reader = Directory.GetFiles("./themes/");
            foreach (var f in reader)
            {
                AddItem(f);
            }

            PathS2CE.UseSource(Path.GetFullPath("./ref/sai2.exe"));
            RefreshCompatibility();
        }

        private void Item_Selected(object sender, RoutedEventArgs e)
        {
            foreach (ListBoxItem item in appList.Items)
            {
                if (item.IsFocused) selectedTheme = item.Name;
            }
        }

        private async void Launch_ButtonClicked(object sender, RoutedEventArgs e)
        {
            if (!RefreshCompatibility()) return;
            launchButton.IsEnabled = false;
            selectSaiButton.IsEnabled = refreshSaiButton.IsEnabled = appList.IsEnabled = false;
            launchButton.Content = "Running";
            try
            {
                await Task.Run(() => mainTheme.Apply(selectedTheme ?? "classic_sai2"));
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, exception.Message, "SAI2 theme could not be applied", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                selectSaiButton.IsEnabled = refreshSaiButton.IsEnabled = true;
                launchButton.Content = "Save & Run";
                RefreshCompatibility();
            }
        }

        private bool RefreshCompatibility()
        {
            var result = CompatibilityS2CE.CheckCurrent();
            compatibilityTitle.Text = (result.IsSupported ? "支持 · " : "不可换肤 · ") + result.Build;
            compatibilityTitle.Foreground = new SolidColorBrush(result.IsSupported ? Color.FromRgb(159, 255, 220) : Color.FromRgb(255, 185, 135));
            compatibilityPath.Text = "原文件：" + result.Path;
            compatibilityPath.ToolTip = "SHA-256: " + result.Sha256;
            compatibilityDetails.Text = result.Details + Environment.NewLine +
                "生成副本：" + Path.GetFullPath(PathS2CE.sai2) + Environment.NewLine +
                "同时生成同名 INI，保留原 EXE/INI；classic 清理生成的副本并启动原版。";
            compatibilityDetails.ToolTip = compatibilityDetails.Text;
            try { supportedBuilds.Text = "已适配：" + string.Join("；", BinaryProfileS2CE.SupportedBuilds()); }
            catch { supportedBuilds.Text = "适配目录无法读取。"; }
            launchButton.IsEnabled = appList.IsEnabled = result.IsSupported;
            return result.IsSupported;
        }

        private void SelectSai_ButtonClicked(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Title = "选择原版 sai2.exe（原文件保持不变）",
                Filter = "SAI2 executable (*.exe)|*.exe", CheckFileExists = true, Multiselect = false };
            if (dialog.ShowDialog(this) != true) return;
            try
            {
                PathS2CE.UseSource(dialog.FileName);
                RefreshCompatibility();
            }
            catch (Exception exception)
            {
                MessageBox.Show(this, exception.Message, "SAI2 selection", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshSai_ButtonClicked(object sender, RoutedEventArgs e) => RefreshCompatibility();
        private void Git_ButtonClicked(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://github.com/supchyan/sai2-colored-edition") { UseShellExecute = true });
        }
        private void Dis_ButtonClicked(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://discord.gg/dGF8p9UGyM") { UseShellExecute = true });
        }
        
        void AddItem(string f, bool vanilla = false)
        {   
            try
            {
                ListBoxItem item = new ListBoxItem()
                {
                    Name = f.PurifyName(),
                };

                StackPanel panel = new StackPanel()
                {
                    Orientation = Orientation.Horizontal,
                };

                item.Content = panel;

                Dictionary<string, string>? colors = new Dictionary<string, string>() {
                    { "0","#C0C0C0" },
                    { "1","#FFFFFF" },
                    { "2","#CCCCCC" },
                    { "3","#000000" },
                    { "4","#BBBBBB" },
                    { "5","#8E8E8E" },
                };

                if (!vanilla)
                {
                    colors = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(f));

                    // Remove AffectColorCircle trigger
                    colors?.Remove("AffectColorCircle");
                }
                else
                {
                    selectedTheme = f;
                    item.IsSelected = true;
                }
                if (colors != null)
                {
                    foreach (var col in colors.Values)
                    {
                        panel.Children.Add(new Rectangle
                        {
                            Fill = new SolidColorBrush(Color.FromRgb(col.toByteColor()[2], col.toByteColor()[1], col.toByteColor()[0])),
                            Width = 12,
                            Height = 12,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                        });
                    }
                }
                
                panel.Children.Add(new TextBlock()
                {
                    Text = f.PurifyName(),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(10, 0, 0, 0),
                });

                item.Selected += Item_Selected;
                appList.Items.Add(item);
            }
            catch
            {

            }
        }

        private static string AssemblyProductVersion
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly()
                    .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false);
                return attributes.Length == 0 ?
                    "" :
                    ((AssemblyInformationalVersionAttribute)attributes[0]).InformationalVersion;
            }
        }
    }
}
