using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Interfaces;
using CompMs.Common.Utility;
using CompMs.MsdialCore.DataObj;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Model.DataObj
{
    internal sealed class PeptideModel {
        public object? AnnotatedSpot { get; }
        public string? AdductType { get; }
        //public string PeptideSeq => _peptideMsResult.Peptide.Sequence;
        public string PeptideSeq { get; }
        public string DatabaseOrigin { get; }
        public int DatabaseOriginID { get; }
        public string ModifiedSequence { get; }
        public Range Position { get; }
        public double ExactMass { get; }
        public Formula Formula { get; }
        public bool IsProteinNterminal { get; }
        public bool IsProteinCterminal { get; }
        public bool IsDecoy { get; }
        public int MissedCleavages { get; }
        public int SamePeptideNumberInSearchedProteins { get; }
        public int CountModifiedAminoAcids { get; }

        public PeptideModel(PeptideMsResult peptideMsResult, IReadOnlyList<ChromatogramPeakFeatureModel> orderedPeaks)
        {
            var idx = orderedPeaks.BinarySearch(peptideMsResult.ChromatogramPeakFeature, COMPARER);
            var peak = idx >= 0 ? orderedPeaks[idx] : null;
            AnnotatedSpot = peak;
            AdductType = peak?.AdductType.AdductIonName;
            PeptideSeq = peptideMsResult.Peptide.Sequence;
            DatabaseOrigin = peptideMsResult.Peptide.DatabaseOrigin;
            DatabaseOriginID = peptideMsResult.Peptide.DatabaseOriginID;
            ModifiedSequence = peptideMsResult.Peptide.ModifiedSequence;
            Formula = peptideMsResult.Peptide.Formula;
            Position = peptideMsResult.Peptide.Position;
            ExactMass = peptideMsResult.Peptide.ExactMass;
            IsProteinNterminal = peptideMsResult.Peptide.IsProteinNterminal;
            IsProteinCterminal = peptideMsResult.Peptide.IsProteinCterminal;
            IsDecoy = peptideMsResult.Peptide.IsDecoy;
            MissedCleavages = peptideMsResult.Peptide.MissedCleavages;
            SamePeptideNumberInSearchedProteins = peptideMsResult.Peptide.SamePeptideNumberInSearchedProteins;
            CountModifiedAminoAcids = peptideMsResult.Peptide.CountModifiedAminoAcids();
        }

        public PeptideModel(PeptideMsResult peptideMsResult, IReadOnlyList<AlignmentSpotPropertyModel> orderedSpots)
        {
            var idx = orderedSpots.BinarySearch(peptideMsResult.AlignmentSpotProperty, COMPARER);
            var peak = idx >= 0 ? orderedSpots[idx] : null;
            AnnotatedSpot = peak;
            AdductType = peak?.AdductType.AdductIonName;
            PeptideSeq = peptideMsResult.Peptide.Sequence;
            DatabaseOrigin = peptideMsResult.Peptide.DatabaseOrigin;
            DatabaseOriginID = peptideMsResult.Peptide.DatabaseOriginID;
            ModifiedSequence = peptideMsResult.Peptide.ModifiedSequence;
            Formula = peptideMsResult.Peptide.Formula;
            Position = peptideMsResult.Peptide.Position;
            ExactMass = peptideMsResult.Peptide.ExactMass;
            IsProteinNterminal = peptideMsResult.Peptide.IsProteinNterminal;
            IsProteinCterminal = peptideMsResult.Peptide.IsProteinCterminal;
            IsDecoy = peptideMsResult.Peptide.IsDecoy;
            MissedCleavages = peptideMsResult.Peptide.MissedCleavages;
            SamePeptideNumberInSearchedProteins = peptideMsResult.Peptide.SamePeptideNumberInSearchedProteins;
            CountModifiedAminoAcids = peptideMsResult.Peptide.CountModifiedAminoAcids();
        }

        private static readonly IComparer<IChromatogramPeak> COMPARER = new ChromatogramPeakComparer();

        private class ChromatogramPeakComparer : IComparer<IChromatogramPeak>
        {
            public int Compare(IChromatogramPeak x, IChromatogramPeak y) {
                return x.ID.CompareTo(y.ID);
            }
        }
    }
}
