using CefSharp.Wpf;
using CefSharp;
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

        ConfigureCefSharp();

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

        MainWindow = new MainWindow();
        MainWindow.Show();
    }

    private static void ConfigureCefSharp()
    {
#if ANYCPU
            //Only required for PlatformTarget of AnyCPU
            CefRuntime.SubscribeAnyCpuAssemblyResolver();
#endif
        var settings = new CefSettings()
        {
            //By default CefSharp will use an in-memory cache, you need to specify a Cache Folder to persist data
            CachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CefSharp\\Cache")
        };

        settings.CefCommandLineArgs.Add("enable-media-stream");
        settings.CefCommandLineArgs.Add("use-fake-ui-for-media-stream");
        settings.CefCommandLineArgs.Add("enable-usermedia-screen-capturing");

        if (!Cef.IsInitialized)
        {
            Cef.Initialize(settings, performDependencyCheck: false, browserProcessHandler: null);
        }
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
