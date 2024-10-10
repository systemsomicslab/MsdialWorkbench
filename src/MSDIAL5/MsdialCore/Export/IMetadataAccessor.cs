using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Extension;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.MsdialCore.Export
{
    public interface IMetadataAccessor
    {
        string[] GetHeaders();
        IReadOnlyDictionary<string, string> GetContent(AlignmentSpotProperty spot, IMSScanProperty msdec);
    }

    public abstract class BaseMetadataAccessor : IMetadataAccessor {
        private readonly IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?>? _refer;
        private readonly ParameterBase _parameter;
        private readonly bool _trimSpectrumToExcelLimit;

        public BaseMetadataAccessor(IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?>? refer, ParameterBase parameter, bool trimSpectrumToExcelLimit = false) {
            _refer = refer;
            _parameter = parameter;
            _trimSpectrumToExcelLimit = trimSpectrumToExcelLimit;
        }

        public string[] GetHeaders() => GetHeadersCore();

        IReadOnlyDictionary<string, string> IMetadataAccessor.GetContent(AlignmentSpotProperty spot, IMSScanProperty msdec) {
            var matchResult = spot.MatchResults.Representative;
            var reference = _refer?.Refer(matchResult);
            return GetContentCore(spot, msdec, reference, matchResult);
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
            IMSScanProperty msdec,
            MoleculeMsReference reference,
            MsScanMatchResult matchResult){

            IEnumerable<string> comments = Enumerable.Empty<string>();
            if (!string.IsNullOrEmpty(spot.Comment)) {
                comments = comments.Append(spot.Comment);
            }
            comments = comments.Append($"Normalized unit: {spot.IonAbundanceUnit}");
            if (matchResult?.AnyMatched ?? false) {
                comments = comments.Append($"Annotation method: {matchResult.AnnotatorID}");
            }
            if (spot.TagCollection.Any()) {
                comments = comments.Append($"Tag: {spot.TagCollection}");
            }
            var comment = string.Join("; ", comments);
            return new Dictionary<string, string>
            {
                { "Alignment ID" ,spot.MasterAlignmentID.ToString() },
                { "Metabolite name", UnknownIfEmpty(spot.Name) },
                { "Adduct type", spot?.AdductType.AdductIonName ?? "null" },
                { "Post curation result", GetPostCurationResult(spot) },
                { "Fill %", spot.FillParcentage.ToString("F2") },
                { "MS/MS assigned", spot.IsMsmsAssigned.ToString() },
                { "Formula", ValueOrNull(reference?.Formula?.FormulaString) },
                { "Ontology", !string.IsNullOrEmpty(reference?.CompoundClass)  ? reference.CompoundClass
                                                                               : ValueOrNull(reference?.Ontology) },
                { "INCHIKEY", ValueOrNull(reference?.InChIKey) },
                { "SMILES", ValueOrNull(reference?.SMILES) },
                { "Annotation tag (VS1.0)" , DataAccess.GetAnnotationCode(matchResult, _parameter).ToString() },
                { "Comment", comment },
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
                { "S/N average", spot.SignalToNoiseAve.ToString("0.00") },
                { "Spectrum reference file name", ValueOrNull(spot.AlignedPeakProperties.FirstOrDefault(peak => peak.FileID == spot.RepresentativeFileID)?.FileName) },
                { "MS1 isotopic spectrum", GetIsotopesListContent(spot) },
                { "MS/MS spectrum", GetSpectrumListContent(msdec) },
            };
        }

        protected string GetIsotopesListContent(AlignmentSpotProperty feature) {
            var isotopes = feature.IsotopicPeaks;
            if (isotopes.IsEmptyOrNull()) {
                return "null";
            }
            var strSpectrum = string.Join(" ", isotopes.Select(isotope => string.Format("{0:F5}:{1:F0}", isotope.Mass, isotope.AbsoluteAbundance)));
            if (strSpectrum.Length < ExportConstants.EXCEL_CELL_SIZE_LIMIT || !_trimSpectrumToExcelLimit) {
                return strSpectrum;
            }
            var builder = new StringBuilder();
            foreach (var isotope in isotopes) {
                if (builder.Length > ExportConstants.EXCEL_CELL_SIZE_LIMIT) {
                    break;
                }
                builder.Append(string.Format("{0:F5}:{1:F0} ", isotope.Mass, isotope.AbsoluteAbundance));
            }
            return builder.ToString();
        }

        protected string GetSpectrumListContent(IMSScanProperty msdec) {
            var spectrum = msdec?.Spectrum;
            if (spectrum.IsEmptyOrNull()) {
                return "null";
            }
            var strSpectrum = string.Join(" ", spectrum.Select(peak => string.Format("{0:F5}:{1:F0}", peak.Mass, peak.Intensity)));
            if (strSpectrum.Length < ExportConstants.EXCEL_CELL_SIZE_LIMIT || !_trimSpectrumToExcelLimit) {
                return strSpectrum;
            }
            var builder = new StringBuilder();
            foreach (var peak in spectrum) {
                if (builder.Length > ExportConstants.EXCEL_CELL_SIZE_LIMIT) {
                    break;
                }
                builder.Append(string.Format("{0:F5}:{1:F0} ", peak.Mass, peak.Intensity));
            }
            return builder.ToString();
        }

        protected static string GetPostCurationResult(AlignmentSpotProperty spot) {
            return "null"; // write something
        }

        protected static string UnknownIfEmpty(string value) => string.IsNullOrEmpty(value) ? "Unknown" : value;

        private readonly static double eps = 1e-10;
        protected static string ValueOrNull(string value) => string.IsNullOrEmpty(value) ? "null" : value;
        protected static string ValueOrNull(float value, string format) => Math.Abs(value) > eps ? value.ToString(format) : "null"; 
        protected static string ValueOrNull(double value, string format) => Math.Abs(value) > eps  ? value.ToString(format) : "null"; 
        protected static string ValueOrNull(double? value, string format) => value != null ? value.Value.ToString(format) : "null"; 
    }

}
