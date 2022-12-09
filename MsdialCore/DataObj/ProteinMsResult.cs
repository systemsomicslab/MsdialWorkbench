using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Extension;
using CompMs.Common.Proteomics.DataObj;
using MessagePack;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.MsdialCore.DataObj {
    [MessagePackObject]
    public class ProteinMsResult {
        public ProteinMsResult() { }
        [SerializationConstructor]
        public ProteinMsResult(int index, string databaseID, FastaProperty fastaProperty) {
            DatabaseID = databaseID;
            FastaProperty = fastaProperty;
            Index = index;
        }
        [Key(0)]
        public int Index { get; }
        [Key(1)]
        public string DatabaseID { get; }
        [Key(2)]
        public FastaProperty FastaProperty { get; }
        [Key(3)]
        public bool IsAnnotated { get; set; }
        [Key(4)]
        public List<PeptideMsResult> MatchedPeptideResults { get; set; } = new List<PeptideMsResult>();

        internal void PropertyUpdates() {
            if (IsAnnotated) {
                MatchedAminoAcidSequence = GetMatchedAminoAcidSequence();
                PeptideCoverage = GetPeptideCoverage();
                SequenceWithMatchedInfo = GetSequenceWithMatchedInfo();
                Score = GetScore();
                MinimumValueOfSharedPeptidesInSearchedProteins = GetMinimumValueOfSharedPeptidesInSearchedProteins();
                UniquePeptides = GetUniquePeptides();
                PeakHeights = GetPeakHeights();
                PeakAreaAboveZeros = GetPeakAreaAboveZeros();
                PeakAreaAboveBaseLines = GetPeakAreaAboveBaseLines();
                NormalizedPeakHeights = GetNormalizedPeakHeights();
                NormalizedPeakAreaAboveZeros = GetNormalizedPeakAreaAboveZeros();
                NormalizedPeakAreaAboveBaseLines = GetNormalizedPeakAreaAboveBaseLines();
            }
        }

        [IgnoreMember]
        public float PeptideCoverage { get; set; }
        public float GetPeptideCoverage() { 
            var aaSeq = MatchedAminoAcidSequence;
            return (float)aaSeq.Count(n => n.IsMatched) / (float)aaSeq.Count();
        }

        [IgnoreMember]
        public string SequenceWithMatchedInfo { get; set; }
        public string GetSequenceWithMatchedInfo() {
            var aaSeq = MatchedAminoAcidSequence;
            var sequenceBuilder = new StringBuilder();
            for(int i = 0; i < aaSeq.Count; i++) {
                sequenceBuilder.Append(aaSeq[i].AminoAcidCode.ToString());
                if (aaSeq[i].IsMatched) {
                    sequenceBuilder.Append("'");
                }
            }
            return sequenceBuilder.ToString();
        }

        [IgnoreMember]
        public float Score { get; set; }
        public float GetScore() {
            if (MatchedPeptideResults.IsEmptyOrNull()) return 0;
            return MatchedPeptideResults.Select(n => n.PEPScore).Aggregate((x, y) => x * y);
        }
        [IgnoreMember]
        public int MinimumValueOfSharedPeptidesInSearchedProteins { get; set; }
        public int GetMinimumValueOfSharedPeptidesInSearchedProteins() {
            if (MatchedPeptideResults.IsEmptyOrNull()) return 0;
            return MatchedPeptideResults.Min(n => n.Peptide.SamePeptideNumberInSearchedProteins);
        }
        [IgnoreMember]
        public List<PeptideMsResult> UniquePeptides { get; set; }
        public List<PeptideMsResult> GetUniquePeptides() {
            var minPep = MinimumValueOfSharedPeptidesInSearchedProteins;
            return MatchedPeptideResults.Where(n => n.Peptide.SamePeptideNumberInSearchedProteins == minPep).ToList();
        }
       
        [IgnoreMember]
        public List<double> PeakHeights { get; set; }
        public List<double> GetPeakHeights() {
            var uniquePeps = UniquePeptides;
            var values = new List<double>();
            for (int i = 0; i < SampleCount; i++) { values.Add(0); }
                    
            foreach (var pep in uniquePeps) {
                var heights = pep.Heights;
                for (int i = 0; i < heights.Count; i++) {
                    values[i] += heights[i];
                }
            }
            return values;
        }


        [IgnoreMember]
        public List<double> PeakAreaAboveZeros { get; set; }
        public List<double> GetPeakAreaAboveZeros() {
            var uniquePeps = UniquePeptides;
            var values = new List<double>();
            for (int i = 0; i < SampleCount; i++) { values.Add(0); }

            foreach (var pep in uniquePeps) {
                var areas = pep.AreasAboveZero;
                for (int i = 0; i < areas.Count; i++) {
                    values[i] += areas[i];
                }
            }
            return values;
        }
        [IgnoreMember]
        public List<double> PeakAreaAboveBaseLines { get; set; }
        public List<double> GetPeakAreaAboveBaseLines() {
            var uniquePeps = UniquePeptides;
            var values = new List<double>();
            for (int i = 0; i < SampleCount; i++) { values.Add(0); }

            foreach (var pep in uniquePeps) {
                var areas = pep.AreasAboveBaseline;
                for (int i = 0; i < areas.Count; i++) {
                    values[i] += areas[i];
                }
            }
            return values;
        }

        [IgnoreMember]
        public List<double> NormalizedPeakHeights { get; set; }
        public List<double> GetNormalizedPeakHeights() {
            var uniquePeps = UniquePeptides;
            var values = new List<double>();
            for (int i = 0; i < SampleCount; i++) { values.Add(0); }

            foreach (var pep in uniquePeps) {
                var heights = pep.NormalizedPeakHeights;
                for (int i = 0; i < heights.Count; i++) {
                    values[i] += heights[i];
                }
            }
            return values;
        }
        [IgnoreMember]
        public List<double> NormalizedPeakAreaAboveZeros { get; set; }
        public List<double> GetNormalizedPeakAreaAboveZeros() {
            var uniquePeps = UniquePeptides;
            var values = new List<double>();
            for (int i = 0; i < SampleCount; i++) { values.Add(0); }

            foreach (var pep in uniquePeps) {
                var areas = pep.NormalizedAreasAboveZero;
                for (int i = 0; i < areas.Count; i++) {
                    values[i] += areas[i];
                }
            }
            return values;
        }
        [IgnoreMember]
        public List<double> NormalizedPeakAreaAboveBaseLines { get; set; }
        public List<double> GetNormalizedPeakAreaAboveBaseLines() {
            var uniquePeps = UniquePeptides;
            var values = new List<double>();
            for (int i = 0; i < SampleCount; i++) { values.Add(0); }

            foreach (var pep in uniquePeps) {
                var areas = pep.NormalizedAreasAboveBaseline;
                for (int i = 0; i < areas.Count; i++) {
                    values[i] += areas[i];
                }
            }
            return values;
        }

        [IgnoreMember]
        public List<MatchedAminoacidResidue> MatchedAminoAcidSequence { get; set; }
        public List<MatchedAminoacidResidue> GetMatchedAminoAcidSequence() {
            var objs = new List<MatchedAminoacidResidue>();
            var sequence = FastaProperty.Sequence;
            var ranges = MatchedPeptideResults.Select(n => n.Peptide.Position).ToList();
            for (int i = 0; i < sequence.Length; i++) {
                if (isResidueInRanges(i, ranges)) {
                    objs.Add(new MatchedAminoacidResidue(true, i, sequence[i]));
                }
                else {
                    objs.Add(new MatchedAminoacidResidue(false, i, sequence[i]));
                }
            }
            return objs;
        }

        private bool isResidueInRanges(int i, List<Range> ranges) {
            foreach (var range in ranges) {
                if (range.Start <= i && range.End >= i) return true;
            }
            return false;
        }

        [IgnoreMember]
        private int SampleCount {
            get {
                if (MatchedPeptideResults.IsEmptyOrNull()) return 0;
                var pep = MatchedPeptideResults[0];
                if (pep.AlignmentSpotProperty is null) {
                    return 1;
                }
                else {
                    return pep.AlignmentSpotProperty.AlignedPeakProperties.Count;
                }
            }
        }

        
    }

    [MessagePackObject]
    public class PeptideMsResult {
        public PeptideMsResult() { }
        public PeptideMsResult(Peptide peptide, ChromatogramPeakFeature feature, string id) {
            Peptide = peptide;
            ChromatogramPeakFeature = feature;
            ShotgunProteomicsDatabaseID = id;

            PropertyUpdates();
        }

        public PeptideMsResult(Peptide peptide, AlignmentSpotProperty feature, string id) {
            Peptide = peptide;
            AlignmentSpotProperty = feature;
            ShotgunProteomicsDatabaseID = id;

            PropertyUpdates();
        }

        internal void PropertyUpdates() {
            PEPScore = GetPEPScore();
            Heights = GetHeights();
            AreasAboveZero = GetAreasAboveZero();
            AreasAboveBaseline = GetAreasAboveBaseline();
            NormalizedPeakHeights = GetNormalizedPeakHeights();
            NormalizedAreasAboveZero = GetNormalizedAreasAboveZero();
            NormalizedAreasAboveBaseline = GetNormalizedAreasAboveBaseline();
        }

        [Key(0)]
        public Peptide Peptide { get; set; }
        [Key(1)]
        public ChromatogramPeakFeature ChromatogramPeakFeature { get; set; }
        [Key(2)]
        public AlignmentSpotProperty AlignmentSpotProperty { get; set; }
        [Key(3)]
        public string ShotgunProteomicsDatabaseID { get; set; }

        [IgnoreMember]
        public float PEPScore { get; set; }
        public float GetPEPScore() { 
            return ChromatogramPeakFeature is null && AlignmentSpotProperty is null 
                ? 0.0F 
                : ChromatogramPeakFeature is null 
                    ? AlignmentSpotProperty.MatchResults.Representative.PEPScore 
                    : ChromatogramPeakFeature.MatchResults.Representative.PEPScore;
        }

        [IgnoreMember]
        public List<double> Heights { get; set; }
        public List<double> GetHeights() {
            if (ChromatogramPeakFeature is null && AlignmentSpotProperty is null) return null;
            if (ChromatogramPeakFeature is null) {
                return AlignmentSpotProperty.AlignedPeakProperties.Select(n => n.PeakHeightTop).ToList();
            }
            else {
                return new List<double>() { ChromatogramPeakFeature.PeakHeightTop };
            }
        }

        [IgnoreMember]
        public List<double> AreasAboveZero { get; set; }
        public List<double> GetAreasAboveZero() {
            if (ChromatogramPeakFeature is null && AlignmentSpotProperty is null) return null;
            if (ChromatogramPeakFeature is null) {
                return AlignmentSpotProperty.AlignedPeakProperties.Select(n => n.PeakAreaAboveZero).ToList();
            }
            else {
                return new List<double>() { ChromatogramPeakFeature.PeakAreaAboveZero };
            }
        }

        [IgnoreMember]
        public List<double> AreasAboveBaseline { get; set; }
        public List<double> GetAreasAboveBaseline() {
            if (ChromatogramPeakFeature is null && AlignmentSpotProperty is null) return null;
            if (ChromatogramPeakFeature is null) {
                return AlignmentSpotProperty.AlignedPeakProperties.Select(n => n.PeakAreaAboveBaseline).ToList();
            }
            else {
                return new List<double>() { ChromatogramPeakFeature.PeakAreaAboveBaseline };
            }
        }

        [IgnoreMember]
        public List<double> NormalizedPeakHeights { get; set; }
        public List<double> GetNormalizedPeakHeights() {
            if (ChromatogramPeakFeature is null && AlignmentSpotProperty is null) return null;
            if (ChromatogramPeakFeature is null) {
                return AlignmentSpotProperty.AlignedPeakProperties.Select(n => n.NormalizedPeakHeight).ToList();
            }
            else {
                return new List<double>();
            }
        }

        [IgnoreMember]
        public List<double> NormalizedAreasAboveZero { get; set; }
        public List<double> GetNormalizedAreasAboveZero() {
            if (ChromatogramPeakFeature is null && AlignmentSpotProperty is null) return null;
            if (ChromatogramPeakFeature is null) {
                return AlignmentSpotProperty.AlignedPeakProperties.Select(n => n.NormalizedPeakAreaAboveZero).ToList();
            }
            else {
                return new List<double>();
            }
        }

        [IgnoreMember]
        public List<double> NormalizedAreasAboveBaseline { get; set; }
        public List<double> GetNormalizedAreasAboveBaseline() {
            if (ChromatogramPeakFeature is null && AlignmentSpotProperty is null) return null;
            if (ChromatogramPeakFeature is null) {
                return AlignmentSpotProperty.AlignedPeakProperties.Select(n => n.NormalizedPeakAreaAboveBaseline).ToList();
            }
            else {
                return new List<double>();
            }
        }
    }

    public class MatchedAminoacidResidue {
        public bool IsMatched { get; }
        public char AminoAcidCode { get; }
        public int Index { get; }
        public MatchedAminoacidResidue(bool isMatched, int index, char code) {
            IsMatched = isMatched;
            Index = index;
            AminoAcidCode = code;
        }
    }
}
