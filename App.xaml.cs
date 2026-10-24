using System.Configuration;
using System.Data;
using System.Windows;

namespace S2CE
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            System.IO.Directory.SetCurrentDirectory(AppContext.BaseDirectory);
            base.OnStartup(e);
        }
    }


}
