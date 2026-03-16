using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using System.IO;

namespace CompMs.MsdialCore.Export;

public sealed class AlignmentSdfExporter : IAlignmentSpectraExporter
{
    void IAlignmentSpectraExporter.Export(Stream stream, AlignmentSpotProperty spot, MSDecResult msdecResult)
    {
        SpectraExport.SaveSpectraTableAsSdfFormat(
            stream,
            spot,
            msdecResult.Spectrum
        );
    }
}
