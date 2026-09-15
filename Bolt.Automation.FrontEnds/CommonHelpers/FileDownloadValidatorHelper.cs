using System.Text;
using Bolt.Automation.Common.Exceptions;
using Bolt.Automation.Common.Logging.Core;
using Microsoft.Playwright;

namespace Bolt.Automation.FrontEnds.CommonHelpers
{
    public class FileDownloadResult
    {
        public string FileName { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public List<string> Columns { get; set; } = new List<string>();
        public int RowCount { get; set; }
        public List<Dictionary<string, string>> Rows { get; set; } = new List<Dictionary<string, string>>();
        public bool IsEmpty => FileSizeBytes == 0 || RowCount == 0;
    }

    public class FileDownloadValidatorHelper
    {
        private readonly IPage _page;
        private readonly IAutomationLogger? _logger;

        public FileDownloadValidatorHelper(IPage page, IAutomationLogger? logger = null)
        {
            _page = page ?? throw new ArgumentNullException(nameof(page));
            _logger = logger;
        }

        /// <summary>
        /// Downloads and validates a file triggered by a button click or action.
        /// Uses Playwright's in-memory download handling - no file system access needed.
        /// </summary>
        /// <param name="downloadTriggerAction">The action that triggers the download (e.g., button click)</param>
        /// <param name="expectedFileExtension">Expected file extension (.csv, .xlsx, etc.)</param>
        /// <param name="downloadTimeoutMs">Timeout for download completion in milliseconds</param>
        /// <returns>FileDownloadResult containing file info, columns, and row data</returns>
        public async Task<FileDownloadResult> ValidateFileDownload(Func<Task> downloadTriggerAction, string? expectedFileExtension = null, int downloadTimeoutMs = 30000)
        {
            try
            {
                // Start waiting for download before triggering the action
                var downloadTask = _page.WaitForDownloadAsync(new() { Timeout = downloadTimeoutMs });
                _logger?.Info("Started waiting for download...");

                // Trigger the download action
                await downloadTriggerAction();
                _logger?.LogUiAction("TriggerDownload", "DownloadAction", "Download action executed");

                // Wait for the download to complete
                var download = await downloadTask;
                _logger?.Info($"Download completed: {download.SuggestedFilename}");

                // Read the download content into memory
                using var stream = await download.CreateReadStreamAsync();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();
                var fileSizeBytes = fileBytes.Length;

                _logger?.Info($"Downloaded file size: {fileSizeBytes} bytes");

                // Validate file is not empty
                if (fileSizeBytes == 0)
                {
                    throw new TestSetupException("Downloaded file is empty (0 bytes)");
                }

                // Validate file extension if specified
                var fileName = download.SuggestedFilename ?? "download";
                if (!string.IsNullOrEmpty(expectedFileExtension))
                {
                    var actualExtension = Path.GetExtension(fileName).ToLowerInvariant();
                    var expectedExt = expectedFileExtension.StartsWith(".") ? expectedFileExtension.ToLowerInvariant() : $".{expectedFileExtension.ToLowerInvariant()}";

                    if (actualExtension != expectedExt)
                    {
                        throw new TestSetupException($"File extension mismatch. Expected: {expectedExt}, Actual: {actualExtension}");
                    }
                }

                // Process file content based on extension
                var result = await ProcessDownloadedFile(fileName, fileBytes);

                _logger?.LogBusinessRule("FileDownloadValidation", !result.IsEmpty,
                    $"File: {fileName}, Size: {fileSizeBytes} bytes, Columns: {result.Columns.Count}, Rows: {result.RowCount}");

                if (result.IsEmpty)
                {
                    throw new TestSetupException("Downloaded file contains no data rows");
                }

                _logger?.Info($"File download validation successful - Columns: {result.Columns.Count}, Rows: {result.RowCount}");
                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogException(ex, "File download validation failed");
                throw;
            }
        }

