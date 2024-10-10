using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using System.Collections.Generic;

namespace CompMs.MsdialImmsCore.Export
{
    public class ImmsMetadataAccessor : BaseMetadataAccessor
    {
        public ImmsMetadataAccessor(IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer, ParameterBase parameter, bool trimSpectrumToExcelLimit) : base(refer, parameter, trimSpectrumToExcelLimit) {

        }

        protected override string[] GetHeadersCore() {
            return new string[] {
                "Alignment ID" ,
                "Average Mz",
                "Average mobility",
                "Average CCS",
                "Metabolite name",
                "Adduct type",
                "Post curation result",
                "Fill %",
                "MS/MS assigned",
                "Reference CCS",
                "Reference m/z",
                "Formula",
                "Ontology",
                "INCHIKEY",
                "SMILES",
                "Annotation tag (VS1.0)" ,
                "CCS matched",
                "m/z matched",
                "MS/MS matched",
                "Comment",
                "Manually modified for quantification",
                "Manually modified for annotation" ,
                "Isotope tracking parent ID",
                "Isotope tracking weight number",
                "CCS similarity",
                "m/z similarity",
                "Simple dot product",
                "Weighted dot product",
                "Reverse dot product",
                "Matched peaks count",
                "Matched peaks percentage",
                "Total score",
                "S/N average",
                "Spectrum reference file name",
                "MS1 isotopic spectrum",
                "MS/MS spectrum",
            };
        }

        protected override Dictionary<string, string> GetContentCore(
            AlignmentSpotProperty spot,
            IMSScanProperty msdec,
            MoleculeMsReference reference,
            MsScanMatchResult matchResult) {

            var content = base.GetContentCore(spot, msdec, reference, matchResult);
            content.Add("Average Mz", spot.MassCenter.ToString("F5"));
            content.Add("Average mobility", spot.TimesCenter.Drift.Value.ToString("F3"));
            content.Add("Average CCS",  spot.CollisionCrossSection <= 0 ?
                "null" : spot.CollisionCrossSection.ToString("F3"));
            content.Add("Reference CCS", ValueOrNull(reference?.CollisionCrossSection.ToString("F3")));
            content.Add("Reference m/z", ValueOrNull(reference?.PrecursorMz.ToString("F5")));
            content.Add("CCS matched", (matchResult?.IsCcsMatch ?? false).ToString());
            content.Add("m/z matched", (matchResult?.IsPrecursorMzMatch ?? false).ToString());
            content.Add("MS/MS matched", (matchResult?.IsSpectrumMatch ?? false).ToString());
            content.Add("CCS similarity", matchResult.IsUnknown ? "null" : matchResult.CcsSimilarity.ToString("F2"));
            return content;
        }
    }

    public class ImmsAnalysisMetadataAccessor : BaseAnalysisMetadataAccessor
    {
        public ImmsAnalysisMetadataAccessor(IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer, ParameterBase parameter, ExportspectraType type) : base(refer, parameter, type) {

        }

        public ImmsAnalysisMetadataAccessor(IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer, ParameterBase parameter) : base(refer, parameter, parameter.ExportSpectraType) {

        }

        protected override string[] GetHeadersCore() {
            return new string[] {
                "Peak ID",
                "Name",
                "Scan",
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
                "Reference CCS",
                "Reference m/z",
                "Formula",
                "Ontology",
                "InChIKey",
                "SMILES",
                "Annotation tag (VS1.0)",
                "CCS matched",
                "m/z matched",
                "MS/MS matched",
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

        protected override Dictionary<string, string> GetContentCore(
            ChromatogramPeakFeature feature,
            MSDecResult msdec,
            MoleculeMsReference reference,
            MsScanMatchResult matchResult,
            IReadOnlyList<RawSpectrum> spectrumList,
            AnalysisFileBean analysisFile,
            ExportStyle exportStyle) {

            var content = base.GetContentCore(feature, msdec, reference, matchResult, spectrumList, analysisFile, exportStyle);
            content["Mobility left"] = string.Format("{0:F3}", feature.ChromXsLeft.Drift.Value);
            content["Mobility"] = string.Format("{0:F3}", feature.ChromXs.Drift.Value);
            content["Mobility right"] = string.Format("{0:F3}", feature.ChromXsRight.Drift.Value);
            content["CCS"] = NullIfNegative(feature.CollisionCrossSection, "F3");
            content["Precursor m/z"] = string.Format("{0:F5}", feature.PrecursorMz);
            content["Reference CCS"] = ValueOrNull(reference?.CollisionCrossSection, "F3");
            content["Reference m/z"] = ValueOrNull(reference?.PrecursorMz, "F5");
            content["CCS matched"] = (matchResult?.IsCcsMatch ?? false).ToString();
            content["m/z matched"] = (matchResult?.IsPrecursorMzMatch ?? false).ToString();
            content["CCS similarity"] = ValueOrNull(matchResult?.CcsSimilarity, "F2");
            content["m/z similarity"] = ValueOrNull(matchResult?.AcurateMassSimilarity, "F2");

            return content;
        }
    }
}
