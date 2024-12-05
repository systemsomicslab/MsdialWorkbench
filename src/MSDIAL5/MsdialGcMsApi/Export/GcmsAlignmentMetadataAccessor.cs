using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.Parameter;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialGcMsApi.Export
{
    public sealed class GcmsAlignmentMetadataAccessor : BaseMetadataAccessor
    {
        public GcmsAlignmentMetadataAccessor(IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer, ParameterBase parameter, bool trimSpectrumToExcelLimit = false) : base(refer, parameter, trimSpectrumToExcelLimit) {

        }

        protected override string[] GetHeadersCore() {
            return new[] {
                "Alignment ID" ,
                "Average Rt(min)",
                "Average RI",
                "Quant mass",
                "Metabolite name",
                "Fill %",
                "Reference RT",
                "Reference RI",
                "Formula",
                "Ontology",
                "INCHIKEY",
                "SMILES",
                "Annotation tag (VS1.0)" ,
                "RT matched",
                "RI matched",
                "EI-MS matched",
                "Comment",
                "Manually modified for quantification",
                "Manually modified for annotation" ,
                "Total score",
                "RT similarity",
                "RI similarity",
                "Total spectrum similarity",
                "Simple dot product",
                "Weighted dot product",
                "Reverse dot product",
                "Fragment presence %",
                "S/N average",
                "Spectrum reference file name",
                "EI spectrum",
            };
        }

        protected override Dictionary<string, string> GetContentCore(AlignmentSpotProperty spot, IMSScanProperty msdec, MoleculeMsReference reference, MsScanMatchResult matchResult) {
            var content = base.GetContentCore(spot, msdec, reference, matchResult);
            content.Add("Average Rt(min)", spot.TimesCenter.RT.Value.ToString("F3"));
            content.Add("Average RI", spot.TimesCenter.RI.Value.ToString("F2"));
            content.Add("Quant mass", spot.QuantMass.ToString());
            content.Add("Reference RT", ValueOrNull(reference?.ChromXs.RT.Value.ToString("F3")));
            content.Add("Reference RI", ValueOrNull(reference?.ChromXs.RI.Value.ToString("F2")));
            content.Add("RT matched", (matchResult?.IsRtMatch ?? false).ToString());
            content.Add("RI matched", (matchResult?.IsRiMatch ?? false).ToString());
            content.Add("EI-MS matched", (matchResult?.IsSpectrumMatch ?? false).ToString());
            content.Add("RT similarity", matchResult.IsUnknown ? "null" : (matchResult.RtSimilarity * 100).ToString("F1"));
            content.Add("RI similarity", matchResult.IsUnknown ? "null" : (matchResult.RiSimilarity * 100).ToString("F1"));
            content.Add("Total spectrum similarity", matchResult.IsUnknown ? "null" : Enumerable.Average(new[] { matchResult.SimpleDotProduct, matchResult.WeightedDotProduct, matchResult.ReverseDotProduct }).ToString("F1"));
            content.Add("Fragment presence %", (matchResult.MatchedPeaksPercentage * 100).ToString("F1"));
            content.Add("EI spectrum", content["MS/MS spectrum"]);
            return content;
        }
    }
}
