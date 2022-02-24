using Microsoft.Web.WebView2.Core;

namespace DarkHtmlViewer
{
    public class VirtualHostNameToFolderMappingSettings
    {
        public bool IsEnabled { get; init; }
        public string Hostname { get; init; }
        public string FolderPath { get; init; }
        public CoreWebView2HostResourceAccessKind AccessKind { get; init; }
    }
}
