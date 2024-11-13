using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using System.Collections.Generic;

namespace CompMs.MsdialLcMsApi.Export
{
    public class LcmsAnalysisMetadataAccessor : BaseAnalysisMetadataAccessor
    {
        public LcmsAnalysisMetadataAccessor(IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer, ParameterBase parameter, ExportspectraType type) : base(refer, parameter, type) {

        }

        public LcmsAnalysisMetadataAccessor(IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer, ParameterBase parameter) : base(refer, parameter, parameter.ExportSpectraType) {

        }

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
            IReadOnlyList<RawSpectrum> spectrumList,
            AnalysisFileBean analysisFile,
            ExportStyle exportStyle) {

            var content = base.GetContentCore(feature, msdec, reference, matchResult, spectrumList, analysisFile, exportStyle);
            content["RT left(min)"] = string.Format("{0:F3}", feature.ChromXsLeft.RT.Value);
            content["RT (min)"] = string.Format("{0:F3}", feature.ChromXsTop.RT.Value);
            content["RT right (min)"] = string.Format("{0:F3}", feature.ChromXsRight.RT.Value);
            content["Precursor m/z"] = string.Format("{0:F5}", feature.PrecursorMz);
            content["Reference RT"] = ValueOrNull(reference?.ChromXs.RT.Value, "F3");
            content["Reference m/z"] = ValueOrNull(reference?.PrecursorMz, "F5");
            content["RT matched"] = (matchResult?.IsRtMatch ?? false).ToString();
            content["m/z matched"] = (matchResult?.IsPrecursorMzMatch ?? false).ToString();
            content["RT similarity"] = ValueOrNull(matchResult?.RtSimilarity, "F2");
            content["m/z similarity"] = ValueOrNull(matchResult?.AcurateMassSimilarity, "F2");

            return content;
        }
    }
}
