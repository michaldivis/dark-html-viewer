using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using System.Windows;
using System;
using System.Windows.Controls;
using System.Windows.Input;
using DarkHelpers;
using CommunityToolkit.Mvvm.Input;
using CefSharp;
using System.Text.RegularExpressions;

namespace DarkHtmlViewer.Cef
{
    public partial class HtmlViewer : UserControl
    {
        private readonly ILogger<HtmlViewer> _logger;
        private readonly Guid _instanceId;

        internal const string BaseUrl = "https://darkHtmlViewer_cef/";

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

            _logger = GetLogger<HtmlViewer>();

            _logger.LogDebug("DarkHtmlViewer-Cef-{InstanceId}: Initializing", _instanceId);

            InitializeCefBrowser();
        }

        #region Initialization

        private bool _initialized;
        private string _loadAfterInitialization = null;
        private void InitializeCefBrowser()
        {
            cefBrowser.RequestHandler = new CustomRequestHandler(TriggerLinkClicked);

            _initialized = true;

            LoadQeuedContentIfAny();
        }

        private void LoadQeuedContentIfAny()
        {
            if (_loadAfterInitialization is null)
            {
                return;
            }

            Load(_loadAfterInitialization);
        }

        #endregion

        #region Loading HTML content

        private static void OnHtmlContentCallBack(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is HtmlViewer viewer)
            {
                viewer.Load(e.NewValue?.ToString());
            }
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

            cefBrowser.LoadHtml(html, BaseUrl);

            OnLoadCompleted();
        }

        private void OnLoadCompleted()
        {
            if (_scrollToNext is not null)
            {
                Scroll(_scrollToNext);
                _scrollToNext = null;
            }

            if (_textToFind is not null)
            {
                Search(_textToFind);
                _textToFind = null;
            }
        }

        #endregion

        #region Navigation

        class CustomRequestHandler : CefSharp.Handler.RequestHandler
        {
            private readonly Action<string> _anchorLinkClickHandler;

            private static readonly Regex _anchorLinkRegex = new(@"https:\/\/darkHtmlViewer_cef\/(?<link>.+)", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

            public CustomRequestHandler(Action<string> anchorLinkClickHandler)
            {
                _anchorLinkClickHandler = anchorLinkClickHandler;
            }

            protected override bool OnBeforeBrowse(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool userGesture, bool isRedirect)
            {
                var urlContainsAnchorLink = TryGetClickedAnchorLink(request.Url, out var clickedAnchorLink);

                if (!urlContainsAnchorLink)
                {
                    return base.OnBeforeBrowse(chromiumWebBrowser, browser, frame, request, userGesture, isRedirect);
                }

                _anchorLinkClickHandler.Invoke(clickedAnchorLink);

                return true;
            }

            private bool TryGetClickedAnchorLink(string url, out string link)
            {
                var match = _anchorLinkRegex.Match(url);

                if (!match.Success)
                {
                    link = null;
                    return false;
                }

                link = match.Groups["link"].Value;
                return true;
            }
        }

        private void TriggerLinkClicked(string link)
        {
            Dispatcher.Invoke(() => LinkClickedCommand?.TryExecute(link));
        }

        #endregion

        #region Scroll to

        private string _scrollToNext = null;

        /// <summary>
        /// Tries to scroll to an element by ID
        /// </summary>
        [RelayCommand]
        public void Scroll(string elementId)
        {
            var script = $"document.getElementById(\"{elementId}\").scrollIntoView();";
            cefBrowser.ExecuteScriptAsync(script);
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

        #region Search

        private string _textToFind = null;

        /// <summary>
        /// Finds text in the loaded HTML
        /// </summary>
        [RelayCommand]
        public void Search(string text)
        {
            var clean = CleanSearchText(text);

            if (string.IsNullOrEmpty(clean))
            {
                return;
            }

            cefBrowser.Find(text, true, false, false);
        }

        private static string CleanSearchText(string text)
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
        public void Print()
        {
            //TODO check out print alternatives (PrintToPdfAsync)
            cefBrowser.Print();
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
            cefBrowser.ZoomLevel = zoom;
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
    }
}
