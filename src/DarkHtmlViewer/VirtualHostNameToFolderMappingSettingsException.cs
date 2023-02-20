using System;
using System.Collections.Generic;
using System.Linq;

namespace DarkHtmlViewer;

public class VirtualHostNameToFolderMappingSettingsException : Exception
{
    public Dictionary<string, string> Errors { get; }

    public VirtualHostNameToFolderMappingSettingsException(Dictionary<string, string> errors) : base(string.Join("\r\n", errors.Select(x => $"{x.Key}: '{x.Value}'")))
    {
        Errors = errors;
    }
}
