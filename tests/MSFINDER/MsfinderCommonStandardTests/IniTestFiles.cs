using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace Riken.Metabolomics.MsfinderCommon.Tests
{
    /// <summary>
    /// Paths of the committed settings files, and byte comparison against a golden.
    /// </summary>
    internal static class IniTestFiles
    {
        public static string Resource(params string[] parts) {
            var path = Path.Combine(new[] { AppDomain.CurrentDomain.BaseDirectory, "Resources", "Ini" }.Concat(parts).ToArray());
            Assert.IsTrue(File.Exists(path), $"Committed test file is missing from the output directory: {path}");
            return path;
        }

        public static string Input(string name) => Resource("Input", name);

        public static string Golden(string name) => Resource("Golden", name);

        /// <summary>A new file name in the temporary directory, for one settings file a test writes.</summary>
        public static string NewTempFile(string suffix = ".ini") {
            var dir = Path.Combine(Path.GetTempPath(), "MsfinderCommonStandardTests");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, Guid.NewGuid().ToString("N") + suffix);
        }

        /// <summary>A settings file in the temporary directory holding exactly these lines.</summary>
        public static string NewTempIni(params string[] lines) {
            var path = NewTempFile();
            File.WriteAllText(path, string.Concat(lines.Select(line => line + "\r\n")));
            return path;
        }

        /// <summary>
        /// Asserts that the file has exactly the expected bytes. On a mismatch the file is kept and named in
        /// the message, with the first differing byte, so a reviewer can inspect it.
        /// </summary>
        public static void AssertBytes(byte[] expected, string actualPath, string what) {
            var actual = File.ReadAllBytes(actualPath);
            var n = Math.Min(expected.Length, actual.Length);
            var first = -1;
            for (var i = 0; i < n && first < 0; i++) {
                if (expected[i] != actual[i]) first = i;
            }
            if (first < 0 && expected.Length == actual.Length) {
                File.Delete(actualPath);
                return;
            }
            Assert.Fail($"{what}: bytes differ at offset {(first < 0 ? n : first)} (expected {expected.Length} bytes, actual {actual.Length}); actual output kept at {actualPath}");
        }
    }
}
