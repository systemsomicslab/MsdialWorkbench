using System.IO;

namespace CompMs.App.Msdial.Utility.Tests;

internal static class TestFiles
{
    /// <summary>
    /// Whether any handle on the file is open, in this process or another: exclusive access fails while one is.
    /// A missing file still throws.
    /// </summary>
    public static bool IsOpen(string path) {
        try {
            using (File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
            return false;
        }
        catch (IOException) when (File.Exists(path)) {
            return true;
        }
    }
}
