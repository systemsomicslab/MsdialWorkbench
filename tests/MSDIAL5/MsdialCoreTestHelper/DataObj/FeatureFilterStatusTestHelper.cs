using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.MsdialCore.DataObj.Tests
{
    public static class FeatureFilterStatusTestHelper {
        public static FeatureFilterStatus CreateSample() {
            return new FeatureFilterStatus
            {
                IsAbundanceFiltered = true, 
                IsRefMatchedFiltered = false,
                IsSuggestedFiltered = true,
                IsUnknownFiltered = false,
                IsBlankFiltered = true,
                IsMsmsContainedFiltered = false,
                IsFragmentExistFiltered = false,
                IsCommentFiltered = false,
            };
        }

        public static void AreEqual(this Assert assert, FeatureFilterStatus expected, FeatureFilterStatus actual) {
            Assert.AreEqual(expected.IsAbundanceFiltered, actual.IsAbundanceFiltered);
            Assert.AreEqual(expected.IsRefMatchedFiltered, actual.IsRefMatchedFiltered);
            Assert.AreEqual(expected.IsSuggestedFiltered , actual.IsSuggestedFiltered);
            Assert.AreEqual(expected.IsUnknownFiltered, actual.IsUnknownFiltered);
            Assert.AreEqual(expected.IsBlankFiltered, actual.IsBlankFiltered);
            Assert.AreEqual(expected.IsMsmsContainedFiltered, actual.IsMsmsContainedFiltered);
            Assert.AreEqual(expected.IsFragmentExistFiltered, actual.IsFragmentExistFiltered);
            Assert.AreEqual(expected.IsCommentFiltered, actual.IsCommentFiltered);
        }
    }
}
