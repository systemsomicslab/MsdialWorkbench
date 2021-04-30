using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Extension;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.MsdialCore.Export
{
    public interface IMetadataFormatter
    {
        string[] GetHeaders();
        ReadOnlyDictionary<string, string> GetContent(AlignmentSpotProperty spot, MSDecResult msdec);
    }

    public abstract class BaseMetadataFormatter : IMetadataFormatter {

        public BaseMetadataFormatter(IMatchResultRefer refer, ParameterBase parameter) {
            this.refer = refer;
            this.parameter = parameter;
        }

        private readonly IMatchResultRefer refer;
        private readonly ParameterBase parameter;

        public string[] GetHeaders() => GetHeadersCore();

        public ReadOnlyDictionary<string, string> GetContent(AlignmentSpotProperty spot, MSDecResult msdec) {
            var matchResult = spot.MatchResults.Representative;
            var reference = refer.Refer(matchResult);
            return new ReadOnlyDictionary<string, string>(GetContentCore(spot, msdec, reference, matchResult));
        }

        protected virtual string[] GetHeadersCore() {
            return new string[] {
                "Alignment ID" ,
                // PeakSpotHeader,
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
                "Annotation tag (VS1.0)" ,
                // AnnotationMatchInfoHeader,
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

        protected virtual Dictionary<string, string> GetContentCore(
            AlignmentSpotProperty spot,
            MSDecResult msdec,
            MoleculeMsReference reference,
            MsScanMatchResult matchResult){

            return new Dictionary<string, string>
            {
                { "Alignment ID" ,spot.MasterAlignmentID.ToString() },
                { "Metabolite name", spot.Name },
                { "Adduct type", spot?.AdductType.AdductIonName ?? "null" },
                { "Post curation result", GetPostCurationResult(spot) },
                { "Fill %", spot.FillParcentage.ToString("F2") },
                { "MS/MS assigned", spot.IsMsmsAssigned.ToString() },
                { "Formula", ValueOrNull(reference?.Formula.FormulaString) },
                { "Ontology", !string.IsNullOrEmpty(reference?.CompoundClass)  ? reference.CompoundClass
                                                                               : ValueOrNull(reference?.Ontology) },
                { "INCHIKEY", ValueOrNull(reference?.InChIKey) },
                { "SMILES", ValueOrNull(reference?.SMILES) },
                { "Annotation tag (VS1.0)" , DataAccess.GetAnnotationCode(matchResult, parameter).ToString() },
                { "Comment", spot.Comment },
                { "Manually modified for quantification", spot.IsManuallyModifiedForQuant.ToString() },
                { "Manually modified for annotation", spot.IsManuallyModifiedForAnnotation.ToString() },
                { "Isotope tracking parent ID", spot.PeakCharacter.IsotopeParentPeakID.ToString() },
                { "Isotope tracking weight number", spot.PeakCharacter.IsotopeWeightNumber.ToString() },
                { "m/z similarity", ValueOrNull(matchResult.AcurateMassSimilarity, "F2") },
                { "Simple dot product", ValueOrNull(matchResult.SimpleDotProduct, "F2") },
                { "Weighted dot product", ValueOrNull(matchResult.WeightedDotProduct, "F2") },
                { "Reverse dot product", ValueOrNull(matchResult.ReverseDotProduct, "F2") },
                { "Matched peaks count", ValueOrNull(matchResult.MatchedPeaksCount, "F2") },
                { "Matched peaks percentage", ValueOrNull(matchResult.MatchedPeaksPercentage, "F2") },
                { "Total score", ValueOrNull(matchResult.TotalScore, "F2") },
                { "S/N average", spot.SignalToNoiseAve.ToString("F2") },
                { "Spectrum reference file name", spot.AlignedPeakProperties[spot.RepresentativeFileID].FileName },
                { "MS1 isotopic spectrum", GetIsotopesListContent(spot) },
                { "MS/MS spectrum", GetSpectrumListContent(msdec) },
            };
        }

        protected string GetIsotopesListContent(AlignmentSpotProperty feature) {
            var isotopes = feature.IsotopicPeaks;
            if (isotopes.IsEmptyOrNull()) {
                return "null";
            }
            return string.Join(";", isotopes.Select(isotope => string.Format("{0:F5} {1:F0}", isotope.Mass, isotope.AbsoluteAbundance)));
        }

        protected string GetSpectrumListContent(MSDecResult msdec) {
            var spectrum = msdec.Spectrum;
            if (spectrum.IsEmptyOrNull()) {
                return "null";
            }
            return string.Join(";", spectrum.Select(peak => string.Format("{0:F5} {1:F0}", peak.Mass, peak.Intensity)));
        }

        protected static string GetPostCurationResult(AlignmentSpotProperty spot) {
            return "null"; // write something
        }

        protected static string ValueOrNull(string value) => string.IsNullOrEmpty(value) ? "null" : value;
        protected static string ValueOrNull(float value, string format) => value != 0 ? value.ToString(format) : "null"; 
        protected static string ValueOrNull(double value, string format) => value != 0 ? value.ToString(format) : "null"; 
    }

}
