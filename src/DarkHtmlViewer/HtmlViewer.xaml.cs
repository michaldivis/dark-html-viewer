using DarkHelpers;
using DarkHelpers.Commands;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DarkHtmlViewer
{
    public partial class HtmlViewer : UserControl
    {
        private readonly ILogger<HtmlViewer> _logger;
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
                typeof(HtmlViewer),
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
                typeof(HtmlViewer),
                new PropertyMetadata(null));

        #endregion

        #region Commands

        public ICommand LoadCommand => new DarkCommand<string>(Load);
        public ICommand ScrollCommand => new DarkAsyncCommand<string>(ScrollAsync);
        public ICommand ScrollOnNextLoadCommand => new DarkCommand<string>(ScrollOnNextLoad);
        public ICommand SearchCommand => new DarkAsyncCommand<string>(SearchAsync);
        public ICommand SearchOnNextLoadCommand => new DarkCommand<string>(SearchOnNextLoad);
        public ICommand PrintCommand => new DarkAsyncCommand(PrintAsync);
        public ICommand ZoomCommand => new DarkCommand<double>(Zoom);

        #endregion

        public HtmlViewer()
        {
            InitializeComponent();

            _instanceId = Guid.NewGuid();

            _fileManager = new DarkHtmlTempFileManager(_instanceId, GetLogger<DarkHtmlTempFileManager>());

            _logger = GetLogger<HtmlViewer>();

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

            Load(_loadAfterInitialization);
        }

        private void WebView2_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            _logger.LogDebug("DarkHtmlViewer-{InstanceId}: {Method}", _instanceId, nameof(WebView2_CoreWebView2InitializationCompleted));
            DisableAllExtraFunctionality(webView2.CoreWebView2.Settings);
            SetupVirtualHostForAssets(webView2);
        }

        #endregion

        #region Loading HTML content

        private static void OnHtmlContentCallBack(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var viewer = sender as HtmlViewer;
            if (viewer != null)
            {
                viewer.Load(e.NewValue?.ToString());
            }
        }

        /// <summary>
        /// Loads an HTML string
        /// </summary>
        public void Load(string html)
        {
            if (_initialized is false)
            {
                _loadAfterInitialization = html;
                return;
            }

            _fileManager.Create(html);
            var htmlFilePath = _fileManager.GetFilePath();
            _logger.LogDebug("DarkHtmlViewer-{InstanceId}: {Method}, file: {HtmlFilePath}", _instanceId, nameof(Load), htmlFilePath);
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

        /// <summary>
        /// Tries to scroll to an element by ID
        /// </summary>
        public async Task ScrollAsync(string elementId)
        {
            var script = $"document.getElementById(\"{elementId}\").scrollIntoView();";
            await webView2.ExecuteScriptAsync(script);
        }

        /// <summary>
        /// Sets the elementId to be scrolled to next time HTML content is loaded
        /// </summary>
        public void ScrollOnNextLoad(string elementId)
        {
            _scrollToNext = elementId;
        }

        #endregion

        #region Search

        private string _textToFind = null;

        /// <summary>
        /// Finds text in the loaded HTML
        /// </summary>
        public async Task SearchAsync(string text)
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

        /// <summary>
        /// Sets the text to be searched for next time HTML content is loaded
        /// </summary>
        public void SearchOnNextLoad(string text)
        {
            _textToFind = text;
        }

        #endregion

        #region Printing

        /// <summary>
        /// Invokes the browser print dialog
        /// </summary>
        public async Task PrintAsync()
        {
            var script = "window.print();";
            await webView2.ExecuteScriptAsync(script);
        }

        #endregion

        #region Zoom

        /// <summary>
        /// Sets the zoom factor of the browser
        /// </summary>
        /// <param name="zoom">Zoom factor, 1.0 is default</param>
        public void Zoom(double zoom)
        {
            webView2.ZoomFactor = zoom;
        }

        #endregion

        #region Virtual host

        private static VirtualHostNameToFolderMappingSettingsValidator _virtualAssetSettingsValidator;

        private static VirtualHostNameToFolderMappingSettings VirtualAssetSettings;

        /// <summary>
        /// Configure the virtual host to folder mapping
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="VirtualHostNameToFolderMappingSettingsException"></exception>
        public static void ConfigureVirtualHostNameToFolderMappingSettings(VirtualHostNameToFolderMappingSettings settings)
        {
            if (settings is null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            var validator = _virtualAssetSettingsValidator ??= new VirtualHostNameToFolderMappingSettingsValidator();

            var validationResult = validator.Validate(settings);

            if (!validationResult.IsValid)
            {
                var firstError = validationResult.Errors.FirstOrDefault();
                throw new VirtualHostNameToFolderMappingSettingsException(firstError.PropertyName, firstError.ErrorMessage);
            }

            VirtualAssetSettings = settings;
        }

        private static void SetupVirtualHostForAssets(WebView2 webView)
        {
            if (VirtualAssetSettings is null)
            {
                return;
            }

            if (VirtualAssetSettings.IsEnabled is false)
            {
                return;
            }

            webView.CoreWebView2.SetVirtualHostNameToFolderMapping(VirtualAssetSettings.Hostname, VirtualAssetSettings.FolderPath, VirtualAssetSettings.AccessKind);
        }

        #endregion

        #region Browser settings

        private static void DisableAllExtraFunctionality(CoreWebView2Settings settings)
        {
            settings.AreBrowserAcceleratorKeysEnabled = true;
            settings.AreDefaultContextMenusEnabled = false;
            settings.AreDefaultScriptDialogsEnabled = false;
            settings.AreDevToolsEnabled = false;

            settings.IsScriptEnabled = false;
            settings.IsStatusBarEnabled = false;
            settings.IsWebMessageEnabled = false;
            settings.IsZoomControlEnabled = false;
            settings.IsBuiltInErrorPageEnabled = false;

            settings.IsGeneralAutofillEnabled = false;
            settings.IsPasswordAutosaveEnabled = false;
            settings.IsSwipeNavigationEnabled = false;
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
            if (LoggerFactoryProvider is null)
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

        #region Utils

        /// <summary>
        /// <para>Check installed runtime compatibility - basic</para>
        /// <para>Tries to retreive the available browser version</para>
        /// <para>Throws if there's a problem</para>
        /// </summary>
        /// <exception cref="WebView2RuntimeNotFoundException" />
        public static void CheckBasicCompatibility()
        {
            _ = CoreWebView2Environment.GetAvailableBrowserVersionString();
        }

        /// <summary>
        /// <para>Check installed runtime compatibility - full</para>
        /// <para>Tries to create an instance of a <see cref="WebView2"/> with valid <see cref="CoreWebView2Environment"/>, <see cref="CoreWebView2Settings"/> and <see cref="VirtualHostNameToFolderMappingSettings"/> (if enabled)</para>
        /// <para>Throws if there's a problem</para>
        /// </summary>
        /// <exception cref="Exception" />
        /// <exception cref="ArgumentException" />
        /// <exception cref="InvalidOperationException" />
        /// <exception cref="ObjectDisposedException" />
        /// <exception cref="WebView2RuntimeNotFoundException" />
        public static async Task CheckFullCompatibilityAsync()
        {
            try
            {
                var webView = new WebView2()
                {
                    Source = new Uri("https://www.google.com")
                };

                var env = await CoreWebView2Environment.CreateAsync().ConfigureAwait(false);

                await webView.EnsureCoreWebView2Async().ConfigureAwait(false);

                DisableAllExtraFunctionality(webView.CoreWebView2.Settings);
                SetupVirtualHostForAssets(webView);
            }
            catch (Exception)
            {
                throw;
            }
        }

        #endregion
    }
}
