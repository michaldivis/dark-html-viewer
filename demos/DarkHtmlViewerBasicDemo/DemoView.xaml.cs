using DarkHelpers;
using System;
using System.Linq;
using System.Windows;

namespace DarkHtmlViewerBasicDemo
{
    public partial class DemoView : Window
    {
        public DemoView()
        {
            InitializeComponent();
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            TryLoadFirstItemHtml();
        }

        private void TryLoadFirstItemHtml()
        {
            if(DataContext is not DemoViewModel vm)
            {
                return;
            }

            var firstItemHtml = vm.Items.FirstOrDefault()?.Html;
            if(firstItemHtml is null)
            {
                return;
            }

            darkHtmlViewer.LoadCommand.TryExecute(firstItemHtml);
        }
    }
}
