using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parser;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.MsdialGcMsApi.Export
{
    public sealed class GcmsAnalysisMetadataAccessor : IAnalysisMetadataAccessor<SpectrumFeature>
    {
        private readonly IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> _refer;
        private readonly IMsScanPropertyLoader<SpectrumFeature> _loader;
        private readonly bool _trimSpectrumToExcelLimit;

        public GcmsAnalysisMetadataAccessor(IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer, IMsScanPropertyLoader<SpectrumFeature> loader, bool trimSpectrumToExcelLimit = true) {
            _refer = refer;
            _loader = loader;
            _trimSpectrumToExcelLimit = trimSpectrumToExcelLimit;
        }

        public string[] GetHeaders() {
            return new string[] {
                "Name",
                "Scan",
                "RT (min)",
                "Retention index",
                "Model Masses",
                "Model ion mz",
                "Model ion height",
                "Model ion area",
                "Integrated height",
                "Integrated area",
                "SMILES",
                "InChIKey",
                "Simple dot product",
                "Weighted dot product",
                "Reverse dot product",
                "Fragment presence %",
                "Total score",
                "Spectrum"
            };
        }

        public Dictionary<string, string> GetContent(SpectrumFeature feature) {
            var scan = _loader.Load(feature);
            MSDecResult msdec = feature.AnnotatedMSDecResult.MSDecResult;
            var matchResult = NullIfUnknown(feature.AnnotatedMSDecResult.MatchResults.Representative);
            var reference = _refer.Refer(matchResult);
            return new Dictionary<string, string>
            {
                ["Name"] = UnknownIfEmpty(feature.AnnotatedMSDecResult.Molecule.Name),
                ["Scan"] = msdec.RawSpectrumID.ToString(),
                ["RT (min)"] = msdec.ChromXs.RT.Value.ToString("F3"),
                ["Retention index"] = EmptyIfNegative(msdec.ChromXs.RI.Value, "F2"),
                ["Model Masses"] = "[" + string.Join(",", msdec.ModelMasses) + "]",
                ["Model ion mz"] = feature.QuantifiedChromatogramPeak.PeakFeature.Mass.ToString(),
                ["Model ion height"] = feature.QuantifiedChromatogramPeak.PeakFeature.PeakHeightTop.ToString(),
                ["Model ion area"] = feature.QuantifiedChromatogramPeak.PeakFeature.PeakAreaAboveZero.ToString(),
                ["Integrated height"] = msdec.IntegratedHeight.ToString(),
                ["Integrated area"] = msdec.IntegratedArea.ToString(),
                ["SMILES"] = reference?.SMILES ?? string.Empty,
                ["InChIKey"] = reference?.InChIKey ?? string.Empty,
                ["Simple dot product"] = NegativeIfNull(matchResult?.SimpleDotProduct, "F2"),
                ["Weighted dot product"] = NegativeIfNull(matchResult?.WeightedDotProduct, "F2"),
                ["Reverse dot product"] = NegativeIfNull(matchResult?.ReverseDotProduct, "F2"),
                ["Fragment presence %"] = NegativeIfNull(matchResult?.MatchedPeaksPercentage, "F2"),
                ["Total score"] = NegativeIfNull(matchResult?.TotalScore, "F2"),
                ["Spectrum"] = EncodeSpectrum(scan),
            };
        }

        private string EncodeSpectrum(IMSScanProperty scan) {
            if (scan.Spectrum is null) {
                return "null";
            }
            var spectrum = scan.Spectrum;
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

        private static string EmptyIfNegative(double value, string format) => value < 0 ? string.Empty : value.ToString(format);
        private static string UnknownIfEmpty(string value) => string.IsNullOrEmpty(value) ? "Unknown" : value;
        private static string NegativeIfNull(double? value, string format) => value?.ToString(format) ?? "-1";
        private static MsScanMatchResult NullIfUnknown(MsScanMatchResult result) => result.IsUnknown ? null : result;
    }
}
