using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using System;
using System.IO;

namespace CompMs.MsdialCore.Export
{
    public sealed class AnalysisMgfExporter : IAnalysisExporter<ChromatogramPeakFeatureCollection>
    {
        private readonly Func<AnalysisFileBean, IMsScanPropertyLoader<ChromatogramPeakFeature>> _loaderFactory;

        public AnalysisMgfExporter(Func<AnalysisFileBean, IMsScanPropertyLoader<ChromatogramPeakFeature>> loaderFuctory) {
            _loaderFactory = loaderFuctory ?? throw new ArgumentNullException(nameof(loaderFuctory));
        }

        void IAnalysisExporter<ChromatogramPeakFeatureCollection>.Export(Stream stream, AnalysisFileBean analysisFile, ChromatogramPeakFeatureCollection peakFeatureCollection, ExportStyle exportStyle) {
            var loader = _loaderFactory(analysisFile);
            foreach (var peak in peakFeatureCollection.Items) {
                SpectraExport.SaveSpectraTableAsMgfFormat(stream, peak, loader.Load(peak).Spectrum);
            }
        }
    }
}
