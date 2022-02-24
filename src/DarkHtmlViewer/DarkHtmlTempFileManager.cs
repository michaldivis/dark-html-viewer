using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace DarkHtmlViewer
{
    internal class DarkHtmlTempFileManager
    {
        private readonly Guid _instanceId;
        private readonly ILogger<DarkHtmlTempFileManager> _logger;

        private const string EmptyFileText = "<p></p>";

        private readonly Regex _isTempFilePathRegex;
        private readonly string _tempFileDir;

        private string _tempFilePath;
        private int _count = 0;

        public DarkHtmlTempFileManager(Guid instanceId, ILogger<DarkHtmlTempFileManager> logger)
        {
            _instanceId = instanceId;
            _logger = logger;

            _tempFileDir = GetTempFileDirPath();
            _tempFilePath = CreateNewTempFilePath();

            _isTempFilePathRegex = new Regex(Regex.Escape(_instanceId.ToString()) + @"_tmp_\d+\.html");

            Create(EmptyFileText);
        }

        public void Create(string html)
        {
            try
            {
                Directory.CreateDirectory(_tempFileDir);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Creating temp files directory failed, path: {TempFileDir}", _tempFileDir);
                return;
            }

            TryDeleteCurrentTempFile();

            _tempFilePath = CreateNewTempFilePath();

            TryWriteFileText(_tempFilePath, html);
        }

        private string CreateNewTempFilePath()
        {
            string tempFilePath;

            do
            {
                tempFilePath = Path.Combine(_tempFileDir, $"{_instanceId}_tmp_{_count}.html");
                _count++;
            } while (File.Exists(tempFilePath));

            return tempFilePath;
        }

        public void TryDeleteCurrentTempFile()
        {
            try 
	        {
                File.Delete(_tempFilePath);
            }
	        catch (Exception ex)
	        {
                _logger.LogError(ex, "Deleting a temp HTML file failed");
	        }   
        }

        public void TryWriteFileText(string filePath, string text)
        {
            try
            {
                File.WriteAllText(filePath, text);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Writing text to file failed");
            }
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

            var isMatch = _isTempFilePathRegex.IsMatch(text);

            return isMatch;
        }
    }
}
