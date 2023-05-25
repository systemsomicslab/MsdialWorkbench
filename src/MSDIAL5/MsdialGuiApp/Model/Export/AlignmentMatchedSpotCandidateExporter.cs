using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.DataObj;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CompMs.App.Msdial.Model.Export
{
    internal sealed class AlignmentMatchedSpotCandidateExporter
    {
        public async Task ExportAsync(Stream stream, IReadOnlyList<MatchedSpotCandidate<AlignmentSpotPropertyModel>> candidates, MatchedSpotCandidateCalculator calculator) {
            var doc = new XElement("MatchedSpots");
            var tasks = candidates.Select(candidate => ToXmlElement(candidate, calculator)).ToArray();
            var elements = await Task.WhenAll(tasks).ConfigureAwait(false);
            foreach (var element in elements) {
                doc.Add(element);
            }
            doc.Save(stream);
        }

        private async Task<XElement> ToXmlElement(MatchedSpotCandidate<AlignmentSpotPropertyModel> candidate, MatchedSpotCandidateCalculator calculator) {
            var spot = await ToXmlElement(candidate.Spot, candidate.Reference, calculator).ConfigureAwait(false);
            var reference = ToXmlElement(candidate.Reference);
            var result = new XElement("MatchedSpot", spot, reference);

            if (candidate.IsLipidReference) {
                result.Add(new XElement("IsExactlyReference", candidate.IsExactlyReference));
                result.Add(new XElement("IsSubgroupOfReference", candidate.IsSubgroupOfReference));
                result.Add(new XElement("IsSupergroupOfReference", candidate.IsSupergroupOfReference));
            }
            else {
                result.Add(new XElement("Annotated", candidate.IsAnnotated));
            }
            result.Add(new XElement("SimilarByMz", candidate.IsSimilarByMz));
            result.Add(new XElement("SimilarByTime", candidate.IsSimilarByTime));
            result.Add(new XElement("StrongerThanThreshold", candidate.IsStrongerThanThreshold));
            return result;
        }

        private XElement ToXmlElement(MoleculeMsReference reference) {
            var referenceElement = new XElement("Reference",
                new XElement("Adduct", reference.AdductType.AdductIonName));
            AddIfContentIsNotEmpty(referenceElement, "Name", reference.Name);
            if (reference.PrecursorMz > 0) {
                referenceElement.Add(new XElement("Mz", reference.PrecursorMz));
            }
            if (reference.ChromXs != null) {
                referenceElement.Add(ToXmlElement(reference.ChromXs));
            }
            return referenceElement;
        }

        private async Task<XElement> ToXmlElement(AlignmentSpotPropertyModel spot, MoleculeMsReference reference, MatchedSpotCandidateCalculator calculator) {
            var spotElement = new XElement("AlignedSpot",
                new XElement("SpotId", spot.MasterAlignmentID),
                new XElement("Adduct", spot.AdductIonName),
                new XElement("Mz", spot.Mass),
                ToXmlElement(((IChromatogramPeak)spot).ChromXs));
            AddIfContentIsNotEmpty(spotElement, "Name", spot.Name);
            var task = spot.AlignedPeakPropertiesModelProperty.ToTask();
            var peaks = spot.AlignedPeakPropertiesModelProperty.Value;
            if (peaks is null) {
                peaks = await task.ConfigureAwait(false);
            }
            foreach (var peak in peaks) {
                spotElement.Add(ToXml(calculator.Score(peak, reference)));
            }
            return spotElement;
        }

        private XElement ToXml(MatchedSpotCandidate<AlignmentChromPeakFeatureModel> candidate) {
            var peakElement = new XElement("Peak");
            peakElement.Add(new XElement("File", candidate.Spot.FileName));
            AddIfContentIsNotEmpty(peakElement, "Name", candidate.Spot.Name);
            peakElement.Add(new XElement("Adduct", candidate.Spot.Adduct.AdductIonName));
            peakElement.Add(new XElement("Mz", candidate.Spot.Mass));
            peakElement.Add(ToXmlElement(candidate.Spot.ChromXsTop));
            peakElement.Add(new XElement("PeakHeight", candidate.Spot.PeakHeightTop));
            if (candidate.Spot.NormalizedPeakHeight > 0) {
                peakElement.Add(new XElement("NormalizedPeakHeight", candidate.Spot.NormalizedPeakHeight));
            }
            if (candidate.IsLipidReference) {
                peakElement.Add(new XElement("IsExactlyReference", candidate.IsExactlyReference));
                peakElement.Add(new XElement("IsSubgroupOfReference", candidate.IsSubgroupOfReference));
                peakElement.Add(new XElement("IsSupergroupOfReference", candidate.IsSupergroupOfReference));
            }
            else {
                peakElement.Add(new XElement("Annotated", candidate.IsAnnotated));
            }
            peakElement.Add(new XElement("SimilarByMz", candidate.IsSimilarByMz));
            peakElement.Add(new XElement("SimilarByTime", candidate.IsSimilarByTime));
            peakElement.Add(new XElement("StrongerThanThreshold", candidate.IsStrongerThanThreshold));
            return peakElement;
        }

        private XElement ToXmlElement(ChromXs chrom) {
            var time = new XElement("Time");
            switch (chrom.MainType) {
                case ChromXType.RT:
                    time.Add(new XElement("Type", "RetentionTime"));
                    break;
                case ChromXType.RI:
                    time.Add(new XElement("Type", "RetentionIndex"));
                    break;
                case ChromXType.Drift:
                    time.Add(new XElement("Type", "DriftTime"));
                    break;
                case ChromXType.Mz:
                    time.Add(new XElement("Type", "Mz"));
                    break;
            }
            if (chrom.RT.Value > 0) {
                time.Add(new XElement("RetentionTime", chrom.RT.Value));
            }
            if (chrom.RI.Value > 0) {
                time.Add(new XElement("RetentionIndex", chrom.RI.Value));
            }
            if (chrom.Drift.Value > 0) {
                time.Add(new XElement("DriftTime", chrom.Drift.Value));
            }
            if (chrom.Mz.Value > 0) {
                time.Add(new XElement("Mz", chrom.Mz.Value));
            }
            return time;
        }

        private void AddIfContentIsNotEmpty(XElement element, string xName, string content) {
            if (!string.IsNullOrEmpty(content)) {
                element.Add(new XElement(xName, content));
            }
        }
    }
}
