using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompMs.MsdialCore.Export
{
    public sealed class AlignmentMgfExporter : IAlignmentSpectraExporter
    {
        public void Export(Stream stream, IReadOnlyList<AlignmentSpotProperty> spots, IReadOnlyList<MSDecResult> msdecResults) {
            foreach (var (spot, result) in spots.Zip(msdecResults, (s, r) => (s, r))) {
                SpectraExport.SaveSpectraTableAsMgfFormat(stream, spot, result.Spectrum);
            }
        }
    }
}
