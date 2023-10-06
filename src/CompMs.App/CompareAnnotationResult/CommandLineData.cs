using CompMs.Common.Components;
using CompMs.Common.Parser;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;

namespace CompMs.App.CompareAnnotationResult
{
    internal sealed class CommandLineData {
        [LongStyleArgument("--input")]
        public string? AlignmentResultPath { get; set; }
        [LongStyleArgument("--output")]
        public string? OutputPath { get; set; }
        [LongStyleArgument("--library")]
        public string? LibraryPath { get; set; }
        [LongStyleArgument("--mz-tolerance")]
        public double MzTolerance { get; set; }
        [LongStyleArgument("--rt-tolerance")]
        public double RtTolerance { get; set; }
        [LongStyleArgument("--amplitude-threshold")]
        public double AmplitudeThreshold { get; set; }

        public List<MoleculeMsReference> GetLibrary() {
            if (!File.Exists(LibraryPath)) {
                throw new Exception("Library path is not entered.");
            }

            string libraryPath = LibraryPath!;
            switch (Path.GetExtension(libraryPath)) {
                case ".txt":
                    var textdb = TextLibraryParser.TextLibraryReader(libraryPath, out string error);
                    if (string.IsNullOrEmpty(error)) {
                        return textdb;
                    }
                    else {
                        throw new Exception(error);
                    }
                case ".msp":
                    var mspdb = LibraryHandler.ReadMspLibrary(libraryPath) ?? throw new Exception("Loading msp file failed.");
                    return mspdb;
                default:
                    throw new Exception( "Unsupported library type.");
            }
        }

        public AlignmentResultContainer LoadSpots() {
            if (!File.Exists(AlignmentResultPath)) {
                throw new Exception("AlignmentResultFile is not found");
            }
            if (!AlignmentResultPath.EndsWith(".arf2")) {
                throw new Exception("Unknown alignment result format.");
            }
            return AlignmentResultContainer.LoadLazy(new AlignmentFileBean() { FilePath = AlignmentResultPath.TrimEnd('2'), });
        }

        public Stream GetOutputStream() {
            var output = OutputPath;
            var file = AlignmentResultPath!;
            if (string.IsNullOrEmpty(output)) {
                output = Directory.GetParent(file)?.FullName;
            }
            if (string.IsNullOrEmpty(output)) {
                throw new Exception("OutputPath is required.");
            }
            if (!Directory.Exists(output)) {
                Directory.CreateDirectory(output);
            }
            return File.Open(Path.Combine(output, Path.ChangeExtension(Path.GetFileName(file), ".xml")), FileMode.Create);
        }
    }
}