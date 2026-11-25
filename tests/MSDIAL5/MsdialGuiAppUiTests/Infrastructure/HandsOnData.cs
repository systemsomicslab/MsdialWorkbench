using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;

namespace CompMs.App.Msdial.UiTests.Infrastructure
{
    /// <summary>
    /// The data of the MS-DIAL metabolomics hands-on (metabolomics_hands-on.zip) under the run
    /// parameter <see cref="RunParameters.HandsOnDataDir"/>. The files are found in their folders
    /// rather than named here.
    /// </summary>
    internal static class HandsOnData
    {
        /// <summary>Part 1, natural product profiling: SCIEX TripleTOF, positive, six wine extracts.</summary>
        public static RawDataset Part1Positive(string handsOnDir) => Measurements(handsOnDir, "Hands-on data1 pos", @"data1\pos", Polarity.Positive);

        /// <summary>The MassBank library part 1 annotates with.</summary>
        public static string Part1Library(string handsOnDir) => Single(handsOnDir, "data1", "*.msp");

        /// <summary>
        /// Part 2, untargeted lipidomics: SCIEX, negative, mouse plasma of SPF and germ-free mice
        /// (three each) and a blank.
        /// </summary>
        public static RawDataset Part2Negative(string handsOnDir) => Measurements(handsOnDir, "Hands-on data2 neg", @"data2\neg", Polarity.Negative);

        /// <summary>The m/z-RT text library of the EquiSPLASH internal standards, negative mode.</summary>
        public static string Part2NegativeTextLibrary(string handsOnDir) => Single(handsOnDir, "data2", "*_Neg_*.txt");

        private static RawDataset Measurements(string handsOnDir, string name, string folder, Polarity polarity) {
            var files = Directory.EnumerateFiles(Path.Combine(handsOnDir, folder), "*.wiff")
                .Select(Path.GetFileName)
                .OrderBy(file => file)
                .ToArray();
            if (files.Length == 0) {
                Assert.Fail($"No .wiff files in '{Path.Combine(handsOnDir, folder)}'. Is the hands-on data unpacked there?");
            }
            return new RawDataset(name, folder, polarity, 1000, files);
        }

        private static string Single(string handsOnDir, string folder, string pattern) {
            var matches = Directory.GetFiles(Path.Combine(handsOnDir, folder), pattern);
            if (matches.Length != 1) {
                Assert.Fail($"Expected one {pattern} in '{Path.Combine(handsOnDir, folder)}', found {matches.Length}.");
            }
            return matches[0];
        }
    }
}
