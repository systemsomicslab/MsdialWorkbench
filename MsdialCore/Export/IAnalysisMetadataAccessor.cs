using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Result;
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
    public interface IAnalysisMetadataAccessor
    {
        string[] GetHeaders();
        Dictionary<string, string> GetContent(ChromatogramPeakFeature feature, MSDecResult msdec, IDataProvider provider);
    }

    public abstract class BaseAnalysisMetadataAccessor : IAnalysisMetadataAccessor
    {
        public BaseAnalysisMetadataAccessor(IMatchResultRefer refer, ParameterBase parameter) {
            this.refer = refer;
            this.parameter = parameter;
        }

        protected readonly ParameterBase parameter;
        protected readonly IMatchResultRefer refer;

        public string[] GetHeaders() => GetHeadersCore();

        public Dictionary<string, string> GetContent(ChromatogramPeakFeature feature, MSDecResult msdec, IDataProvider provider) {
            var matchResult = NullIfUnknown(feature.MatchResults.Representative);
            var reference = refer.Refer(matchResult);
            return GetContentCore(feature, msdec, reference, matchResult, provider.LoadMs1Spectrums());
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
            IReadOnlyList<RawSpectrum> spectrumList) {

            return new Dictionary<string, string> {
                { "Peak ID", feature.MasterPeakID.ToString() },
                { "Name", feature.Name },
                { "Scan", feature.MS1RawSpectrumIdTop.ToString() },
                // "m/z left", "m/z", "m/z right",
                { "Height", string.Format("{0:0}", feature.PeakHeightTop) },
                { "Area", string.Format("{0:0}", feature.PeakAreaAboveZero) },
                { "Model masses", string.Join(" ", msdec.ModelMasses) },
                { "Adduct",  feature.AdductType?.AdductIonName ?? "null" },
                { "Isotope",  feature.PeakCharacter.IsotopeWeightNumber.ToString() },
                { "Comment",  feature.Comment},
                { "Formula", reference?.Formula.FormulaString ?? "null" },
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
                { "MSMS spectrum", GetSpectrumListContent(msdec, spectrumList) }
            };
        }

        protected string GetIsotopesListContent(ChromatogramPeakFeature feature, IReadOnlyList<RawSpectrum> spectrumList) {
            var isotopes = DataAccess.GetIsotopicPeaks(spectrumList, feature.MS1RawSpectrumIdTop, (float)feature.PrecursorMz, parameter.CentroidMs1Tolerance);
            if (isotopes.IsEmptyOrNull()) {
                return "null";
            }
            return string.Join(";", isotopes.Select(isotope => string.Format("{0:F5} {1:F0}", isotope.Mass, isotope.AbsoluteAbundance)));
        }

        protected string GetSpectrumListContent(MSDecResult msdec, IReadOnlyList<RawSpectrum> spectrumList) {
            var spectrum = DataAccess.GetMassSpectrum(spectrumList, msdec, parameter.ExportSpectraType, msdec.RawSpectrumID, parameter);
            if (spectrum.IsEmptyOrNull()) {
                return "null";
            }
            return string.Join(";", spectrum.Select(peak => string.Format("{0:F5} {1:F0}", peak.Mass, peak.Intensity)));
        }

        protected static string ValueOrNull(string value) => string.IsNullOrEmpty(value) ? "null" : value;
        protected static string ValueOrNull(double? value, string format) => value?.ToString(format) ?? "null";
        protected static MsScanMatchResult NullIfUnknown(MsScanMatchResult result) => result.IsUnknown ? null : result;
    }
}
