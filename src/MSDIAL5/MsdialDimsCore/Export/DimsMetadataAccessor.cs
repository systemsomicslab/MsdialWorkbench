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

namespace CompMs.MsdialDimsCore.Export
{
    public class DimsMetadataAccessor : BaseMetadataAccessor
    {
        public DimsMetadataAccessor(IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer, ParameterBase parameter, bool trimSpectrumToExcelLimit) : base(refer, parameter, trimSpectrumToExcelLimit) {

        }

        protected override string[] GetHeadersCore() {
            return new string[] {
                "Alignment ID" ,
                "Average Mz",
                "Metabolite name",
                "Adduct type",
                "Post curation result",
                "Fill %",
                "MS/MS assigned",
                "Reference m/z",
                "Formula",
                "Ontology",
                "INCHIKEY",
                "SMILES",
                "Annotation tag (VS1.0)" ,
                "m/z matched",
                "MS/MS matched",
                "Comment",
                "Manually modified for quantification",
                "Manually modified for annotation" ,
                "Isotope tracking parent ID",
                "Isotope tracking weight number",
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
            content.Add("Reference m/z", reference?.PrecursorMz.ToString("F5") ?? "null");
            content.Add("m/z matched", (matchResult?.IsPrecursorMzMatch ?? false).ToString());
            content.Add("MS/MS matched", (matchResult?.IsSpectrumMatch ?? false).ToString());
            return content;
        }
    }

    public class DimsAnalysisMetadataAccessor : BaseAnalysisMetadataAccessor
    {
        public DimsAnalysisMetadataAccessor(IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer, ParameterBase parameter) : base(refer, parameter, parameter.ExportSpectraType) {

        }

        public DimsAnalysisMetadataAccessor(IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer, ParameterBase parameter, ExportspectraType type) : base(refer, parameter, type) {

        }

        protected override string[] GetHeadersCore() {
            return new string[] {
                "Peak ID",
                "Name",
                "Scan",
                "m/z left",
                "m/z",
                "m/z right",
                "Height",
                "Area",
                "Model masses",
                "Adduct",
                "Isotope",
                "Comment",
                "Reference m/z",
                "Formula",
                "Ontology",
                "InChIKey",
                "SMILES",
                "Annotation tag (VS1.0)",
                "m/z matched",
                "MS/MS matched",
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
            AnalysisFileBean analysisFile, ExportStyle exportStyle) {

            var content = base.GetContentCore(feature, msdec, reference, matchResult, spectrumList, analysisFile, exportStyle);
            content["m/z left"] = string.Format("{0:F5}", feature.ChromXsLeft.Mz.Value);
            content["m/z"] = string.Format("{0:F5}", feature.PrecursorMz);
            content["m/z right"] = string.Format("{0:F5}", feature.ChromXsRight.Mz.Value);
            content["m/z matched"] = (matchResult?.IsPrecursorMzMatch ?? false).ToString();
            content["m/z similarity"] = ValueOrNull(matchResult?.AcurateMassSimilarity, "F2");
            content["Reference m/z"] = ValueOrNull(reference?.PrecursorMz, "F5");

            return content;
        }

    }
}
