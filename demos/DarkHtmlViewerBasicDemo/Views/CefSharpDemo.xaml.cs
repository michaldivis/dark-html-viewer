using DarkHelpers;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using CefSharp.Wpf;
using CefSharp;
using System;
using System.IO;
using DarkHtmlViewer;
using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using CefSharp.SchemeHandler;

namespace DarkHtmlViewerBasicDemo.Views;
public partial class CefSharpDemo : UserControl
{
    private DemoItem _currentItem;

    public ICommand LoadItemCommand { get; }
    public ICommand HandleLinkClickCommand { get; }

    public DarkObservableCollection<DemoItem> Items { get; } = new();

    public CefSharpDemo()
    {
        InitializeComponent();

        ConfigureCefSharpViewer();

        LoadItemCommand = new RelayCommand<DemoItem>(LoadItem);
        HandleLinkClickCommand = new RelayCommand<string>(HandleLinkClick);

        DataContext = this;

        Loaded += WebView2Demo_Loaded;
    }

    private void ConfigureCefSharpViewer()
    {
        HtmlViewer.ConfigureLogger(() => NullLoggerFactory.Instance);

#if ANYCPU
            //Only required for PlatformTarget of AnyCPU
            CefRuntime.SubscribeAnyCpuAssemblyResolver();
#endif
        var settings = new CefSettings()
        {
            //By default CefSharp will use an in-memory cache, you need to specify a Cache Folder to persist data
            CachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CefSharp\\Cache")
        };

        var appLocation = Assembly.GetExecutingAssembly().Location;
        var htmlAssetsDir = Path.Combine(Path.GetDirectoryName(appLocation), "Files");

        settings.RegisterScheme(new CefCustomScheme
        {
            SchemeName = "localfolder",
            DomainName = "darkcefassets",
            SchemeHandlerFactory = new FolderSchemeHandlerFactory(
                rootFolder: htmlAssetsDir
            )
        });

        settings.CefCommandLineArgs.Add("enable-media-stream");
        settings.CefCommandLineArgs.Add("use-fake-ui-for-media-stream");
        settings.CefCommandLineArgs.Add("enable-usermedia-screen-capturing");

        if (!Cef.IsInitialized)
        {
            Cef.Initialize(settings, performDependencyCheck: false, browserProcessHandler: null);
        }
    }

    private void WebView2Demo_Loaded(object sender, RoutedEventArgs e)
    {
        var items = GenereateItems();
        Items.AddRange(items);

        var firstItem = items.First();
        LoadItem(firstItem);
    }

    private void LoadItem(DemoItem item)
    {
        _currentItem = item;
        htmlViewer.LoadCommand.TryExecute(item.Html);
    }

    private IEnumerable<DemoItem> GenereateItems()
    {
        return new List<DemoItem>
        {
            new()
            {
                Title = "Home",
                ItemCode = "home",
                Html = LoadHtml("home")
            },
            new()
            {
                Title = "Super cool looking code!",
                ItemCode = "page1",
                Html = LoadHtml("page1")
            },
            new()
            {
                Title = "What the hell are NFTs?",
                ItemCode = "page2",
                Html = LoadHtml("page2")
            },
        };
    }

    private string LoadHtml(string itemCode)
    {
        var rawHtml = itemCode switch
        {
            "home" => Properties.Resources.home,
            "page1" => Properties.Resources.page1,
            "page2" => Properties.Resources.page2,
            _ => null
        };

        var preparedHtml = rawHtml.Replace("{htmlResDir}", "localfolder://darkcefassets/");

        return preparedHtml;
    }

    private static readonly Regex ItemCodeRegex = new(@"(?<itemCode>[a-zA-Z0-9]+)-.+");

    private void HandleLinkClick(string link)
    {
        var itemCodeMatch = ItemCodeRegex.Match(link);
        if (!itemCodeMatch.Success)
        {
            return;
        }

        var itemCode = itemCodeMatch.Groups["itemCode"].Value;
        var item = Items.FirstOrDefault(a => a.ItemCode == itemCode);
        if (item is null)
        {
            return;
        }

        if (item == _currentItem)
        {
            htmlViewer.ScrollCommand.TryExecute(link);
            return;
        }

        htmlViewer.ScrollOnNextLoadCommand.TryExecute(link);

        LoadItem(item);
    }
}
