# Dark HTML Viewer

A WPF user control for displaying in memory HTML. The control stores the loaded HTML in temporary files and uses the [WebView2](https://docs.microsoft.com/en-us/microsoft-edge/webview2/) control to display them.

## Nuget
[![Nuget](https://img.shields.io/nuget/v/divis.darkhtmlviewer?label=Divis.DarkHtmlViewer)](https://www.nuget.org/packages/Divis.DarkHtmlViewer/)

## Usage

### Basics
Add the namespace to your XAML
```XAML
xmlns:darkhtmlviewer="clr-namespace:DarkHtmlViewer;assembly=DarkHtmlViewer"
```

And use the `DarkHtmlViewer` control
```XAML
<darkhtmlviewer:DarkHtmlViewer x:Name="darkHtmlViewer" />
```

### Loading HTML content
To load content into the viewer, bind an HTML string to it's `HtmlContent` property
```XAML
<darkhtmlviewer:DarkHtmlViewer x:Name="darkHtmlViewer" HtmlContent="{Binding MyHtmlString}" />
```
or use the `LoadCommand` and pass the HTML string as  the `CommandParameter`
```XAML
<Button
    Command="{Binding ElementName=darkHtmlViewer, Path=LoadCommand}"
    CommandParameter="{Binding MyHtmlString}"
    Content="Load HTML using a command" />
```

### Link clicks
Whenever a link is clicked in the loaded HTML file, the control fires the `LinkClickedCommand`. Bind you own command to that in order to handle link clicks, example:

View.cs
```XAML
<darkhtmlviewer:DarkHtmlViewer
    x:Name="darkHtmlViewer"
    LinkClickedCommand="{Binding MyLinkClickedCommand}" />
```

ViewModel.cs
```Csharp
public ICommand MyLinkClickedCommand => new DarkCommand<string>(HandleLinkClick);

private void HandleLinkClick(string link)
{
    Debug.WriteLine($"Link clicked: {link}");
}
```

### Scroll to
To scroll to a specific element id, you have several options.

`ScrollCommand`: tries to scroll to a specific element in the currently loaded HTML file
```XAML
<Button
    Command="{Binding ElementName=darkHtmlViewer, Path=ScrollCommand}"
    CommandParameter="elementId"
    Content="Scroll to elementId" />
```

`ScrollOnNextLoadCommand`: will try to scroll to a specific element in the next loaded HTML file
```XAML
<Button
    Command="{Binding ElementName=darkHtmlViewer, Path=ScrollOnNextLoadCommand}"
    CommandParameter="elementId"
    Content="Scroll to elementId on next load" />
```

### Search

`SearchCommand`: finds a search term on the current page
```XAML
<Button
    Command="{Binding ElementName=darkHtmlViewer, Path=SearchCommand}"
    CommandParameter="search text"
    Content="Search for text" />
```

`SearchOnNextLoadCommand`: finds a search term in the next loaded HTML file
```XAML
<Button
    Command="{Binding ElementName=darkHtmlViewer, Path=SearchOnNextLoadCommand}"
    CommandParameter="search text"
    Content="Search for text on next load" />
```

### Printing

The `PrintCommand` can be used to bring up the default print dialog window.
```XAML
<Button
    Command="{Binding ElementName=darkHtmlViewer, Path=PrintCommand}"
    Content="Show print dialog" />
```

### Logging
Enable logging for the control by configuring an `ILoggerFactory` provider like so:
```csharp
DarkHtmlViewer.DarkHtmlViewer.ConfigureLogger(() => NullLoggerFactory.Instance);
```

### Virtual host name to folder path mapping
See [this page](https://docs.microsoft.com/en-us/dotnet/api/microsoft.web.webview2.core.corewebview2.setvirtualhostnametofoldermapping?view=webview2-dotnet-1.0.1108.44) to learn more.

Enable virtual host name to folder path mapping like so:
```csharp
DarkHtmlViewer.DarkHtmlViewer.ConfigureVirtualHostNameToFolderMappingSettings(new VirtualHostNameToFolderMappingSettings
{
    IsEnabled = true,
    Hostname = "myfiles.local",
    FolderPath = @"C:\Resources\MyFiles",
    AccessKind = Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow
});
```

You can then access your assets like this in HTML:
```html
<head>
    <link href="https://myfiles.local/bootstrap.min.css" rel="stylesheet">
</head>

<body>
    <img src="https://myfiles.local/my_image.jpg" />
</body>
```