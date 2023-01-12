using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using System.Collections.Generic;
using System.IO;

namespace CompMs.MsdialCore.Export
{
    public sealed class AlignmentMgfExporter : IAlignmentSpectraExporter
    {
        public void Export(Stream stream, IReadOnlyList<AlignmentSpotProperty> spots, IReadOnlyList<MSDecResult> msdecResults) {
            foreach (var spot in spots) {
                SpectraExport.SaveSpectraTableAsMgfFormat(stream, spot, msdecResults[spot.MasterAlignmentID].Spectrum);
            }
        }
    }
}
