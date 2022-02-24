using System;

namespace DarkHtmlViewer
{
    public class VirtualHostNameToFolderMappingSettingsException : Exception
    {
        public string SettingName { get; }

        public VirtualHostNameToFolderMappingSettingsException(string settingName, string message) : base($"{message}. Setting name: \"{settingName}\"")
        {
            SettingName = settingName;
        }
    }
}
