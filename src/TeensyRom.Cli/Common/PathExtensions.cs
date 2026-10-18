using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace TeensyRom.Cli.Common
{
    public static class PathExtensions
    {
        public static string EnsureUnixPathEnding(this string path) => path.EndsWith("/") ? path : path + "/";
        public static string ToUnixPath(this string path) => path.Replace("\\", "/");

        public static string GetUnixParentPath(this string path)
        {
            if (string.IsNullOrEmpty(path) || path == "/") return "/";

            if (!path.StartsWith("/")) path = "/" + path;

            if (path.EndsWith("/")) path = path.TrimEnd('/');

            var segments = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length <= 1) return "/";

            var parentPath = string.Join("/", segments.Take(segments.Length - 1));

            return ("/" + parentPath).EnsureUnixPathEnding();
        }

        public static string GetUnixFileExtension(this string path)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;

            int lastDotIndex = path.LastIndexOf('.');

            if (lastDotIndex <= 0)
                return string.Empty;

            return path.Substring(lastDotIndex).ToLower();
        }

        public static string GetFileNameFromPath(this string path)
        {
            if (string.IsNullOrEmpty(path)) return string.Empty;

            var segments = path.Split('/');
            return segments.Length > 0 ? segments[^1] : string.Empty;
        }

        public static bool IsValidUnixPath(this string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }
            // Regular expression explanation:
            // ^\/                : Ensures the path starts with a '/'
            // [^\\:*?"<>|]+      : Ensures that invalid characters are not present
            // (\/[^\\:*?"<>|]+)* : Matches directories or filenames separated by '/' (no consecutive slashes)
            string pattern = @"^\/([^\\:*?""<>|]+(\/[^\\:*?""<>|]+)*)?$";

            return Regex.IsMatch(path, pattern);
        }

        public static string GetOsFriendlyPath(this string path) 
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return path.ToUnixPath();
            }
            return path;
        }
    }
}