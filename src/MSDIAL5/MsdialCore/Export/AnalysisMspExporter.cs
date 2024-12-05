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
    public sealed class AnalysisMspExporter : IAnalysisExporter<ChromatogramPeakFeatureCollection>
    {
        private readonly IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> _refer;
        private readonly ParameterBase _parameter;
        private readonly Func<AnalysisFileBean, IMsScanPropertyLoader<ChromatogramPeakFeature>> _loaderFactory;

        public AnalysisMspExporter(IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer, ParameterBase parameter) {
            _refer = refer ?? throw new ArgumentNullException(nameof(refer));
            _parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
            _loaderFactory = file => new MSDecLoader(file.DeconvolutionFilePath, file.DeconvolutionFilePathList);
        }

        public AnalysisMspExporter(IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer, ParameterBase parameter, Func<AnalysisFileBean, IMsScanPropertyLoader<ChromatogramPeakFeature>> loaderFuctory) {
            _refer = refer ?? throw new ArgumentNullException(nameof(refer));
            _parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
            _loaderFactory = loaderFuctory ?? throw new ArgumentNullException(nameof(loaderFuctory));
        }

        void IAnalysisExporter<ChromatogramPeakFeatureCollection>.Export(Stream stream, AnalysisFileBean analysisFile, ChromatogramPeakFeatureCollection peakFeatureCollection, ExportStyle exportStyle) {
            var loader = _loaderFactory(analysisFile);
            foreach (var peak in peakFeatureCollection.Items) {
                SpectraExport.SaveSpectraTableAsNistFormat(stream, peak, loader.Load(peak).Spectrum, _refer, _parameter);
            }
        }
    }
}
