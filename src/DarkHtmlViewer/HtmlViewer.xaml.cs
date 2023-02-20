using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DarkHtmlViewer;

public partial class HtmlViewer : UserControl
{
    private readonly ILogger<HtmlViewer> _logger;
    private readonly Guid _instanceId;
    private readonly TempFileManager _tempFileManager;

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

    public HtmlViewer()
    {
        InitializeComponent();

        _instanceId = Guid.NewGuid();

        _tempFileManager = new TempFileManager(_instanceId, GetLogger<TempFileManager>());

        _logger = GetLogger<HtmlViewer>();

        _logger.LogDebug("DarkHtmlViewer-{InstanceId}: Initializing", _instanceId);

        InitializeWebView2();
    }

    #region Initialization

    private bool _initialized;
    private string? _loadAfterInitialization = null;
    private async void InitializeWebView2()
    {
        var initFilePath = _tempFileManager.Create();

        _logger.LogDebug("DarkHtmlViewer-{InstanceId}: {Method}, init file: {InitFile}", _instanceId, nameof(InitializeWebView2), initFilePath);

        webView2.DefaultBackgroundColor = _defaultBackgroundColor;

        webView2.CoreWebView2InitializationCompleted += WebView2_CoreWebView2InitializationCompleted;
        webView2.NavigationStarting += WebView2_NavigationStarting;
        webView2.NavigationCompleted += WebView2_NavigationCompleted;

        var appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var dataFolder = Path.Combine(appDataDir, "webView2_env");
        var env = await CoreWebView2Environment.CreateAsync(null, dataFolder);

        await webView2.EnsureCoreWebView2Async(env);

        if(initFilePath is not null)
        {
            SetWebViewSource(initFilePath);
        }
    }    

    private void WebView2_CoreWebView2InitializationCompleted(object? sender, CoreWebView2InitializationCompletedEventArgs e)
    {
        _logger.LogDebug("DarkHtmlViewer-{InstanceId}: {Method}", _instanceId, nameof(WebView2_CoreWebView2InitializationCompleted));

        _initialized = true;

        webView2.Visibility = Visibility.Visible;        

        DisableAllExtraFunctionality(webView2.CoreWebView2.Settings);
        SetupVirtualHostForAssets(webView2);

        if (_loadAfterInitialization is not null)
        {
            Load(_loadAfterInitialization);
        }
    }

    private void SetWebViewSource(string uri)
    {
        webView2.CoreWebView2?.Navigate(uri);
    }

    #endregion

    #region Loading HTML content

    private static void OnHtmlContentCallBack(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        if(sender is not HtmlViewer viewer)
        {
            return;
        }

        viewer.Load(e.NewValue?.ToString() ?? string.Empty);
    }

    /// <summary>
    /// Loads an HTML string
    /// </summary>
    [RelayCommand]
    public void Load(string html)
    {
        if (_initialized is false)
        {
            _loadAfterInitialization = html;
            return;
        }

        var htmlFilePath = _tempFileManager.Create(html);

        if(htmlFilePath is null)
        {
            return;
        }

        _logger.LogDebug("DarkHtmlViewer-{InstanceId}: {Method}, file: {HtmlFilePath}", _instanceId, nameof(Load), htmlFilePath);

        SetWebViewSource(htmlFilePath);
    }

    #endregion

    #region Navigation

    private void WebView2_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        var linkName = Path.GetFileName(e.Uri);

        var isTempFileName = _tempFileManager.IsTempFileName(linkName);

        if (isTempFileName)
        {
            return;
        }

        e.Cancel = true;

