using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
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
        public DimsMetadataAccessor(IMatchResultRefer refer, ParameterBase parameter) : base(refer, parameter) {

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
            MSDecResult msdec,
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
}
