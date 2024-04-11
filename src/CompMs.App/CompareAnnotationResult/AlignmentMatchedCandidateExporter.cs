using CompMs.Common.Components;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using System.Xml.Linq;

namespace CompMs.App.CompareAnnotationResult
{
    internal sealed class AlignmentMatchedSpotCandidateExporter : MatchedSpotCandidateExporterBase<AlignmentSpotProperty>
    {
        protected override async Task<XElement> ToXmlElement(AlignmentSpotProperty spot, MoleculeMsReference reference, MatchedSpotCandidateCalculator calculator) {
            var spotElement = new XElement("AlignedSpot",
                new XElement("SpotId", spot.MasterAlignmentID),
                new XElement("Adduct", spot.AdductType.AdductIonName),
                new XElement("Mz", spot.MassCenter),
                ToXmlElement(((IChromatogramPeak)spot).ChromXs),
                new XElement("IsMsmsAssigned", spot.IsMsmsAssigned));
            AddIfContentIsNotEmpty(spotElement, "Name", spot.Name);
            var peaks = await spot.AlignedPeakPropertiesTask.ConfigureAwait(false);
            foreach (var peak in peaks) {
                spotElement.Add(ToXml(calculator.Score(peak, reference)));
            }
            return spotElement;
        }

        private XElement ToXml(MatchedSpotCandidate<AlignmentChromPeakFeature> candidate) {
            var peakElement = new XElement("Peak");
            peakElement.Add(new XElement("File", candidate.Spot.FileName));
            AddIfContentIsNotEmpty(peakElement, "Name", candidate.Spot.Name);
            peakElement.Add(new XElement("Adduct", candidate.Spot.PeakCharacter.AdductType.AdductIonName));
            peakElement.Add(new XElement("Mz", candidate.Spot.Mass));
            peakElement.Add(ToXmlElement(candidate.Spot.ChromXsTop));
            peakElement.Add(new XElement("PeakHeight", candidate.Spot.PeakHeightTop));
            if (candidate.Spot.NormalizedPeakHeight > 0) {
                peakElement.Add(new XElement("NormalizedPeakHeight", candidate.Spot.NormalizedPeakHeight));
            }
            peakElement.Add(new XElement("IsMsmsAssigned", candidate.Spot.IsMsmsAssigned));
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
            peakElement.Add(new XElement("TotalScore", candidate.Spot.MatchResults.Representative.TotalScore));
            return peakElement;
        }
    }
}
