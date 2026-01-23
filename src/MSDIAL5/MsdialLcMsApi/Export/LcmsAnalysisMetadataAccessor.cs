using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.Raw.Abstractions;
using System.Collections.Generic;

namespace CompMs.MsdialLcMsApi.Export
{
    public class LcmsAnalysisMetadataAccessor : BaseAnalysisMetadataAccessor
    {
        public LcmsAnalysisMetadataAccessor(IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer, ParameterBase parameter, ExportspectraType type, Common.Parameter.MsRefSearchParameterBase? searchParameter = null) : base(refer, parameter, type) {
            _searchParameter = searchParameter;
        }

        public LcmsAnalysisMetadataAccessor(IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer, ParameterBase parameter) : base(refer, parameter, parameter.ExportSpectraType) {

        }

        private readonly Common.Parameter.MsRefSearchParameterBase? _searchParameter;

        protected override string[] GetHeadersCore() {
            return new string[] {
                "Peak ID",
                "Name",
                "Scan",
                "RT left(min)",
                "RT (min)",
                "RT right (min)",
                "Precursor m/z",
                "Height",
                "Area",
                "Model masses",
                "Adduct",
                "Isotope",
                "Comment",
                "Reference RT",
                "Reference m/z",
                "Formula",
                "Ontology",
                "InChIKey",
                "SMILES",
                "Annotation tag (VS1.0)",
                "RT matched",
                "m/z matched",
                "MS/MS matched",
                "RT similarity",
                "m/z similarity",
                "Simple dot product",
                "Weighted dot product",
                "Reverse dot product",
                "Enhanced dot product",
                "Spectrum entropy",
                "Matched peaks count",
                "Matched peaks percentage",
                "Total score",
                "S/N",
                "MS1 isotopes",
                "MSMS spectrum" };
        }

        protected override Dictionary<string, string> GetContentCore(
            ChromatogramPeakFeature feature,
            MSDecResult msdec,
            MoleculeMsReference reference,
            MsScanMatchResult matchResult,
            AnalysisFileBean analysisFile,
            ExportStyle exportStyle,
            IDataProvider provider) {

            var content = base.GetContentCore(feature, msdec, reference, matchResult, analysisFile, exportStyle, provider);
            content["RT left(min)"] = string.Format("{0:F3}", feature.PeakFeature.ChromXsLeft.RT.Value);
            content["RT (min)"] = string.Format("{0:F3}", feature.PeakFeature.ChromXsTop.RT.Value);
            content["RT right (min)"] = string.Format("{0:F3}", feature.PeakFeature.ChromXsRight.RT.Value);
            content["Precursor m/z"] = string.Format("{0:F5}", feature.PrecursorMz);
            content["Reference RT"] = ValueOrNull(reference?.ChromXs.RT.Value, "F3");
            content["Reference m/z"] = ValueOrNull(reference?.PrecursorMz, "F5");
            content["RT matched"] = (matchResult?.IsRtMatch ?? false).ToString();
            content["m/z matched"] = (matchResult?.IsPrecursorMzMatch ?? false).ToString();
            content["RT similarity"] = ValueOrNull(matchResult?.RtSimilarity, "F2");
            content["m/z similarity"] = ValueOrNull(matchResult?.AcurateMassSimilarity, "F2");

            var searchparameter = _searchParameter;
            double? weighteddot = reference is not null ? System.Math.Sqrt(Common.Algorithm.Scoring.MsScanMatching.GetWeightedDotProduct(msdec, reference, searchparameter.Ms2Tolerance, searchparameter.MassRangeBegin, searchparameter.MassRangeEnd)) : null;
            double? reversedot = reference is not null ? System.Math.Sqrt(Common.Algorithm.Scoring.MsScanMatching.GetReverseDotProduct(msdec, reference, searchparameter.Ms2Tolerance, searchparameter.MassRangeBegin, searchparameter.MassRangeEnd)) : null;
            double? enhanceddot = reference is not null ? System.Math.Sqrt(Common.Algorithm.Scoring.MsScanMatching.GetEnhancedDotProduct(msdec, reference, searchparameter.Ms2Tolerance, searchparameter.MassRangeBegin, searchparameter.MassRangeEnd, .6d)) : null;
            double? entropy = reference is not null ? Common.Algorithm.Scoring.MsScanMatching.GetSpectralEntropySimilarity(msdec.Spectrum, reference?.Spectrum, searchparameter.Ms2Tolerance) : null;
            content["Weighted dot product"] = ValueOrNull(weighteddot/*matchResult?.WeightedDotProduct*/, "F3");
            content["Enhanced dot product"] = ValueOrNull(enhanceddot/*matchResult?.EnhancedDotProduct*/, "F3");
            content["Spectrum entropy"] = ValueOrNull(entropy/*matchResult?.SpectralEntropy*/, "F3");

            return content;
        }
    }
}
