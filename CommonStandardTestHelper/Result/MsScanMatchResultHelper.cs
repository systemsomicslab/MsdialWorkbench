using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.Common.DataObj.Result.Tests
{
    public static class MsScanMatchResultHelper
    {
        public static void AreEqual(this Assert assert, MsScanMatchResult expected, MsScanMatchResult actual) {
            Assert.AreEqual(expected.Name, actual.Name);
            Assert.AreEqual(expected.InChIKey, actual.InChIKey);
            Assert.AreEqual(expected.TotalScore, actual.TotalScore);
            Assert.AreEqual(expected.WeightedDotProduct, actual.WeightedDotProduct);
            Assert.AreEqual(expected.SimpleDotProduct, actual.SimpleDotProduct);
            Assert.AreEqual(expected.ReverseDotProduct, actual.ReverseDotProduct);
            Assert.AreEqual(expected.MatchedPeaksCount, actual.MatchedPeaksCount);
            Assert.AreEqual(expected.MatchedPeaksPercentage, actual.MatchedPeaksPercentage);
            Assert.AreEqual(expected.EssentialFragmentMatchedScore, actual.EssentialFragmentMatchedScore);
            Assert.AreEqual(expected.AndromedaScore, actual.AndromedaScore);
            Assert.AreEqual(expected.PEPScore, actual.PEPScore);
            Assert.AreEqual(expected.RtSimilarity, actual.RtSimilarity);
            Assert.AreEqual(expected.RiSimilarity, actual.RiSimilarity);
            Assert.AreEqual(expected.CcsSimilarity, actual.CcsSimilarity);
            Assert.AreEqual(expected.IsotopeSimilarity, actual.IsotopeSimilarity);
            Assert.AreEqual(expected.AcurateMassSimilarity, actual.AcurateMassSimilarity);
            Assert.AreEqual(expected.LibraryID, actual.LibraryID);
            Assert.AreEqual(expected.LibraryIDWhenOrdered, actual.LibraryIDWhenOrdered);
            Assert.AreEqual(expected.IsPrecursorMzMatch, actual.IsPrecursorMzMatch);
            Assert.AreEqual(expected.IsSpectrumMatch, actual.IsSpectrumMatch);
            Assert.AreEqual(expected.IsRtMatch, actual.IsRtMatch);
            Assert.AreEqual(expected.IsRiMatch, actual.IsRiMatch);
            Assert.AreEqual(expected.IsCcsMatch, actual.IsCcsMatch);
            Assert.AreEqual(expected.IsLipidClassMatch, actual.IsLipidClassMatch);
            Assert.AreEqual(expected.IsLipidChainsMatch, actual.IsLipidChainsMatch);
            Assert.AreEqual(expected.IsLipidPositionMatch, actual.IsLipidPositionMatch);
            Assert.AreEqual(expected.IsLipidDoubleBondPositionMatch, actual.IsLipidDoubleBondPositionMatch);
            Assert.AreEqual(expected.IsOtherLipidMatch, actual.IsOtherLipidMatch);
            Assert.AreEqual(expected.Source, actual.Source);
            Assert.AreEqual(expected.AnnotatorID, actual.AnnotatorID);
            Assert.AreEqual(expected.SpectrumID, actual.SpectrumID);
            Assert.AreEqual(expected.IsDecoy, actual.IsDecoy);
            Assert.AreEqual(expected.Priority, actual.Priority);
            Assert.AreEqual(expected.IsReferenceMatched, actual.IsReferenceMatched);
            Assert.AreEqual(expected.IsAnnotationSuggested, actual.IsAnnotationSuggested);
        }
    }
}