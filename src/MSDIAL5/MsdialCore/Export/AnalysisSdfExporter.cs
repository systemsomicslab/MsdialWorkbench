using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using System;
using System.IO;

namespace CompMs.MsdialCore.Export
{
    public sealed class AnalysisSdfExporter : IAnalysisExporter<ChromatogramPeakFeatureCollection>
    {
        private readonly Func<AnalysisFileBean, IMsScanPropertyLoader<ChromatogramPeakFeature>> _loaderFactory;
        private readonly ParameterBase _parameter;
        private bool _exportNoStructurePeak;
        public AnalysisSdfExporter(Func<AnalysisFileBean, IMsScanPropertyLoader<ChromatogramPeakFeature>> loaderFuctory,ParameterBase parameter) {
            _loaderFactory = loaderFuctory ?? throw new ArgumentNullException(nameof(loaderFuctory));
            _parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
        }

        void IAnalysisExporter<ChromatogramPeakFeatureCollection>.Export(Stream stream, AnalysisFileBean analysisFile, ChromatogramPeakFeatureCollection peakFeatureCollection, ExportStyle exportStyle) {
            var loader = _loaderFactory(analysisFile);
            foreach (var peak in peakFeatureCollection.Items) {
                SpectraExport.SaveSpectraTableAsSdfFormat(
                    stream, 
                    peak, 
                    loader.Load(peak).Spectrum,
                    _exportNoStructurePeak,
                    _parameter
            );
            }
        }
    }
}
