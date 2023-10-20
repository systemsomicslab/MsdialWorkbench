using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using System.Collections.Generic;
using System.IO;

namespace CompMs.MsdialCore.Export
{
    public interface IAlignmentSpectraExporter
    {
        void Export(Stream stream, AlignmentSpotProperty spot, MSDecResult msdecResult);
        void Export(Stream stream, IReadOnlyList<AlignmentSpotProperty> spots, IReadOnlyList<MSDecResult> msdecResults);
    }
}
