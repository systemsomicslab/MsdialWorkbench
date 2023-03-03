using CompMs.Common.DataObj.Result;
using CompMs.Common.Extension;
using CompMs.Common.Interfaces;
using CompMs.Common.Proteomics.DataObj;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.MsdialCore.Export {
    public abstract class ProteomicsBaseAccessor : IMetadataAccessor {
        public ProteomicsBaseAccessor(IMatchResultRefer<PeptideMsReference, MsScanMatchResult> refer, bool trimSpectrumToExcelLimit = false) {
            this.refer = refer;
            _trimSpectrumToExcelLimit = trimSpectrumToExcelLimit;
        }

        private readonly IMatchResultRefer<PeptideMsReference, MsScanMatchResult> refer;
        private readonly bool _trimSpectrumToExcelLimit;

        public string[] GetHeaders() => GetHeadersCore();

        IReadOnlyDictionary<string, string> IMetadataAccessor.GetContent(AlignmentSpotProperty spot, IMSScanProperty msdec) {
            var matchResult = spot.MatchResults.Representative;
            var reference = refer?.Refer(matchResult);
            return GetContentCore(spot, msdec, reference, matchResult);
        }

        protected virtual string[] GetHeadersCore() {
            return new string[] {
                "Alignment ID",
                // PeakSpotHeader,
                "Protein group ID",
                "Protein",
                "Peptide name",
                "Adduct type",
                "Fill %",
                "MS/MS assigned",

                // AnnotationMatchInfoHeader,
                "Comment",
                "Manually modified for quantification",
                "Manually modified for annotation" ,
                "Isotope tracking parent ID",
                "Isotope tracking weight number",
                "m/z similarity",
                "Andromeda score",
                "S/N average",
                "Spectrum reference file name",
                "MS1 isotopic spectrum",
                "MS/MS spectrum",
            };
        }

        protected virtual Dictionary<string, string> GetContentCore(
            AlignmentSpotProperty spot,
            IMSScanProperty msdec,
            PeptideMsReference reference,
            MsScanMatchResult matchResult) {

            return new Dictionary<string, string>
            {
                { "Alignment ID" ,spot.MasterAlignmentID.ToString() },
                { "Protein group ID", spot.ProteinGroupID.ToString() },
                { "Protein", spot.Protein },
                { "Peptide name", UnknownIfEmpty(spot.Name) },
                { "Adduct type", spot?.AdductType.AdductIonName ?? "null" },
                { "Fill %", spot.FillParcentage.ToString("F2") },
                { "MS/MS assigned", spot.IsMsmsAssigned.ToString() },
                { "Comment", spot.Comment },
                { "Manually modified for quantification", spot.IsManuallyModifiedForQuant.ToString() },
                { "Manually modified for annotation", spot.IsManuallyModifiedForAnnotation.ToString() },
                { "Isotope tracking parent ID", spot.PeakCharacter.IsotopeParentPeakID.ToString() },
                { "Isotope tracking weight number", spot.PeakCharacter.IsotopeWeightNumber.ToString() },
                { "m/z similarity", ValueOrNull(matchResult.AcurateMassSimilarity, "F2") },
                { "Andromeda score", ValueOrNull(matchResult.MatchedPeaksPercentage, "F2") },
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
        protected static string ValueOrNull(double value, string format) => Math.Abs(value) > eps ? value.ToString(format) : "null";
        protected static string ValueOrNull(double? value, string format) => value != null ? value.Value.ToString(format) : "null";
    }
}
