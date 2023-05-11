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
        public async Task ExportAsync(Stream stream, IReadOnlyList<MatchedSpotCandidate<AlignmentSpotPropertyModel>> candidates) {
            var doc = new XElement("MatchedSpots");
            var tasks = candidates.Select(ToXmlElement).ToArray();
            var elements = await Task.WhenAll(tasks).ConfigureAwait(false);
            foreach (var element in elements) {
                doc.Add(element);
            }
            doc.Save(stream);
        }

        private async Task<XElement> ToXmlElement(MatchedSpotCandidate<AlignmentSpotPropertyModel> candidate) {
            var spot = await ToXmlElement(candidate.Spot).ConfigureAwait(false);
            var reference = ToXmlElement(candidate.Reference);
            var annotated = new XElement("Annotated", candidate.IsAnnotated);
            var similarWithMz = new XElement("SimilarByMz", candidate.IsSimilarByMz);
            var similarWithChrom = new XElement("SimilarByTime", candidate.IsSimilarByTime);
            return new XElement("MatchedSpot", reference, spot, annotated, similarWithMz, similarWithChrom);
        }

        private XElement ToXmlElement(MoleculeMsReference reference) {
            var referenceElement = new XElement("Reference",
                new XElement("Name", reference.Name),
                new XElement("Adduct", reference.AdductType.AdductIonName));
            if (reference.PrecursorMz > 0) {
                referenceElement.Add(new XElement("Mz", reference.PrecursorMz));
            }
            if (reference.ChromXs != null) {
                referenceElement.Add(ToXmlElement(reference.ChromXs));
            }
            return referenceElement;
        }

        private async Task<XElement> ToXmlElement(AlignmentSpotPropertyModel spot) {
            var spotElement = new XElement("AlignedSpot",
                new XElement("SpotId", spot.MasterAlignmentID),
                new XElement("Name", spot.Name),
                new XElement("Adduct", spot.AdductIonName),
                new XElement("Mz", spot.Mass),
                ToXmlElement(((IChromatogramPeak)spot).ChromXs));
            var task = spot.AlignedPeakPropertiesModelProperty.ToTask();
            var peaks = spot.AlignedPeakPropertiesModelProperty.Value;
            if (peaks is null) {
                peaks = await task.ConfigureAwait(false);
            }
            foreach (var peak in peaks) {
                spotElement.Add(ToXml(peak));
            }
            return spotElement;
        }

        private XElement ToXml(AlignmentChromPeakFeatureModel peak) {
            var peakElement = new XElement("Peak");
            peakElement.Add(new XElement("File", peak.FileName));
            peakElement.Add(new XElement("Name", peak.Name));
            peakElement.Add(new XElement("Adduct", peak.Adduct.AdductIonName));
            peakElement.Add(new XElement("Mz", peak.Mass));
            peakElement.Add(ToXmlElement(peak.ChromXsTop));
            peakElement.Add(new XElement("PeakHeight", peak.PeakHeightTop));
            if (peak.NormalizedPeakHeight > 0) {
                peakElement.Add(new XElement("NormalizedPeakHeight", peak.NormalizedPeakHeight));
            }
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
    }
}
