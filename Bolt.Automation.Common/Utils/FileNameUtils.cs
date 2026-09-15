namespace Bolt.Automation.Common.Utils
{
    public static class FileNameUtils
    {
        public static string SanitizeFileName(string fileName, int maxLength = 100)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
            return sanitized.Length > maxLength ? sanitized.Substring(0, maxLength) : sanitized;
        }
    }
}

