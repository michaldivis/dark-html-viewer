using Microsoft.Extensions.Logging.Abstractions;
using System.Windows;

namespace DarkHtmlViewerBasicDemo
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            //configure logger
            DarkHtmlViewer.DarkHtmlViewer.ConfigureLogger(() => NullLogger<DarkHtmlViewer.DarkHtmlViewer>.Instance);
        }
    }
}
