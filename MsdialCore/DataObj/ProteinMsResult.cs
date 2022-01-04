using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Extension;
using CompMs.Common.Interfaces;
using CompMs.Common.Proteomics.DataObj;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.MsdialCore.DataObj {
    [MessagePackObject]
    public class ProteinMsResult {
        public ProteinMsResult() { }
        public ProteinMsResult(string databaseID, int index, FastaProperty fasta) {
            DatabaseID = databaseID;
            FastaProperty = fasta;
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
        [IgnoreMember]
        public float PeptideCoverage { 
            get {
                var aaSeq = GetMatchedAminoAcidSequence();
                return (float)aaSeq.Count(n => n.IsMatched) / (float)aaSeq.Count();
            } 
        }

        [IgnoreMember]
        public string SequenceWithMatchedInfo {
            get {
                var aaSeq = GetMatchedAminoAcidSequence();
                var sequence = string.Empty;
                for(int i = 0; i < aaSeq.Count; i++) {
                    if (aaSeq[i].IsMatched) {
                        sequence += aaSeq[i].AminoAcidCode.ToString() + "'";
                    }
                    else {
                        sequence += aaSeq[i].AminoAcidCode.ToString();
                    }
                }
                return sequence;
            }
        }

        [IgnoreMember]
        public float Score {
            get {
                return MatchedPeptideResults.Select(n => n.PEPScore).Aggregate((x, y) => x * y);
            }
        }
        [IgnoreMember]
        public int MinimumValueOfSharedPeptidesInSearchedProteins { get => MatchedPeptideResults.Min(n => n.Peptide.SamePeptideNumberInSearchedProteins); }
        [IgnoreMember]
        public List<PeptideMsResult> UniquePeptides {
            get {
                var minPep = MinimumValueOfSharedPeptidesInSearchedProteins;
                return MatchedPeptideResults.Where(n => n.Peptide.SamePeptideNumberInSearchedProteins == minPep).ToList();
            } 
        }
        [IgnoreMember]
        public List<double> PeakHeights {
            get {
                var uniquePeps = UniquePeptides;
                var values = new List<double>();
                for (int i = 0; i < SampleCount; i++) { values.Add(0); }
                    
                foreach (var pep in uniquePeps) {
                    var heights = pep.Heights();
                    for (int i = 0; i < heights.Count; i++) {
                        values[i] += heights[i];
                    }
                }
                return values;
            } 
        }
        [IgnoreMember]
        public List<double> PeakAreaAboveZeros {
            get {
                var uniquePeps = UniquePeptides;
                var values = new List<double>();
                for (int i = 0; i < SampleCount; i++) { values.Add(0); }

                foreach (var pep in uniquePeps) {
                    var areas = pep.AreasAboveZero();
                    for (int i = 0; i < areas.Count; i++) {
                        values[i] += areas[i];
                    }
                }
                return values;
            }
        }
        [IgnoreMember]
        public List<double> PeakAreaAboveBaseLines {
            get {
                var uniquePeps = UniquePeptides;
                var values = new List<double>();
                for (int i = 0; i < SampleCount; i++) { values.Add(0); }

                foreach (var pep in uniquePeps) {
                    var areas = pep.AreasAboveBaseline();
                    for (int i = 0; i < areas.Count; i++) {
                        values[i] += areas[i];
                    }
                }
                return values;
            }
        }

        [IgnoreMember]
        public List<double> NormalizedPeakHeights {
            get {
                var uniquePeps = UniquePeptides;
                var values = new List<double>();
                for (int i = 0; i < SampleCount; i++) { values.Add(0); }

                foreach (var pep in uniquePeps) {
                    var heights = pep.NormalizedPeakHeights();
                    for (int i = 0; i < heights.Count; i++) {
                        values[i] += heights[i];
                    }
                }
                return values;
            }
        }
        [IgnoreMember]
        public List<double> NormalizedPeakAreaAboveZeros {
            get {
                var uniquePeps = UniquePeptides;
                var values = new List<double>();
                for (int i = 0; i < SampleCount; i++) { values.Add(0); }

                foreach (var pep in uniquePeps) {
                    var areas = pep.NormalizedAreasAboveZero();
                    for (int i = 0; i < areas.Count; i++) {
                        values[i] += areas[i];
                    }
                }
                return values;
            }
        }
        [IgnoreMember]
        public List<double> NormalizedPeakAreaAboveBaseLines {
            get {
                var uniquePeps = UniquePeptides;
                var values = new List<double>();
                for (int i = 0; i < SampleCount; i++) { values.Add(0); }

                foreach (var pep in uniquePeps) {
                    var areas = pep.NormalizedAreasAboveBaseline();
                    for (int i = 0; i < areas.Count; i++) {
                        values[i] += areas[i];
                    }
                }
                return values;
            }
        }


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
        public PeptideMsResult(Peptide peptide, ChromatogramPeakFeature feature) {
            Peptide = peptide;
            ChromatogramPeakFeature = feature;
        }

        public PeptideMsResult(Peptide peptide, AlignmentSpotProperty feature) {
            Peptide = peptide;
            AlignmentSpotProperty = feature;
        }

        [Key(0)]
        public Peptide Peptide { get; }
        [Key(1)]
        public ChromatogramPeakFeature ChromatogramPeakFeature { get; }
        [Key(2)]
        public AlignmentSpotProperty AlignmentSpotProperty { get; }
        [IgnoreMember]
        public float PEPScore { get => ChromatogramPeakFeature is null && AlignmentSpotProperty is null 
                ? 0.0F 
                : ChromatogramPeakFeature is null 
                    ? AlignmentSpotProperty.MatchResults.Representative.PEPScore 
                    : ChromatogramPeakFeature.MatchResults.Representative.PEPScore;
        }

        public List<double> Heights() {
            if (ChromatogramPeakFeature is null && AlignmentSpotProperty is null) return null;
            if (ChromatogramPeakFeature is null) {
                return AlignmentSpotProperty.AlignedPeakProperties.Select(n => n.PeakHeightTop).ToList();
            }
            else {
                return new List<double>() { ChromatogramPeakFeature.PeakHeightTop };
            }
        }

        public List<double> AreasAboveZero() {
            if (ChromatogramPeakFeature is null && AlignmentSpotProperty is null) return null;
            if (ChromatogramPeakFeature is null) {
                return AlignmentSpotProperty.AlignedPeakProperties.Select(n => n.PeakAreaAboveZero).ToList();
            }
            else {
                return new List<double>() { ChromatogramPeakFeature.PeakAreaAboveZero };
            }
        }

        public List<double> AreasAboveBaseline() {
            if (ChromatogramPeakFeature is null && AlignmentSpotProperty is null) return null;
            if (ChromatogramPeakFeature is null) {
                return AlignmentSpotProperty.AlignedPeakProperties.Select(n => n.PeakAreaAboveBaseline).ToList();
            }
            else {
                return new List<double>() { ChromatogramPeakFeature.PeakAreaAboveBaseline };
            }
        }

        public List<double> NormalizedPeakHeights() {
            if (ChromatogramPeakFeature is null && AlignmentSpotProperty is null) return null;
            if (ChromatogramPeakFeature is null) {
                return AlignmentSpotProperty.AlignedPeakProperties.Select(n => n.NormalizedPeakHeight).ToList();
            }
            else {
                return new List<double>();
            }
        }

        public List<double> NormalizedAreasAboveZero() {
            if (ChromatogramPeakFeature is null && AlignmentSpotProperty is null) return null;
            if (ChromatogramPeakFeature is null) {
                return AlignmentSpotProperty.AlignedPeakProperties.Select(n => n.NormalizedPeakAreaAboveZero).ToList();
            }
            else {
                return new List<double>();
            }
        }

        public List<double> NormalizedAreasAboveBaseline() {
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
