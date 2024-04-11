using CompMs.Common.Components;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.DataObj;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CompMs.MsdialCore.Export
{
    public abstract class MatchedSpotCandidateExporterBase<T> where T: IAnnotatedObject, IChromatogramPeak
    {
        public async Task ExportAsync(Stream stream, IReadOnlyList<MatchedSpotCandidate<T>> candidates, MatchedSpotCandidateCalculator calculator) {
            var doc = new XElement("MatchedSpots");
            var tasks = candidates.Select(candidate => ToXmlElement(candidate, calculator)).ToArray();
            var elements = await Task.WhenAll(tasks).ConfigureAwait(false);
            foreach (var element in elements) {
                doc.Add(element);
            }
            doc.Save(stream);
        }

        private async Task<XElement> ToXmlElement(MatchedSpotCandidate<T> candidate, MatchedSpotCandidateCalculator calculator) {
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
            result.Add(new XElement("TotalScore", candidate.Spot.MatchResults.Representative.TotalScore));
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

        protected abstract Task<XElement> ToXmlElement(T spot, MoleculeMsReference reference, MatchedSpotCandidateCalculator calculator);

        protected XElement ToXmlElement(ChromXs chrom) {
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

        protected void AddIfContentIsNotEmpty(XElement element, string xName, string content) {
            if (!string.IsNullOrEmpty(content)) {
                element.Add(new XElement(xName, content));
            }
        }
    }
}
