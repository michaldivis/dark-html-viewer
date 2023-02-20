using DarkHtmlViewer;
using Microsoft.Extensions.Logging;
using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows;

namespace DarkHtmlViewerBasicDemo;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        CheckCompatibility();

        HtmlViewer.ConfigureLogger(() => LoggerFactory.Create(c =>
        {
            c.SetMinimumLevel(LogLevel.Debug);
            c.AddDebug();
        }));

        var appLocation = Assembly.GetExecutingAssembly().Location;
        var appDirName = Path.GetDirectoryName(appLocation)!;
        var htmlAssetsDir = Path.Combine(appDirName, "Files");

        HtmlViewer.ConfigureVirtualHostNameToFolderMappingSettings(new VirtualHostNameToFolderMappingSettings
        {
            Hostname = "darkassets.local",
            FolderPath = htmlAssetsDir,
            AccessKind = Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow
        });

        HtmlViewer.ConfigureDefaultBackgroundColor(Color.FromArgb(255, 24, 24, 24));

        MainWindow = new DemoView();
        MainWindow.Show();
    }

    private static void CheckCompatibility()
    {
        try
        {
            HtmlViewer.CheckBasicCompatibility();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}
