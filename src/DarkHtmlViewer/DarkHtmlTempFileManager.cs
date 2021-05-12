using System;
using System.IO;

namespace DarkHtmlViewer
{
    internal class DarkHtmlTempFileManager
    {
        private Guid _instanceId;
        private string _tempFileDir;
        private string _tempFilePath;
        private int _count = 0;

        public DarkHtmlTempFileManager()
        {
            Initialize();
        }

        private void Initialize()
        {
            _instanceId = Guid.NewGuid();

            _tempFileDir = GetTempFileDirPath();
            _tempFilePath = Path.Combine(_tempFileDir, $"{_instanceId}_tmp_{_count}.html");

            Create(null);
        }

        public void Create(string html)
        {
            Directory.CreateDirectory(_tempFileDir);

            File.Delete(_tempFilePath);

            _count++;
            _tempFilePath = Path.Combine(_tempFileDir, $"{_instanceId}_tmp_{_count}.html");

            File.WriteAllText(_tempFilePath, html);
        }

        public string GetFilePath()
        {
            return _tempFilePath;
        }

        private string GetTempFileDirPath()
        {
            return Path.Combine(Path.GetTempPath(), "tmp_html");
        }
    }
}
