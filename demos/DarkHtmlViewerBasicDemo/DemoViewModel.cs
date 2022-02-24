using DarkHelpers;
using DarkHelpers.Commands;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;

namespace DarkHtmlViewerBasicDemo
{
    public class DemoViewModel
    {
        public ICommand MyLinkClickedCommand => new DarkCommand<string>(link => Debug.WriteLine($"Link clicked: {link}"));

        public DarkObservableCollection<DemoItem> Items { get; set; } = new DarkObservableCollection<DemoItem>();

        public DemoViewModel()
        {
            var items = GenereateItems();
            Items.AddRange(items);
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
                },new DemoItem
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

            var appLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var htmlResDir = Path.Combine(Path.GetDirectoryName(appLocation), "Files");
            var preparedHtml = rawHtml.Replace("{htmlResDir}", $"{htmlResDir}\\");

            return preparedHtml;
        }
    }
}
