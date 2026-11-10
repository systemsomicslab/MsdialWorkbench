using System.Collections.Generic;

namespace CompMs.App.MsdialConsole.ReleaseTests
{
    public enum IonMode { Positive, Negative }

    /// <summary>
    /// One run of the console application over files from the demo data.
    ///
    /// The method file starts from what the console's own <c>template</c> command writes for the
    /// mode and ion mode, so the built-in defaults are exercised as shipped; <see cref="Method"/>
    /// holds only what this data needs on top of them.
    /// </summary>
    public sealed class ConsoleCase
    {
        public ConsoleCase(string name, string mode, IonMode ionMode, string[] rawData) {
            Name = name;
            Mode = mode;
            IonMode = ionMode;
            RawData = rawData;
        }

        /// <summary>The test case name, which is what a failure report shows.</summary>
        public string Name { get; }

        /// <summary>The console command: lcms, gcms, dims, imms or lcimms.</summary>
        public string Mode { get; }

        public IonMode IonMode { get; }

        /// <summary>
        /// The measurement files, relative to the demo data directory, as a user puts them in the
        /// input folder: a file, or for the vendors that store a run as a folder (Agilent and
        /// Bruker .d, Waters .raw), that folder. Companions such as a .wiff.scan are listed too.
        /// </summary>
        public IReadOnlyList<string> RawData { get; }

        /// <summary>Method file entries that replace the template's, or are added to it.</summary>
        public IReadOnlyDictionary<string, string> Method { get; init; } = new Dictionary<string, string>();

        /// <summary>
        /// A retention index compound file (relative to the demo data directory). When set, the
        /// run writes an RI dictionary that maps every input file to it, as GC-MS with RI needs.
        /// </summary>
        public string? RiCompoundFile { get; init; }

        /// <summary>
        /// Why the case is expected to fail, when it is: a known limitation that is tracked
        /// elsewhere. The test then reports Inconclusive with the reason instead of running.
        /// </summary>
        public string? KnownIssue { get; init; }

        public override string ToString() => Name;
    }
}
