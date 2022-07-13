using DarkHtmlViewer;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace DarkHtmlViewerBasicDemo;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        await CheckCompatibilityAsync();

        //configure logger
        HtmlViewer.ConfigureLogger(() => NullLoggerFactory.Instance);

        var appLocation = Assembly.GetExecutingAssembly().Location;
        var htmlAssetsDir = Path.Combine(Path.GetDirectoryName(appLocation), "Files");

        HtmlViewer.ConfigureVirtualHostNameToFolderMappingSettings(new VirtualHostNameToFolderMappingSettings
        {
            IsEnabled = true,
            Hostname = "darkassets.local",
            FolderPath = htmlAssetsDir,
            AccessKind = Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow
        });

        MainWindow = new DemoView();
        MainWindow.Show();
    }

    private static async Task CheckCompatibilityAsync()
    {
        var basicWatch = new System.Diagnostics.Stopwatch();
        var fullWatch = new System.Diagnostics.Stopwatch();

        //check compatibility - basic
        try
        {
            basicWatch.Start();
            HtmlViewer.CheckBasicCompatibility();
            basicWatch.Stop();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        return;

        //check compatibility - full
        try
        {
            fullWatch.Start();
            await HtmlViewer.CheckFullCompatibilityAsync();
            fullWatch.Stop();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        Console.WriteLine($"Basic: {basicWatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Full: {fullWatch.ElapsedMilliseconds}ms");
        Console.WriteLine();
    }
}
