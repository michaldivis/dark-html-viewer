using System;
using System.IO;
using System.Text.RegularExpressions;

namespace DarkHtmlViewer
{
    internal class DarkHtmlTempFileManager
    {
        private readonly Guid _instanceId;
        private readonly string _tempFileDir;
        private string _tempFilePath;
        private int _count = 0;

        public DarkHtmlTempFileManager(Guid instanceId)
        {
            _instanceId = instanceId;

            _tempFileDir = GetTempFileDirPath();
            _tempFilePath = Path.Combine(_tempFileDir, $"{_instanceId}_tmp_{_count}.html");

            Create("<p></p>");
        }

        public void Create(string html)
        {
            Directory.CreateDirectory(_tempFileDir);

            DeleteTempFile();

            _count++;
            _tempFilePath = Path.Combine(_tempFileDir, $"{_instanceId}_tmp_{_count}.html");

            File.WriteAllText(_tempFilePath, html);
        }

        public void DeleteTempFile()
        {
            File.Delete(_tempFilePath);
        }

        public string GetFilePath()
        {
            return _tempFilePath;
        }

        private string GetTempFileDirPath()
        {
            return Path.Combine(Path.GetTempPath(), "tmp_html");
        }

        public bool IsTempFilePath(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            var pattern = _instanceId + @"_tmp_\d+\.html";
            var match = Regex.Match(text, pattern);
            return match.Success;
        }
    }
}
