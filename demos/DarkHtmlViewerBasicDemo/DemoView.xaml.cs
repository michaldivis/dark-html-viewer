using System.Collections.ObjectModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using System.ComponentModel;

namespace DarkHtmlViewerBasicDemo;

public partial class DemoView : Window
{
    private DemoItem? _currentItem;

    public ObservableCollection<DemoItem> Items { get; } = new();

    [GeneratedRegex("(?<itemCode>[a-zA-Z0-9]+)-.+", RegexOptions.Compiled | RegexOptions.Singleline)]
    private static partial Regex ItemCodeRegex();

    public DemoView()
    {
        InitializeComponent();

        DataContext = this;
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);

        var items = GenereateItems();

        foreach (var item in items)
        {
            Items.Add(item);
        }

        var firstItem = items.First();
        LoadItem(firstItem);
    }

    [RelayCommand]
    private void LoadItem(DemoItem? item)
    {
        if(item is null)
        {
            return;
        }

        _currentItem = item;

        TryExecute(htmlViewer.LoadCommand, item.Html);
    }

    private IEnumerable<DemoItem> GenereateItems()
    {
        return new List<DemoItem>
        {
            new DemoItem
            {
                Title = "Home",
                ItemCode = "home",
                Html = LoadHtml("home")
            },
            new DemoItem
            {
                Title = "Super cool looking code!",
                ItemCode = "page1",
                Html = LoadHtml("page1")
            },
            new DemoItem
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

        if (rawHtml is null)
        {
            return "<html><body><p>No content.</p></body></html>";
        }

        var preparedHtml = rawHtml.Replace("{htmlResDir}", "https://darkassets.local/");

        return preparedHtml;
    }

    [RelayCommand]
    private void HandleLinkClick(string? link)
    {
        Debug.WriteLine("Link clicked: " + link ?? "null");

        if (link is null)
        {
            return;
        }

        var itemCodeMatch = ItemCodeRegex().Match(link);
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
            TryExecute(htmlViewer.ScrollCommand, link);
            return;
        }

        TryExecute(htmlViewer.ScrollOnNextLoadCommand, link);

        LoadItem(item);
    }

    private static void TryExecute<T>(IRelayCommand<T> command, T? parameter)
    {
        if(command is null)
        {
            return;
        }

        if (command.CanExecute(parameter))
        {
            command.Execute(parameter);
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);

        htmlViewer.Cleanup();
    }

    private void sliderZoom_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        htmlViewer?.Zoom(e.NewValue);
    }
}
