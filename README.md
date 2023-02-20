# Dark HTML Viewer

A WPF user control for displaying in memory HTML. The control stores the loaded HTML in temporary files and uses the [WebView2](https://docs.microsoft.com/en-us/microsoft-edge/webview2/) control to display them.

## Nuget
[![Nuget](https://img.shields.io/nuget/v/divis.darkhtmlviewer?label=Divis.DarkHtmlViewer)](https://www.nuget.org/packages/Divis.DarkHtmlViewer/)

## Usage

### Basics
Add the namespace to your XAML
```XML
xmlns:dhv="clr-namespace:DarkHtmlViewer;assembly=DarkHtmlViewer"
```

And use the `HtmlViewer` control
```XML
<dhv:HtmlViewer x:Name="htmlViewer" />
```

### Commands & methods
`LoadCommand` => `void Load(string html)`\
`ScrollCommand` => `Task ScrollAsync(string elementId)`\
`ScrollOnNextLoadCommand` => `void ScrollOnNextLoad(string elementId)`\
`SearchCommand` => `Task SearchAsync(string text)`\
`SearchOnNextLoadCommand` => `void SearchOnNextLoad(string text)`\
`SaveScrollPositionForNextLoadCommand` => `SaveScrollPositionForNextLoadAsync()`\
`PrintCommand` => `Task PrintAsync()`\
`ZoomCommand` => `void Zoom(double zoom)`

### Loading HTML content
To load content into the viewer, bind an HTML string to it's `HtmlContent` property
```XML
<dhv:HtmlViewer x:Name="htmlViewer" HtmlContent="{Binding MyHtmlString}" />
```
or use the `LoadCommand` and pass the HTML string as  the `CommandParameter`
```XML
<Button
    Command="{Binding ElementName=htmlViewer, Path=LoadCommand}"
    CommandParameter="{Binding MyHtmlString}"
    Content="Load HTML using a command" />
```

### Link clicks
Whenever a link is clicked in the loaded HTML file, the control fires the `LinkClickedCommand`. Bind you own command to that in order to handle link clicks, example:

View.cs
```XML
<dhv:HtmlViewer
    x:Name="htmlViewer"
    LinkClickedCommand="{Binding MyLinkClickedCommand}" />
```

ViewModel.cs
```Csharp
public ICommand MyLinkClickedCommand => new RelayCommand<string>(HandleLinkClick);

private void HandleLinkClick(string? link)
{
    Debug.WriteLine($"Link clicked: {link}");
}
```

### Scroll to
To scroll to a specific element id, you have several options.

`ScrollCommand`: tries to scroll to a specific element in the currently loaded HTML file
```XML
<Button
    Command="{Binding ElementName=htmlViewer, Path=ScrollCommand}"
    CommandParameter="elementId"
    Content="Scroll to elementId" />
```

`ScrollOnNextLoadCommand`: will try to scroll to a specific element in the next loaded HTML file
```XML
<Button
    Command="{Binding ElementName=htmlViewer, Path=ScrollOnNextLoadCommand}"
    CommandParameter="elementId"
    Content="Scroll to elementId on next load" />
```

### Save scroll position
Saves the current scroll position and tries to restore it next time HTML content is loaded. If `ScrollOnNextLoad` is used as well, this will be ignored

`SaveScrollPositionForNextLoadCommand`: will try to scroll to a specific element in the next loaded HTML file
```XML
<Button
    Command="{Binding ElementName=htmlViewer, Path=SaveScrollPositionForNextLoadCommand}"
    Content="Save scroll position for next load" />
```

### Search

`SearchCommand`: finds a search term on the current page
```XML
<Button
    Command="{Binding ElementName=htmlViewer, Path=SearchCommand}"
    CommandParameter="search text"
    Content="Search for text" />
```

`SearchOnNextLoadCommand`: finds a search term in the next loaded HTML file
```XML
<Button
    Command="{Binding ElementName=htmlViewer, Path=SearchOnNextLoadCommand}"
    CommandParameter="search text"
    Content="Search for text on next load" />
```

### Printing

The `PrintCommand` can be used to bring up the default print dialog window.
```XML
<Button
    Command="{Binding ElementName=htmlViewer, Path=PrintCommand}"
    Content="Show print dialog" />
```

### Logging
Enable logging for the control by configuring an `ILoggerFactory` provider like so:
```csharp
var loggerFactory = LoggerFactory.Create(c =>
{
    c.SetMinimumLevel(LogLevel.Debug);
    c.AddDebug();
});

HtmlViewer.ConfigureLogger(() => loggerFactory);
```

### Virtual host name to folder path mapping
See [this page](https://docs.microsoft.com/en-us/dotnet/api/microsoft.web.webview2.core.corewebview2.setvirtualhostnametofoldermapping?view=webview2-dotnet-1.0.1108.44) to learn more.

Enable virtual host name to folder path mapping like so:
```csharp
HtmlViewer.ConfigureVirtualHostNameToFolderMappingSettings(new VirtualHostNameToFolderMappingSettings
{
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

### Default browser background color

Configure the default background color of the control like so:

```csharp
using System.Drawing;

var backgroundColor = Color.FromArgb(255, 24, 24, 24);
HtmlViewer.ConfigureDefaultBackgroundColor(backgroundColor);
```