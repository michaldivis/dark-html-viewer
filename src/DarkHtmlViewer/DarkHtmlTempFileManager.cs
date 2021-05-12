using System;
using System.IO;

namespace DarkHtmlViewer
{
    internal class DarkHtmlTempFileManager
    {
        private Guid _instanceId;
        private readonly string _tempFileDir;
        private string _tempFilePath;
        private int _count = 1;

        public DarkHtmlTempFileManager(string tmpFileDirPath)
        {
            _instanceId = Guid.NewGuid();

            _tempFileDir = tmpFileDirPath;
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
    }
}
