using Microsoft.Extensions.Logging;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DarkHtmlViewer;

internal class TempFileManager
{
    private readonly Guid _instanceId;
    private readonly ILogger<TempFileManager> _logger;

    private const string _emptyFileText = "<p></p>";

    private readonly string _tempFileDir;

    private readonly List<string> _files = new();

    public TempFileManager(Guid instanceId, ILogger<TempFileManager> logger)
    {
        _instanceId = instanceId;
        _logger = logger;

        _tempFileDir = GetTempFileDirPath();

        Create(_emptyFileText);
    }

    /// <summary>
    /// Created a temporary HTML file
    /// </summary>
    /// <param name="html">HTML content</param>
    /// <returns>HTML file path</returns>
    public string? Create(string? html = null)
    {
        try
        {
            Directory.CreateDirectory(_tempFileDir);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Creating temp files directory failed, path: {TempFileDir}", _tempFileDir);
            return null;
        }

        TryDeletePreviousFiles();

        var filePath = GenerateTempFilePath();

        var success = TryCreateFile(filePath, html ?? string.Empty);

        if(!success)
        {
            return null;
        }

        _files.Add(filePath);

        _logger.LogDebug("Created a file, path: {FilePath}", filePath);

        return filePath;
    }

    /// <summary>
    /// Checks whether a file path is one of the generated temp files that belong to this manager.
    /// </summary>
    /// <param name="filePath">File path to check</param>
    public bool IsTempFileName(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return false;
        }

        return _files.Any(x => x.EndsWith(filePath));
    }

    private string GenerateTempFilePath()
    {
        var tempFilePath = Path.Combine(_tempFileDir, $"{_instanceId}_tmp_{Guid.NewGuid()}.html");
        return tempFilePath;
    }

    public void TryDeletePreviousFiles()
    {
        for (int i = 0; i < _files.Count; i++)
        {
            var filePath = _files[i];

            try
            {
                File.Delete(filePath);
                _files.RemoveAt(i);
                _logger.LogDebug("Deleted a file, path: {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Deleting a file failed, path: {FilePath}", filePath);
            }
        }
    }

    private bool TryCreateFile(string filePath, string text)
    {
        try
        {
            using var file = File.Open(filePath, FileMode.CreateNew, FileAccess.Write);
            using var writer = new StreamWriter(file);
            writer.Write(text);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Writing text to file failed, path: {FilePath}", filePath);
            return false;
        }
    }

    private static string GetTempFileDirPath()
    {
        return Path.Combine(Path.GetTempPath(), "tmp_html");
    }
}