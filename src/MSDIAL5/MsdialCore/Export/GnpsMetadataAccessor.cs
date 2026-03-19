using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System.Collections.Generic;

namespace CompMs.MsdialCore.Export;

public class GnpsMetadataAccessor : BaseMetadataAccessor
{
    public GnpsMetadataAccessor(IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?>? refer, ParameterBase parameter, bool trimSpectrumToExcelLimit = false) : base(refer, parameter, trimSpectrumToExcelLimit) {

    }

    protected override string[] GetHeadersCore() {
        return [
            "Alignment ID" ,
            // PeakSpotHeader,
            "Average Rt(min)",
            "Average Mz",
            "Metabolite name",
            "Adduct type",
            "Post curation result",
            "Fill %",
            "MS/MS assigned",
            // ReferenceHeader,
            "Formula",
            "Ontology",
            "INCHIKEY",
            "SMILES",
            // AnnotationMatchInfoHeader,
            "Comment",
            "Isotope tracking parent ID",
            "Isotope tracking weight number",
            "Weighted dot product",
            "Reverse dot product",
            "Matched peaks percentage",
            "S/N average",
            "Spectrum reference file name",
            "MS1 isotopic spectrum",
            "MS/MS spectrum",
        ];
    }

    protected override Dictionary<string, string> GetContentCore(AlignmentSpotProperty spot, IMSScanProperty msdec, MoleculeMsReference reference, MsScanMatchResult matchResult) {
        var result = base.GetContentCore(spot, msdec, reference, matchResult);
        result.Add("Average Rt(min)", ValueOrNull(spot?.TimesCenter?.RT.Value, "F3"));
        result.Add("Average Mz", spot.MassCenter.ToString("F5"));
        return result;
    }
}