        TriggerLinkClicked(linkName);
    }

    private void TriggerLinkClicked(string? link)
    {
        if(LinkClickedCommand is null)
        {
            return;
        }

        if (LinkClickedCommand.CanExecute(link))
        {
            LinkClickedCommand.Execute(link);
        }
    }

    private async void WebView2_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (_scrollToNext is not null)
        {
            await ScrollAsync(_scrollToNext);
            _scrollToNext = null;
        }
        else if(_scrollPositionXNext is not null && _scrollPositionYNext is not null)
        {
            await ScrollAsync(_scrollPositionXNext, _scrollPositionYNext);
            _scrollPositionXNext = null;
            _scrollPositionYNext = null;
        }

        if (_textToFind is not null)
        {
            await SearchAsync(_textToFind);
            _textToFind = null;
        }
    }

    #endregion

    #region Scroll to

    private string? _scrollToNext = null;

    /// <summary>
    /// Tries to scroll to an element by ID
    /// </summary>
    [RelayCommand]
    public async Task ScrollAsync(string elementId)
    {
        var script = $"document.getElementById(\"{elementId}\").scrollIntoView();";
        await webView2.ExecuteScriptAsync(script);
    }

    /// <summary>
    /// Tries to scroll to a position
    /// </summary>
    private async Task ScrollAsync(string x, string y)
    {
        var script = $"window.scrollTo({x}, {y});";
        await webView2.ExecuteScriptAsync(script);
    }

    /// <summary>
    /// Sets the elementId to be scrolled to next time HTML content is loaded
    /// </summary>
    [RelayCommand]
    public void ScrollOnNextLoad(string elementId)
    {
        _scrollToNext = elementId;
    }

    #endregion

    #region Save scroll position

    private string? _scrollPositionXNext = null;
    private string? _scrollPositionYNext = null;

    /// <summary>
    /// Saves the current scroll position and tries to restore it next time HTML content is loaded
    /// <para>If <see cref="ScrollOnNextLoad"/> is used as well, this will be ignored</para>
    /// </summary>
    [RelayCommand]
    public async Task SaveScrollPositionForNextLoadAsync()
    {
        _scrollPositionXNext = await webView2.ExecuteScriptAsync("window.scrollX");
        _scrollPositionYNext = await webView2.ExecuteScriptAsync("window.scrollY");
    }

    #endregion

    #region Search

    private string? _textToFind = null;

    /// <summary>
    /// Finds text in the loaded HTML
    /// </summary>
    [RelayCommand]
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

    private static string? CleanSearchText(string text)
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
    [RelayCommand]
    public void SearchOnNextLoad(string text)
    {
        _textToFind = text;
    }

    #endregion

    #region Printing

    /// <summary>
    /// Invokes the browser print dialog
    /// </summary>
    [RelayCommand]
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
    [RelayCommand]
    public void Zoom(double zoom)
    {
        webView2.ZoomFactor = zoom;
    }

    #endregion

    #region Virtual host

    private static VirtualHostNameToFolderMappingSettingsValidator? _virtualAssetSettingsValidator;

    private static VirtualHostNameToFolderMappingSettings? _virtualAssetSettings;

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
            var errors = validationResult.Errors.ToDictionary(x => x.PropertyName, x => x.ErrorMessage);
            throw new VirtualHostNameToFolderMappingSettingsException(errors);
        }

        _virtualAssetSettings = settings;
    }

    private static void SetupVirtualHostForAssets(WebView2 webView)
    {
        if (_virtualAssetSettings is null)
        {
            return;
        }

        webView.CoreWebView2.SetVirtualHostNameToFolderMapping(_virtualAssetSettings.Hostname, _virtualAssetSettings.FolderPath, _virtualAssetSettings.AccessKind);
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

    private static Func<ILoggerFactory>? _loggerFactoryProvider;

    public static void ConfigureLogger(Func<ILoggerFactory> loggerFactoryProvider)
    {
        _loggerFactoryProvider = loggerFactoryProvider;
    }

    private static ILogger<T> GetLogger<T>()
    {
        if (_loggerFactoryProvider is null)
        {
            return NullLogger<T>.Instance;
        }

        return _loggerFactoryProvider.Invoke().CreateLogger<T>();
    }

    #endregion

    #region Cleanup

    /// <summary>
    /// Removes the temporary files created by the control
    /// </summary>
    public void Cleanup()
    {
        _tempFileManager.TryDeletePreviousFiles();
    }

    #endregion

    #region Theme

    private static Color _defaultBackgroundColor = Color.White;

    /// <summary>
    /// Configure the default background color for the WebView
    /// </summary>
    /// <param name="defaultBackgroundColor">The background color</param>
    public static void ConfigureDefaultBackgroundColor(Color defaultBackgroundColor)
    {
        _defaultBackgroundColor = defaultBackgroundColor;
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
