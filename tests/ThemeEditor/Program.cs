using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Collections;
class Program
{
 [STAThread] static void Main(string[] args)
 {
  var app = new S2CE.App(); app.InitializeComponent();
  var assembly = typeof(S2CE.App).Assembly;
  var option = assembly.GetType("S2CE.Components.ThemeOption")!.GetMethod("Load")!.Invoke(null, new object[]{args[0],true});
  var editorType = assembly.GetType("S2CE.Components.ThemeEditorWindow")!;
  var window = (Window)Activator.CreateInstance(editorType, BindingFlags.Instance|BindingFlags.NonPublic,null,new[]{option},null)!;
  window.Measure(new Size(1200,820)); window.Arrange(new Rect(0,0,1200,820)); window.UpdateLayout();
  void Pump() { var frame=new DispatcherFrame(); var timer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(100)}; timer.Tick+=(_,_)=>{timer.Stop();frame.Continue=false;};timer.Start();Dispatcher.PushFrame(frame); }
  byte[] Snapshot() { var b=(BitmapSource)((Image)window.FindName("previewImage")).Source;var result=new byte[b.PixelWidth*b.PixelHeight*4];b.CopyPixels(result,b.PixelWidth*4,0);return result; }
  Pump();var before=Snapshot(); if(args.Length > 1) { var render=new RenderTargetBitmap(1200,820,96,96,System.Windows.Media.PixelFormats.Pbgra32);var content=(FrameworkElement)window.Content;content.Measure(new Size(1152,772));content.Arrange(new Rect(0,0,1152,772));content.UpdateLayout();render.Render(content);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(render));using var stream=System.IO.File.Create(args[1]);encoder.Save(stream); }
  var field=((IEnumerable)((ItemsControl)window.FindName("colorFields")).ItemsSource).Cast<object>().First();
  field.GetType().GetProperty("HexText")!.SetValue(field,"#224466");Pump();var after=Snapshot();
  if(before.SequenceEqual(after))throw new Exception("Hex edit did not update real screenshot");
  int offset=(400*1532+900)*4;
  if(!before.AsSpan(offset,4).SequenceEqual(after.AsSpan(offset,4)))throw new Exception("Preview recolored canvas content");
  ((Slider)window.FindName("hueSlider")).Value=140;Pump();
  if(after.SequenceEqual(Snapshot()))throw new Exception("Picker did not refresh screenshot");
  if(((TextBox)window.FindName("nameInput")).Text != "example_sakura_theme")throw new Exception("Saved theme identity changed");
  if(!((Button)window.FindName("saveThemeButton")).IsEnabled)throw new Exception("Save disabled for valid colors");
  field.GetType().GetProperty("HexText")!.SetValue(field,"#12");Pump();
  if(((Button)window.FindName("saveThemeButton")).IsEnabled)throw new Exception("Invalid hex accepted");
  Console.WriteLine("PASS editor initialization, live hex/picker preview, canvas preservation, saved theme mode, invalid input");
  window.Close();app.Shutdown();
 }
}


