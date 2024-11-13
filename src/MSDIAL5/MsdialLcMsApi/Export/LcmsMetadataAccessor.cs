using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
using CompMs.Common.Proteomics.DataObj;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.Parameter;
using System.Collections.Generic;

namespace CompMs.MsdialLcMsApi.Export
{
    public class LcmsProteomicsMetadataAccessor : ProteomicsBaseAccessor {
        public LcmsProteomicsMetadataAccessor(IMatchResultRefer<PeptideMsReference?, MsScanMatchResult?> refer, bool trimSpectrumToExcelLimit) : base(refer, trimSpectrumToExcelLimit) {

        }
    }


    public class LcmsMetadataAccessor : BaseMetadataAccessor
    {
        public LcmsMetadataAccessor(IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer, ParameterBase parameter, bool trimSpectrumToExcelLimit) : base(refer, parameter, trimSpectrumToExcelLimit) {

        }

        protected override string[] GetHeadersCore() {
            return new string[] {
                "Alignment ID" ,
                "Average Rt(min)",
                "Average Mz",
                "Metabolite name",
                "Adduct type",
                "Post curation result",
                "Fill %",
                "MS/MS assigned",
                "Reference RT",
                "Reference m/z",
                "Formula",
                "Ontology",
                "INCHIKEY",
                "SMILES",
                "Annotation tag (VS1.0)" ,
                "RT matched",
                "m/z matched",
                "MS/MS matched",
                "Comment",
                "Manually modified for quantification",
                "Manually modified for annotation" ,
                "Isotope tracking parent ID",
                "Isotope tracking weight number",
                "RT similarity",
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
            content.Add("Average Rt(min)", ValueOrNull(spot?.TimesCenter?.RT.Value, "F3"));
            content.Add("Average Mz", spot.MassCenter.ToString("F5"));
            content.Add("Reference RT", reference?.ChromXs.RT.Value.ToString("F3") ?? "null");
            content.Add("Reference m/z", reference?.PrecursorMz.ToString("F5") ?? "null");
            content.Add("RT matched", (matchResult?.IsRtMatch ?? false).ToString());
            content.Add("m/z matched", (matchResult?.IsPrecursorMzMatch ?? false).ToString());
            content.Add("MS/MS matched", (matchResult?.IsSpectrumMatch ?? false).ToString());
            content.Add("RT similarity", ValueOrNull(matchResult?.RtSimilarity, "F2"));
            return content;
        }
    }
}
