using DarkHelpers;
using DarkHelpers.Commands;
using DarkHtmlViewer;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;

namespace DarkHtmlViewerBasicDemo
{
    public class DemoViewModel
    {
        public ICommand MyLinkClickedCommand => new DarkCommand<string>(link => Debug.WriteLine($"Link clicked: {link}"));

        public DarkObservableCollection<DemoItem> Items { get; set; } = new DarkObservableCollection<DemoItem>();

        public LoadAndScrollData TestLoadAndScrollData { get; set; }

        public DemoViewModel()
        {
            IEnumerable<DemoItem> items = GenereateItems();
            Items.AddRange(items);

            TestLoadAndScrollData = new LoadAndScrollData
            {
                HtmlContent = Items.FirstOrDefault().Html,
                Link = "footer"
            };
        }

        private IEnumerable<DemoItem> GenereateItems()
        {
            IEnumerable<DemoItem> items = Enumerable.Range(1, 5)
                .Select(a => GenerateItem(a));
            return items;
        }

        private DemoItem GenerateItem(int index)
        {
            return new DemoItem
            {
                Title = $"Page {index}",
                Html = GenerateHtml(index)
            };
        }

        private string GenerateHtml(int index)
        {
            var headContent = "<link rel=\"preconnect\" href=\"https://fonts.gstatic.com\"><link href=\"https://fonts.googleapis.com/css2?family=Open+Sans:ital,wght@0,300;0,400;0,600;0,700;0,800;1,300;1,400;1,600;1,700;1,800&display=swap\" rel=\"stylesheet\"> <style> body { font-family: 'Open Sans', sans-serif; } </style>";

            return $"<html><head>{headContent}</head><body><h2>Page {index}</h2><p>Hello there, this is a test page #{index}</p><p>Click <a href=\"someLink{index}\">here</a></p><p>Fusce suscipit libero eget elit. Mauris tincidunt sem sed arcu. Class aptent taciti sociosqu ad litora torquent per conubia nostra, per inceptos hymenaeos. Vestibulum erat nulla, ullamcorper nec, rutrum non, nonummy ac, erat. Proin in tellus sit amet nibh dignissim sagittis. Nullam feugiat, turpis at pulvinar vulputate, erat libero tristique tellus, nec bibendum odio risus sit amet ante. Nam libero tempore, cum soluta nobis est eligendi optio cumque nihil impedit quo minus id quod maxime placeat facere possimus, omnis voluptas assumenda est, omnis dolor repellendus. Duis viverra diam non justo. Integer in sapien. Phasellus rhoncus. Quis autem vel eum iure reprehenderit qui in ea voluptate velit esse quam nihil molestiae consequatur, vel illum qui dolorem eum fugiat quo voluptas nulla pariatur? Aenean placerat. Fusce tellus odio, dapibus id fermentum quis, suscipit id erat. Nullam dapibus fermentum ipsum. Duis sapien nunc, commodo et, interdum suscipit, sollicitudin et, dolor. Aliquam erat volutpat.Etiam commodo dui eget wisi. Mauris elementum mauris vitae tortor. Donec quis nibh at felis congue commodo. Proin in tellus sit amet nibh dignissim sagittis. Pellentesque habitant morbi tristique senectus et netus et malesuada fames ac turpis egestas. Duis condimentum augue id magna semper rutrum. Cras elementum. Class aptent taciti sociosqu ad litora torquent per conubia nostra, per inceptos hymenaeos. Maecenas lorem. Nulla quis diam. Mauris suscipit, ligula sit amet pharetra semper, nibh ante cursus purus, vel sagittis velit mauris vel metus. Maecenas ipsum velit, consectetuer eu lobortis ut, dictum at dui. Fusce aliquam vestibulum ipsum. Nullam dapibus fermentum ipsum. Nulla accumsan, elit sit amet varius semper, nulla mauris mollis quam, tempor suscipit diam nulla vel leo. Donec iaculis gravida nulla. Etiam bibendum elit eget erat. Aenean id metus id velit ullamcorper pulvinar. Nullam sapien sem, ornare ac, nonummy non, lobortis a enim. Cum sociis natoque penatibus et magnis dis parturient montes, nascetur ridiculus mus. Curabitur sagittis hendrerit ante. Morbi leo mi, nonummy eget tristique non, rhoncus non leo. Vivamus porttitor turpis ac leo. Mauris dolor felis, sagittis at, luctus sed, aliquam non, tellus. Mauris dictum facilisis augue. Fusce tellus odio, dapibus id fermentum quis, suscipit id erat. Aliquam erat volutpat. Vestibulum erat nulla, ullamcorper nec, rutrum non, nonummy ac, erat. Aenean vel massa quis mauris vehicula lacinia. Curabitur ligula sapien, pulvinar a vestibulum quis, facilisis vel sapien. Praesent id justo in neque elementum ultrices. Nemo enim ipsam voluptatem quia voluptas sit aspernatur aut odit aut fugit, sed quia consequuntur magni dolores eos qui ratione voluptatem sequi nesciunt. Vivamus ac leo pretium faucibus. Praesent dapibus. Morbi leo mi, nonummy eget tristique non, rhoncus non leo. Pellentesque arcu. Nam quis nulla. Pellentesque habitant morbi tristique senectus et netus et malesuada fames ac turpis egestas.Nulla quis diam. Sed vel lectus. Donec odio tempus molestie, porttitor ut, iaculis quis, sem. In dapibus augue non sapien. Et harum quidem rerum facilis est et expedita distinctio. Sed vel lectus. Donec odio tempus molestie, porttitor ut, iaculis quis, sem. Praesent in mauris eu tortor porttitor accumsan. Itaque earum rerum hic tenetur a sapiente delectus, ut aut reiciendis voluptatibus maiores alias consequatur aut perferendis doloribus asperiores repellat. Curabitur sagittis hendrerit ante. Etiam bibendum elit eget erat. Duis bibendum, lectus ut viverra rhoncus, dolor nunc faucibus libero, eget facilisis enim ipsum id lacus. Duis sapien nunc, commodo et, interdum suscipit, sollicitudin et, dolor. Duis viverra diam non justo. Duis condimentum augue id magna semper rutrum. Nulla turpis magna, cursus sit amet, suscipit a, interdum id, felis. Nunc auctor. Duis sapien nunc, commodo et, interdum suscipit, sollicitudin et, dolor.</p><div id=\"footer\"><h4>Footer</h4><p>We wish you the best from page #{index}</p></div></body></html>";
        }
    }
}
