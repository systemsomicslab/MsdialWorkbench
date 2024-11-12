using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using System;
using System.Collections.Generic;

namespace CompMs.MsdialCore.Export
{
    public sealed class ChromatogramShapeMetadataAccessorDecorator : IAnalysisMetadataAccessor {
        private static readonly string[] AdditionalContents = new[] {
            "Estimated noise",
            "S/N",
            "Sharpness",
            "Gaussian similarity",
            "Ideal slope",
            "Symmetry"
        };
        private readonly IAnalysisMetadataAccessor _accessor;

        public ChromatogramShapeMetadataAccessorDecorator(IAnalysisMetadataAccessor accessor)
        {
            _accessor = accessor;
        }

        public Dictionary<string, string> GetContent(ChromatogramPeakFeature feature, MSDecResult msdec, IDataProvider provider, AnalysisFileBean analysisFile, ExportStyle exportStyle) {
            var contents = _accessor.GetContent(feature, msdec, provider, analysisFile, exportStyle);
            var peakshape = feature.PeakShape;
            contents[AdditionalContents[0]] = peakshape.EstimatedNoise.ToString("F3");
            contents[AdditionalContents[1]] = peakshape.SignalToNoise.ToString("F5");
            contents[AdditionalContents[2]] = peakshape.ShapenessValue.ToString("F3");
            contents[AdditionalContents[3]] = peakshape.GaussianSimilarityValue.ToString("F3");
            contents[AdditionalContents[4]] = peakshape.IdealSlopeValue.ToString("F3");
            contents[AdditionalContents[5]] = peakshape.SymmetryValue.ToString("F3");
            return contents;
        }

        public string[] GetHeaders() {
            var header = _accessor.GetHeaders();
            var idx = Array.IndexOf(header, "Model masses");
            if (idx == -1) {
                idx = header.Length;
            }
            var result = new string[header.Length + AdditionalContents.Length];
            Array.Copy(header, 0, result, 0, idx);
            Array.Copy(AdditionalContents, 0, result, idx, AdditionalContents.Length);
            Array.Copy(header, idx, result, idx + AdditionalContents.Length, header.Length - idx);
            return result;
        }
    }
}
