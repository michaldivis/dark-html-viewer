using DarkHelpers.Commands;
using Microsoft.Web.WebView2.Core;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using DarkHelpers;

namespace DarkHtmlViewer
{
    public partial class DarkHtmlViewer : UserControl
    {
        private readonly ILogger<DarkHtmlViewer> _logger;
        private readonly Guid _instanceId;
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
        public ICommand ScrollOnNextLoadCommand => new DarkCommand<string>(ScrollOnNextLoad);
        public ICommand SearchCommand => new DarkAsyncCommand<string>(SearchAsync);
        public ICommand SearchOnNextLoadCommand => new DarkCommand<string>(SearchOnNextLoad);
        public ICommand PrintCommand => new DarkAsyncCommand(PrintAsync);

        #endregion

        public DarkHtmlViewer()
        {
            InitializeComponent();

            _instanceId = Guid.NewGuid();

            _fileManager = new DarkHtmlTempFileManager(_instanceId, GetLogger<DarkHtmlTempFileManager>());

            _logger = GetLogger<DarkHtmlViewer>();

            _logger.LogDebug("DarkHtmlViewer-{InstanceId}: Initializing", _instanceId);

            InitializeWebView2();
        }

        #region Initialization

        private bool _initialized;
        private string _loadAfterInitialization = null;
        private async void InitializeWebView2()
        {
            var initFilePath = _fileManager.GetFilePath();
            _logger.LogDebug("DarkHtmlViewer-{InstanceId}: {Method}, init file: {InitFile}", _instanceId, nameof(InitializeWebView2), initFilePath);

            webView2.CoreWebView2InitializationCompleted += WebView2_CoreWebView2InitializationCompleted;
            webView2.NavigationStarting += WebView2_NavigationStarting;
            webView2.NavigationCompleted += WebView2_NavigationCompleted;

            webView2.CoreWebView2InitializationCompleted += WebView2_CoreWebView2InitializationCompleted;
            webView2.NavigationStarting += WebView2_NavigationStarting;
            webView2.NavigationCompleted += WebView2_NavigationCompleted;

            var appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var dataFolder = Path.Combine(appDataDir, "webView2_env");
            var env = await CoreWebView2Environment.CreateAsync(null, dataFolder);

            await webView2.EnsureCoreWebView2Async(env);

            SetWebViewSource(initFilePath);

            webView2.Visibility = Visibility.Visible;

            _initialized = true;

            LoadQeuedContentIfAny();
        }

        private void SetWebViewSource(string uri)
        {
            webView2.CoreWebView2?.Navigate(uri);
        }

        private void LoadQeuedContentIfAny()
        {
            if (_loadAfterInitialization is null)
            {
                return;
            }

            LoadHtmlContent(_loadAfterInitialization);
        }

        private void WebView2_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            _logger.LogDebug("DarkHtmlViewer-{InstanceId}: {Method}", _instanceId, nameof(WebView2_CoreWebView2InitializationCompleted));
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
            if (_initialized is false)
            {
                _loadAfterInitialization = html;
                return;
            }

            _fileManager.Create(html);
            var htmlFilePath = _fileManager.GetFilePath();
            _logger.LogDebug("DarkHtmlViewer-{InstanceId}: {Method}, file: {HtmlFilePath}", _instanceId, nameof(LoadHtmlContent), htmlFilePath);
            SetWebViewSource(htmlFilePath);
        }

        #endregion

        #region Navigation

        private void WebView2_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            var linkName = Path.GetFileName(e.Uri);

            var isTempFileName = _fileManager.IsTempFilePath(linkName);

            if (isTempFileName)
            {
                return;
            }

            e.Cancel = true;

            TriggerLinkClicked(linkName);
        }

        private void TriggerLinkClicked(string link)
        {
            LinkClickedCommand?.TryExecute(link);
        }

        private async void WebView2_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (_scrollToNext is not null)
            {
                await ScrollAsync(_scrollToNext);
                _scrollToNext = null;
            }

            if (_textToFind is not null)
            {
                await SearchAsync(_textToFind);
                _textToFind = null;
            }
        }

        #endregion

        #region Scroll to

        private string _scrollToNext = null;

        private async Task ScrollAsync(string link)
        {
            var script = $"document.getElementById(\"{link}\").scrollIntoView();";
            await webView2.ExecuteScriptAsync(script);
        }

        private void ScrollOnNextLoad(string link)
        {
            _scrollToNext = link;
        }

        #endregion

        #region Search

        private string _textToFind = null;

        private async Task SearchAsync(string text)
        {
            var clean = CleanSearchText(text);
            if (string.IsNullOrEmpty(clean))
            {
                return;
            }

            //TODO try to find a way to highlight all occurances instead of just the first one
            var script = $"window.find('{clean}');";
            await webView2.ExecuteScriptAsync(script);
        }

        private string CleanSearchText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            var clean = text.Replace("'", "");

            if (string.IsNullOrEmpty(clean))
            {
                return null;
            }

            return clean;
        }

        private void SearchOnNextLoad(string text)
        {
            _textToFind = text;
        }

        #endregion

        #region Printing

        private async Task PrintAsync()
        {
            var script = "window.print();";
            await webView2.ExecuteScriptAsync(script);
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
            webView2.CoreWebView2.Settings.IsBuiltInErrorPageEnabled = false;
            webView2.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = true;
            webView2.CoreWebView2.Settings.IsGeneralAutofillEnabled = false;
            webView2.CoreWebView2.Settings.IsPasswordAutosaveEnabled = false;
        }

        #endregion

        #region Logging

        private static Func<ILoggerFactory> LoggerFactoryProvider;

        public static void ConfigureLogger(Func<ILoggerFactory> loggerFactoryProvider)
        {
            LoggerFactoryProvider = loggerFactoryProvider;
        }

        private static ILogger<T> GetLogger<T>()
        {
            if(LoggerFactoryProvider is null)
            {
                return NullLogger<T>.Instance;
            }

            return LoggerFactoryProvider.Invoke().CreateLogger<T>();
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Removes the temporary files created by the control
        /// </summary>
        public void Cleanup()
        {
            _fileManager.TryDeleteCurrentTempFile();
        }

        #endregion
    }
}
