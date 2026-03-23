using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using System;
using System.IO;

namespace CompMs.MsdialCore.Export;

public sealed class AlignmentSdfExporter : IAlignmentSpectraExporter
{
    private readonly ParameterBase _parameter;
    private bool _exportNoMs2Peak;
    public AlignmentSdfExporter(bool exportNoMs2Peak, ParameterBase parameter)
    {
        _exportNoMs2Peak = exportNoMs2Peak;
        _parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
    }
    public void Export(Stream stream, AlignmentSpotProperty spot, MSDecResult msdecResult)
    { 
        Export(stream, spot, msdecResult, _exportNoMs2Peak, _parameter);
    }
    public void Export(Stream stream, AlignmentSpotProperty spot, MSDecResult msdecResult, bool exportNoMs2Peak, ParameterBase parameter)
    {
        SpectraExport.SaveSpectraTableAsSdfFormat(stream, spot, msdecResult.Spectrum, exportNoMs2Peak, parameter);
    }
}
