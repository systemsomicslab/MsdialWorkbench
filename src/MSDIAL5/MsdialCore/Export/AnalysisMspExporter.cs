using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using System.IO;

namespace CompMs.MsdialCore.Export
{
    public sealed class AnalysisMspExporter : IAnalysisExporter
    {
        private readonly IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> _refer;
        private readonly ParameterBase _parameter;

        public AnalysisMspExporter(IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer, ParameterBase parameter) {
            _refer = refer;
            _parameter = parameter;
        }

        void IAnalysisExporter.Export(Stream stream, AnalysisFileBean analysisFile, ChromatogramPeakFeatureCollection peakFeatureCollection) {
            var loader = new MSDecLoader(analysisFile.DeconvolutionFilePath);
            foreach (var peak in peakFeatureCollection.Items) {
                SpectraExport.SaveSpectraTableAsNistFormat(stream, peak, loader.LoadMSDecResult(peak.MasterPeakID).Spectrum, _refer, _parameter);
            }
        }
    }
}
