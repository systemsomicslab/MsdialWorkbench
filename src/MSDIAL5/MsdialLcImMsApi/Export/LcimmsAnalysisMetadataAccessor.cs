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

namespace CompMs.MsdialLcImMsApi.Export
{
    /// <summary>
    /// The LcimmsAnalysisMetadataAccessor class is responsible for gathering and summarizing necessary metadata from surrounding objects when outputting peak information.
    /// </summary>
    public sealed class LcimmsAnalysisMetadataAccessor : BaseAnalysisMetadataAccessor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LcimmsAnalysisMetadataAccessor"/> class.
        /// </summary>
        /// <param name="refer">An object that provides references for MoleculeMsReference and MsScanMatchResult.</param>
        /// <param name="parameter">Parameters for the analysis.</param>
        /// <param name="type">The type of export spectra.</param>
        public LcimmsAnalysisMetadataAccessor(IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer, ParameterBase parameter, ExportspectraType type) : base(refer, parameter, type) {

        }

        /// <summary>
        /// Gets the headers for the metadata.
        /// </summary>
        /// <returns>An array of metadata headers.</returns>
        protected override string[] GetHeadersCore() {
            return new string[] {
                "Peak ID",
                "Name",
                "Scan",
                "RT left(min)",
                "RT (min)",
                "RT right (min)",
                "Mobility left",
                "Mobility",
                "Mobility right",
                "CCS",
                "Precursor m/z",
                "Height",
                "Area",
                "Model masses",
                "Adduct",
                "Isotope",
                "Comment",
                "Reference RT",
                "Reference CCS",
                "Reference m/z",
                "Formula",
                "Ontology",
                "InChIKey",
                "SMILES",
                "Annotation tag (VS1.0)",
                "RT matched",
                "CCS matched",
                "m/z matched",
                "MS/MS matched",
                "RT similarity",
                "CCS similarity",
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

        /// <summary>
        /// Gets the content for the metadata.
        /// </summary>
        /// <param name="feature">The chromatogram peak feature.</param>
        /// <param name="msdec">The MSDec result.</param>
        /// <param name="reference">The molecule MS reference.</param>
        /// <param name="matchResult">The match result.</param>
        /// <param name="spectrumList">The list of raw spectra.</param>
        /// <param name="analysisFile">The analysis file bean.</param>
        /// <returns>A dictionary containing the metadata content.</returns>
        /// <param name="exportStyle"></param>
        protected override Dictionary<string, string> GetContentCore(
            ChromatogramPeakFeature feature,
            MSDecResult msdec,
            MoleculeMsReference reference,
            MsScanMatchResult matchResult,
            IReadOnlyList<RawSpectrum> spectrumList,
            AnalysisFileBean analysisFile,
            ExportStyle exportStyle) {

            var content = base.GetContentCore(feature, msdec, reference, matchResult, spectrumList, analysisFile, exportStyle);
            content["RT left(min)"] = string.Format("{0:F3}", feature.PeakFeature.ChromXsLeft.RT.Value);
            content["RT (min)"] = string.Format("{0:F3}", feature.PeakFeature.ChromXsTop.RT.Value);
            content["RT right (min)"] = string.Format("{0:F3}", feature.PeakFeature.ChromXsRight.RT.Value);
            content["Mobility left"] = string.Format("{0:F3}", feature.PeakFeature.ChromXsLeft.Drift.Value);
            content["Mobility"] = string.Format("{0:F3}", feature.PeakFeature.ChromXsTop.Drift.Value);
            content["Mobility right"] = string.Format("{0:F3}", feature.PeakFeature.ChromXsRight.Drift.Value);
            content["CCS"] = string.Format("{0:F3}", feature.CollisionCrossSection);
            content["Precursor m/z"] = string.Format("{0:F5}", feature.PrecursorMz);
            content["Reference RT"] = ValueOrNull(reference?.ChromXs.RT.Value, "F3");
            content["Reference CCS"] = ValueOrNull(reference?.CollisionCrossSection, "F3");
            content["Reference m/z"] = ValueOrNull(reference?.PrecursorMz, "F5");
            content["RT matched"] = (matchResult?.IsRtMatch ?? false).ToString();
            content["CCS matched"] = (matchResult?.IsCcsMatch ?? false).ToString();
            content["m/z matched"] = (matchResult?.IsPrecursorMzMatch ?? false).ToString();
            content["RT similarity"] = ValueOrNull(matchResult?.RtSimilarity, "F2");
            content["CCS similarity"] = ValueOrNull(matchResult?.CcsSimilarity, "F2");
            content["m/z similarity"] = ValueOrNull(matchResult?.AcurateMassSimilarity, "F2");

            return content;
        }
    }
}
