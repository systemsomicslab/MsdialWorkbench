using CompMs.App.Msdial.Model.DataObj;
using CompMs.MsdialLcMsApi.Algorithm.Alignment;

namespace CompMs.App.Msdial.Model.Export
{
    /// <summary>
    /// The note an alignment export carries about its RT axis: set when the alignment was made with
    /// the automatic RT correction and its map was saved with it (see <see cref="AlignmentRtAxis"/>).
    /// </summary>
    internal static class AlignmentRtAxisNote
    {
        public static string? For(AlignmentFileBeanModel alignmentFile) {
            return AlignmentRtAxis.Load(alignmentFile.AlignmentFileBean, alignmentFile.AnalysisFiles, correctionRequested: false).ExportNote();
        }
    }
}