        /// <summary>
        /// Downloads and validates a file served straight from a URL, e.g. a document link returned by an API.
        /// </summary>
        /// <remarks>
        /// Chromium turns a navigation to an attachment into a download and abandons the page load, so
        /// Playwright reports "Download is starting" on the goto. That is the success path here, not a failure.
        /// </remarks>
        public async Task<FileDownloadResult> ValidateFileDownloadFromUrl(string url, string? expectedFileExtension = null, int downloadTimeoutMs = 30000)
        {
            return await ValidateFileDownload(
                async () =>
                {
                    try
                    {
                        await _page.GotoAsync(url);
                    }
                    catch (PlaywrightException ex) when (ex.Message.Contains("Download is starting", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger?.Debug($"Navigation to [{url}] became a download, as expected");
                    }
                },
                expectedFileExtension,
                downloadTimeoutMs);
        }

        /// <summary>
        /// Processes the downloaded file content and extracts columns and rows
        /// </summary>
        private async Task<FileDownloadResult> ProcessDownloadedFile(string fileName, byte[] fileBytes)
        {
            var result = new FileDownloadResult
            {
                FileName = fileName,
                FileSizeBytes = fileBytes.Length
            };

            var extension = Path.GetExtension(fileName).ToLowerInvariant();

            switch (extension)
            {
                case ".csv":
                    await ProcessCsvFile(fileBytes, result);
                    break;
                case ".xlsx":
                case ".xls":
                    _logger?.Warning("Excel file processing not implemented yet. Treating as CSV.");
                    await ProcessCsvFile(fileBytes, result);
                    break;
                case ".txt":
                    await ProcessTextFile(fileBytes, result);
                    break;
                default:
                    _logger?.Warning($"Unknown file extension: {extension}. Attempting to process as CSV.");
                    await ProcessCsvFile(fileBytes, result);
                    break;
            }

            return result;
        }

        /// <summary>
        /// Processes CSV file content
        /// </summary>
        private async Task ProcessCsvFile(byte[] fileBytes, FileDownloadResult result)
        {
            await Task.Run(() =>
            {
                var content = Encoding.UTF8.GetString(fileBytes);
                var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                if (lines.Length == 0)
                {
                    _logger?.Warning("CSV file has no content lines");
                    return;
                }

                // First line as headers
                var headerLine = lines[0];
                result.Columns = ParseCsvLine(headerLine);
                _logger?.Debug($"Found {result.Columns.Count} columns: {string.Join(", ", result.Columns)}");

                // Process data rows
                for (int i = 1; i < lines.Length; i++)
                {
                    var dataLine = lines[i];
                    if (string.IsNullOrWhiteSpace(dataLine)) continue;

                    var values = ParseCsvLine(dataLine);
                    var rowData = new Dictionary<string, string>();

                    // Map values to columns
                    for (int j = 0; j < Math.Min(result.Columns.Count, values.Count); j++)
                    {
                        rowData[result.Columns[j]] = values[j];
                    }

                    // Add empty values for missing columns
                    for (int j = values.Count; j < result.Columns.Count; j++)
                    {
                        rowData[result.Columns[j]] = string.Empty;
                    }

                    result.Rows.Add(rowData);
                }

                result.RowCount = result.Rows.Count;
                _logger?.Debug($"Processed {result.RowCount} data rows from CSV");
            });
        }

        /// <summary>
        /// Processes plain text file content
        /// </summary>
        private async Task ProcessTextFile(byte[] fileBytes, FileDownloadResult result)
        {
            await Task.Run(() =>
            {
                var content = Encoding.UTF8.GetString(fileBytes);
                var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                // For text files, treat each line as a single column "Content"
                result.Columns = new List<string> { "Content" };

                foreach (var line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        result.Rows.Add(new Dictionary<string, string> { ["Content"] = line.Trim() });
                    }
                }

                result.RowCount = result.Rows.Count;
                _logger?.Debug($"Processed {result.RowCount} lines from text file");
            });
        }

        /// <summary>
        /// Parses a CSV line handling quoted fields and commas within quotes
        /// </summary>
        private List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        // Escaped quote
                        current.Append('"');
                        i++; // Skip next quote
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString().Trim());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            result.Add(current.ToString().Trim());
            return result;
        }
    }
}