using CompMs.Common.Components;
using CompMs.Common.Parser;
using CompMs.MsdialCore.DataObj;

namespace CompareAnnotationResult
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
            throw new NotImplementedException();
        }

        public AlignmentResultContainer LoadSpots() {
            throw new NotImplementedException();
        }
    }
}