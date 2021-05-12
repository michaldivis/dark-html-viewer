using System;
using System.IO;

namespace DarkHtmlViewer
{
    internal class DarkHtmlTempFileManager
    {
        private Guid _instanceId;
        private string _tempFileDir;
        private string _tempFilePath;
        private int _count = 1;

        public DarkHtmlTempFileManager()
        {
            Initialize();
        }

        private void Initialize()
        {
            _instanceId = Guid.NewGuid();

            _tempFileDir = GetTempFileDirPath();
            _tempFilePath = Path.Combine(_tempFileDir, $"{_instanceId}_tmp_{_count}.html");
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
            //TODO replace this dir path with a path from config (ideally to appdata)
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "tmp_html");
        }
    }
}
