using DarkHelpers.Commands;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DarkHtmlViewer
{
    public partial class DarkHtmlViewer : UserControl
    {
        private readonly DarkHtmlTempFileManager _fileManager;

        #region Bindable properties

        public string HtmlContent
        {
            get => (string)GetValue(HtmlContentProperty);
            set => SetValue(HtmlContentProperty, value);
        }

        public static readonly DependencyProperty HtmlContentProperty =
            DependencyProperty.Register(
                nameof(HtmlContent),
                typeof(string),
                typeof(DarkHtmlViewer),
                new PropertyMetadata(
                    string.Empty,
                    new PropertyChangedCallback(OnHtmlContentCallBack)));

        public ICommand LinkClickedCommand
        {
            get => (ICommand)GetValue(LinkClickedCommandProperty);
            set => SetValue(LinkClickedCommandProperty, value);
        }

        public static readonly DependencyProperty LinkClickedCommandProperty =
            DependencyProperty.Register(
                nameof(LinkClickedCommand),
                typeof(ICommand),
                typeof(DarkHtmlViewer),
                new PropertyMetadata(null));

        #endregion

        #region Commands

        public ICommand LoadCommand => new DarkCommand<string>(LoadHtmlContent);
        public ICommand ScrollCommand => new DarkAsyncCommand<string>(ScrollAsync);
        public ICommand LoadAndScrollCommand => new DarkAsyncCommand<LoadAndScrollData>(LoadAndScrollAsync);

        #endregion

        public DarkHtmlViewer()
        {
            InitializeComponent();

            _fileManager = new DarkHtmlTempFileManager();

            InitializeWebView2();
        }

        #region Initialization

        private void InitializeWebView2()
        {
            webView2.Source = new Uri(_fileManager.GetFilePath());

            webView2.CoreWebView2InitializationCompleted += WebView2_CoreWebView2InitializationCompleted;
            webView2.NavigationStarting += WebView2_NavigationStarting;
        }

        private void WebView2_CoreWebView2InitializationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2InitializationCompletedEventArgs e)
        {
            DisableAllExtraFunctionality();
        }

        #endregion

        #region Loading HTML content

        private static void OnHtmlContentCallBack(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var viewer = sender as DarkHtmlViewer;
            if (viewer != null)
            {
                viewer.LoadHtmlContent(e.NewValue?.ToString());
            }
        }

        private void LoadHtmlContent(string html)
        {
            _fileManager.Create(html);
            webView2.Source = new Uri(_fileManager.GetFilePath());
        }

        #endregion

        #region Navigation

        private void WebView2_NavigationStarting(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs e)
        {
            string linkName = Path.GetFileName(e.Uri);

            string currentFileName = Path.GetFileName(_fileManager.GetFilePath());

            if (linkName == currentFileName)
            {
                return;
            }

            e.Cancel = true;

            TriggerLinkClicked(linkName);
        }

        private void TriggerLinkClicked(string link)
        {
            bool canExecute = LinkClickedCommand?.CanExecute(link) ?? false;

            if (canExecute is false)
            {
                return;
            }

            LinkClickedCommand?.Execute(link);
        }

        #endregion

        #region Scroll to

        private async Task ScrollAsync(string link)
        {
            string script = $"document.getElementById(\"{link}\").scrollIntoView();";
            await webView2.ExecuteScriptAsync(script);
        }

        private async Task LoadAndScrollAsync(LoadAndScrollData data)
        {
            LoadHtmlContent(data.HtmlContent);
            await ScrollAsync(data.Link);
        }

        #endregion

        #region Browser settings

        private void DisableAllExtraFunctionality()
        {
            webView2.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            webView2.CoreWebView2.Settings.AreDevToolsEnabled = false;
            webView2.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = false;
            webView2.CoreWebView2.Settings.IsScriptEnabled = false;
            webView2.CoreWebView2.Settings.IsStatusBarEnabled = false;
            webView2.CoreWebView2.Settings.IsWebMessageEnabled = false;
            webView2.CoreWebView2.Settings.IsZoomControlEnabled = false;
        }

        #endregion
    }
}
