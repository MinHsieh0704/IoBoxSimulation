using Min_Helpers.LogHelper;
using Min_Helpers.PrintHelper;
using System.Globalization;
using System.Threading;
using System.Windows;

namespace IoBoxSimulation
{
    /// <summary>
    /// App.xaml 的互動邏輯
    /// </summary>
    public partial class App : Application
    {
        public Print PrintService { get; set; }

        public Log LogService { get; set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en");

            LogService = new Log();
            PrintService = new Print(LogService);

            LogService.Write("");
            PrintService.Log("App Start", Print.EMode.info);

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            PrintService.Log("App End", Print.EMode.info);

            base.OnExit(e);
        }
    }
}
