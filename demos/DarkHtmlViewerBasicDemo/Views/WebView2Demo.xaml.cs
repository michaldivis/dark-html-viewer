using DarkHelpers;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;

namespace DarkHtmlViewerBasicDemo.Views;
public partial class WebView2Demo : UserControl
{
    private DemoItem _currentItem;

    public ICommand LoadItemCommand { get; }
    public ICommand HandleLinkClickCommand { get; }

    public DarkObservableCollection<DemoItem> Items { get; } = new();

    public WebView2Demo()
    {
        InitializeComponent();

        LoadItemCommand = new RelayCommand<DemoItem>(LoadItem);
        HandleLinkClickCommand = new RelayCommand<string>(HandleLinkClick);

        DataContext = this;

        Loaded += WebView2Demo_Loaded;
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

    private static IEnumerable<DemoItem> GenereateItems()
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

    private static string LoadHtml(string itemCode)
    {
        var rawHtml = itemCode switch
        {
            "home" => Properties.Resources.home,
            "page1" => Properties.Resources.page1,
            "page2" => Properties.Resources.page2,
            _ => null
        };

        var preparedHtml = rawHtml.Replace("{htmlResDir}", "https://darkassets.local/");

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
