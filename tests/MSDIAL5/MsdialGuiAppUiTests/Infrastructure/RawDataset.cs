using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompMs.App.Msdial.UiTests.Infrastructure
{
    public enum Polarity { Positive, Negative }

    /// <summary>
    /// A set of raw files from the demo data, with the settings that process them sensibly.
    ///
    /// The settings are not about getting the chemistry right; they are what it takes for the
    /// workflow to run through. A minimum peak height above every peak in the data, for one,
    /// leaves the first file empty and MS-DIAL stops with "No peak information".
    /// </summary>
    public sealed class RawDataset
    {
        public RawDataset(string name, string folder, Polarity polarity, double minimumPeakHeight, params string[] files) {
            Name = name;
            Folder = folder;
            Polarity = polarity;
            MinimumPeakHeight = minimumPeakHeight;
            Files = files;
        }

        /// <summary>The test case name, which is what a failure report shows.</summary>
        public string Name { get; }

        /// <summary>The folder under the demo data directory that holds the files.</summary>
        public string Folder { get; }

        public Polarity Polarity { get; }

        public double MinimumPeakHeight { get; }

        /// <summary>
        /// The measurement files as a user picks them: a file, or for the vendors that store a
        /// run as a folder (Agilent and Bruker .d, Waters .raw), that folder.
        /// </summary>
        public IReadOnlyList<string> Files { get; }

        /// <summary>
        /// Runs stored as folders cannot be picked in a file dialog; a user drags them in instead.
        /// </summary>
        public bool NeedsDragAndDrop(string demoDataDir) {
            return Files.Any(file => Directory.Exists(Path.Combine(demoDataDir, Folder, file)));
        }

        /// <summary>
        /// Copies the files into <paramref name="destination"/> together with the companions a
        /// vendor keeps beside them under the same name (a .wiff needs its .wiff.scan).
        /// </summary>
        public void CopyTo(string demoDataDir, string destination) {
            var source = Path.Combine(demoDataDir, Folder);
            foreach (var file in Files) {
                var stem = file + ".";
                var baseName = Path.GetFileNameWithoutExtension(file) + ".";
                foreach (var entry in Directory.EnumerateFileSystemEntries(source)) {
                    var name = Path.GetFileName(entry);
                    if (name != file && !name.StartsWith(stem, StringComparison.OrdinalIgnoreCase) && !IsCompanion(name, baseName, file)) {
                        continue;
                    }
                    var target = Path.Combine(destination, name);
                    if (Directory.Exists(entry)) {
                        CopyDirectory(entry, target);
                    }
                    else {
                        File.Copy(entry, target, overwrite: true);
                    }
                }
            }
        }

        // Sciex keeps "x.wiff.scan" and "x.timeseries.data" next to "x.wiff" or "x.wiff2": same
        // base name, other extension. Results MS-DIAL wrote earlier into the demo folder share the
        // base name too, but carry a timestamp ("x_202409051643.pai2"), so they never match "x.".
        private static bool IsCompanion(string name, string baseName, string file) {
            var extension = Path.GetExtension(file);
            return name.StartsWith(baseName, StringComparison.OrdinalIgnoreCase)
                && (extension.Equals(".wiff", StringComparison.OrdinalIgnoreCase) || extension.Equals(".wiff2", StringComparison.OrdinalIgnoreCase));
        }

        private static void CopyDirectory(string source, string destination) {
            Directory.CreateDirectory(destination);
            foreach (var file in Directory.EnumerateFiles(source)) {
                File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), overwrite: true);
            }
            foreach (var directory in Directory.EnumerateDirectories(source)) {
                CopyDirectory(directory, Path.Combine(destination, Path.GetFileName(directory)));
            }
        }

        public override string ToString() => Name;
    }
}
