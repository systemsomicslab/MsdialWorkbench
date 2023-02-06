using System;
using System.IO;

namespace CompMs.Common.Extension
{
    public static class PathExtension
    {
        public static string GetRelativePath(string relativeTo, string path) {
            if (string.IsNullOrEmpty(path)) {
                return string.Empty;
            }

            var uri = new Uri(relativeTo);
            var relativeUri = uri.MakeRelativeUri(new Uri(path));
            var relativePath = Uri.UnescapeDataString(relativeUri.ToString());
            return relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        }
    }
}
