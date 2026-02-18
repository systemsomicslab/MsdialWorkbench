using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using System.IO;

namespace CompMs.MsdialCore.Export;

public sealed class AlignmentSdfExporter : IAlignmentSpectraExporter
{
    private readonly bool _exportNoMs2Molecule;
    private readonly bool _set2dCoordinates;
    public AlignmentSdfExporter(bool exportNoMs2Molecule, bool set2dCoordinates)
    {
        _exportNoMs2Molecule = exportNoMs2Molecule;
        _set2dCoordinates = set2dCoordinates;
    }
    public AlignmentSdfExporter() : this(exportNoMs2Molecule: true, set2dCoordinates: true) { }

    void IAlignmentSpectraExporter.Export(Stream stream, AlignmentSpotProperty spot, MSDecResult msdecResult)
    {
        SpectraExport.SaveSpectraTableAsSdfFormat(
            stream,
            spot,
            msdecResult.Spectrum,
            _exportNoMs2Molecule,
            _set2dCoordinates
        );
    }
}
