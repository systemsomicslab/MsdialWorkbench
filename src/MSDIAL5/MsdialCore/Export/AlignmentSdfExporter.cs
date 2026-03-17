using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using System;
using System.IO;

namespace CompMs.MsdialCore.Export;

public sealed class AlignmentSdfExporter : IAlignmentSpectraExporter
{
    private readonly ParameterBase _parameter;
    private bool _exportNoStructurePeak;
    public AlignmentSdfExporter(bool exportNoStructurePeak, ParameterBase parameter)
    {
        _exportNoStructurePeak = exportNoStructurePeak;
        _parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
    }
    public void Export(Stream stream, AlignmentSpotProperty spot, MSDecResult msdecResult)
    { 
        Export(stream, spot, msdecResult, _exportNoStructurePeak, _parameter);
    }
    public void Export(Stream stream, AlignmentSpotProperty spot, MSDecResult msdecResult, bool exportNoStructurePeak, ParameterBase parameter)
    {
        SpectraExport.SaveSpectraTableAsSdfFormat(stream, spot, msdecResult.Spectrum, exportNoStructurePeak, parameter);
    }
}
