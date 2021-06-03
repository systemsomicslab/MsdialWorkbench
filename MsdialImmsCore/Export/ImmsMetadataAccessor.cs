using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.MsdialImmsCore.Export
{
    public class ImmsMetadataAccessor : BaseMetadataAccessor
    {
        public ImmsMetadataAccessor(IMatchResultRefer refer, ParameterBase parameter) : base(refer, parameter) {

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
            MSDecResult msdec,
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
            content.Add("CCS similarity", ValueOrNull(matchResult?.CcsSimilarity, "F2"));
            return content;
        }
    }
}
