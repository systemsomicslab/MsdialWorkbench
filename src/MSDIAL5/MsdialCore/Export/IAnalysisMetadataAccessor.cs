using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Export
{
    public interface IAnalysisMetadataAccessor<T>
    {
        string[] GetHeaders();
        Dictionary<string, string> GetContent(T feature);
    }

    public interface IAnalysisMetadataAccessor
    {
        string[] GetHeaders();
        Dictionary<string, string> GetContent(ChromatogramPeakFeature feature, MSDecResult msdec, IDataProvider provider, AnalysisFileBean analysisFile, ExportStyle exportStyle);
    }

    public abstract class BaseAnalysisMetadataAccessor : IAnalysisMetadataAccessor
    {
        public BaseAnalysisMetadataAccessor(IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer, ParameterBase parameter, ExportspectraType type) {
            this.refer = refer;
            this.parameter = parameter;
            this.type = type;
        }

        protected readonly ParameterBase parameter;
        protected readonly IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer;
        private readonly ExportspectraType type;

        public string[] GetHeaders() => GetHeadersCore();

        public Dictionary<string, string> GetContent(ChromatogramPeakFeature feature, MSDecResult msdec, IDataProvider provider, AnalysisFileBean analysisFile, ExportStyle exportStyle) {
            var matchResult = NullIfUnknown(feature.MatchResults.Representative);
            var reference = matchResult is null ? null : refer.Refer(matchResult);
            return GetContentCore(feature, msdec, reference, matchResult, provider.LoadMs1Spectrums(), analysisFile, exportStyle);
        }

        protected virtual string[] GetHeadersCore() {
            return new string[] {
                "Peak ID",
                "Name",
                "Scan",
                // "m/z left", "m/z", "m/z right",
                "Height",
                "Area",
                "Model masses",
                "Adduct",
                "Isotope",
                "Comment",
                // "Reference m/z",
                "Formula",
                "Ontology",
                "InChIKey",
                "SMILES",
                "Annotation tag (VS1.0)",
                // "m/z matched",
                "MS/MS matched",
                // "m/z similarity",
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

        protected virtual Dictionary<string, string> GetContentCore(
            ChromatogramPeakFeature feature,
            MSDecResult msdec,
            MoleculeMsReference reference,
            MsScanMatchResult matchResult,
            IReadOnlyList<RawSpectrum> spectrumList,
            AnalysisFileBean analysisFile,
            ExportStyle exportStyle) {

            IEnumerable<string> comments = Enumerable.Empty<string>();
            if (!string.IsNullOrEmpty(feature.Comment)) {
                comments = comments.Append(feature.Comment);
            }
            if (matchResult?.AnyMatched ?? false) {
                comments = comments.Append($"Annotation method: {matchResult.AnnotatorID}");
            }
            if (feature.TagCollection.Any()) {
                comments = comments.Append($"Tag: {feature.TagCollection}");
            }
            var comment = string.Join("; ", comments);
            return new Dictionary<string, string> {
                { "Peak ID", feature.MasterPeakID.ToString() },
                { "Name", UnknownIfEmpty(feature.Name) },
                { "Scan", feature.MS1RawSpectrumIdTop.ToString() },
                // "m/z left", "m/z", "m/z right",
                { "Height", string.Format("{0:0}", feature.PeakHeightTop) },
                { "Area", string.Format("{0:0}", feature.PeakAreaAboveZero) },
                { "Model masses", string.Join(" ", msdec.ModelMasses) },
                { "Adduct",  feature.AdductType?.AdductIonName ?? "null" },
                { "Isotope",  feature.PeakCharacter.IsotopeWeightNumber.ToString() },
                { "Comment",  comment},
                { "Formula", reference?.Formula?.FormulaString ?? "null" },
                { "Ontology", !string.IsNullOrEmpty(reference?.CompoundClass)  ? reference.CompoundClass
                                                                               : ValueOrNull(reference?.Ontology) },
                { "InChIKey", reference?.InChIKey ?? "null" },
                { "SMILES", reference?.SMILES ?? "null" },
                { "Annotation tag (VS1.0)", DataAccess.GetAnnotationCode(matchResult, parameter).ToString() },
                // "m/z matched",
                { "MS/MS matched", (matchResult?.IsSpectrumMatch ?? false).ToString() },
                // { "m/z similarity", mzSimilarity },
                { "Simple dot product", ValueOrNull(matchResult?.SimpleDotProduct, "F2") },
                { "Weighted dot product", ValueOrNull(matchResult?.WeightedDotProduct, "F2") },
                { "Reverse dot product", ValueOrNull(matchResult?.ReverseDotProduct, "F2") },
                { "Matched peaks count", ValueOrNull(matchResult?.MatchedPeaksCount, "F2") },
                { "Matched peaks percentage", ValueOrNull(matchResult?.MatchedPeaksPercentage, "F2") },
                { "Total score", ValueOrNull(matchResult?.TotalScore, "F2") },
                { "S/N", string.Format("{0:0.00}", feature.PeakShape.SignalToNoise)},
                { "MS1 isotopes", GetIsotopesListContent(feature, spectrumList) },
                { "MSMS spectrum", GetSpectrumListContent(msdec, spectrumList, analysisFile, exportStyle) }
            };
        }

        private string GetIsotopesListContent(ChromatogramPeakFeature feature, IReadOnlyList<RawSpectrum> spectrumList) {
            var spectrum = spectrumList.FirstOrDefault(spec => spec.OriginalIndex == feature.MS1RawSpectrumIdTop);
            if (spectrum is null) {
                return "null";
            }
            var isotopes = DataAccess.GetFineIsotopicPeaks(spectrum, feature.PeakCharacter, feature.PrecursorMz, parameter.CentroidMs1Tolerance, parameter.PeakPickBaseParam.MaxIsotopesDetectedInMs1Spectrum);
            if (isotopes.IsEmptyOrNull()) {
                return "null";
            }
            return string.Join(" ", isotopes.Select(isotope => string.Format("{0:F5}:{1:F0}", isotope.Mass, isotope.AbsoluteAbundance)));
        }

        private string GetSpectrumListContent(MSDecResult msdec, IReadOnlyList<RawSpectrum> spectrumList, AnalysisFileBean analysisFile, ExportStyle exportStyle) {
            var spectrum = DataAccess.GetMassSpectrum(spectrumList, msdec, type, msdec.RawSpectrumID, parameter, analysisFile.AcquisitionType);
            if (spectrum.IsEmptyOrNull()) {
                return "null";
            }
            var strSpectrum = string.Join(" ", spectrum.Select(peak => string.Format("{0:F5}:{1:F0}", peak.Mass, peak.Intensity)));
            if (strSpectrum.Length < ExportConstants.EXCEL_CELL_SIZE_LIMIT || !exportStyle.TrimToExcelLimit) {
                return strSpectrum;
            }
            var builder = new System.Text.StringBuilder();
            foreach (var peak in spectrum) {
                if (builder.Length > ExportConstants.EXCEL_CELL_SIZE_LIMIT) {
                    break;
                }
                builder.Append(string.Format("{0:F5}:{1:F0} ", peak.Mass, peak.Intensity));
            }
            return builder.ToString();
        }

        protected static string UnknownIfEmpty(string value) => string.IsNullOrEmpty(value) ? "Unknown" : value;
        protected static string ValueOrNull(string value) => string.IsNullOrEmpty(value) ? "null" : value;
        protected static string ValueOrNull(double? value, string format) => value?.ToString(format) ?? "null";
        protected static string NullIfNegative(double value, string format) => value <= 0 ? "null" : value.ToString(format);
        protected static MsScanMatchResult NullIfUnknown(MsScanMatchResult result) => result.IsUnknown ? null : result;
    }
}
