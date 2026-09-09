using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CompMs.MsdialCore.Algorithm.Tests
{
    /// <summary>
    /// The target-formula path records its terms too.
    /// </summary>
    /// <remarks>
    /// This one is easy to overlook because it is not an annotator: a target formula list is
    /// matched against aligned spots inside <see cref="IsotopeTracking"/>, on mass and retention
    /// time alone, and its result lands in the same match-result container as everything else. A
    /// consumer reading that container cannot tell where a result came from, so a result from here
    /// that carried no record would look like an annotator that measured nothing.
    /// </remarks>
    [TestClass()]
    public class IsotopeTrackingMeasuredTermsTests
    {
        [TestMethod()]
        public void ATargetFormulaMatchRecordsMassAndRetentionTime() {
            var spot = Spot(massCenter: 100.0, retentionTime: 2.0);
            var container = Container(spot);
            var formula = Formula(precursorMz: 100.0, retentionTime: 2.0);

            IsotopeTracking.SetTargetFormulaInformation(container, new List<MoleculeMsReference> { formula, }, Parameter());

            Assert.IsNotNull(spot.TextDbBasedMatchResult, "the spot was expected to match the target formula");
            Assert.AreEqual(
                MeasuredTerms.AccurateMass | MeasuredTerms.RetentionTime,
                spot.TextDbBasedMatchResult.MeasuredTerms);
        }

        [TestMethod()]
        public void ATargetFormulaWithoutATimeRecordsMassOnly() {
            // A target formula list may carry masses alone. Nothing here compares a retention time
            // then, and nothing here ever opens a spectrum.
            var spot = Spot(massCenter: 100.0, retentionTime: 2.0);
            var container = Container(spot);
            var formula = Formula(precursorMz: 100.0, retentionTime: 0.0);

            IsotopeTracking.SetTargetFormulaInformation(container, new List<MoleculeMsReference> { formula, }, Parameter());

            Assert.IsNotNull(spot.TextDbBasedMatchResult, "the spot was expected to match the target formula");
            Assert.AreEqual(MeasuredTerms.AccurateMass, spot.TextDbBasedMatchResult.MeasuredTerms);
        }

        private static ParameterBase Parameter() {
            var parameter = new ParameterBase { CentroidMs1Tolerance = 0.01f, };
            parameter.TextDbSearchParam.Ms1Tolerance = 0.01f;
            parameter.TextDbSearchParam.RtTolerance = 0.5f;
            parameter.TextDbSearchParam.TotalScoreCutoff = 0f;
            return parameter;
        }

        private static AlignmentResultContainer Container(AlignmentSpotProperty spot) {
            return new AlignmentResultContainer
            {
                TotalAlignmentSpotCount = 1,
                AlignmentSpotProperties = new ObservableCollection<AlignmentSpotProperty> { spot, },
            };
        }

        private static AlignmentSpotProperty Spot(double massCenter, double retentionTime) {
            return new AlignmentSpotProperty
            {
                AlignmentID = 0,
                MassCenter = massCenter,
                TimesCenter = new ChromXs(retentionTime, ChromXType.RT, ChromXUnit.Min),
            };
        }

        private static MoleculeMsReference Formula(double precursorMz, double retentionTime) {
            return new MoleculeMsReference
            {
                ScanID = 0,
                Name = "a target formula",
                InChIKey = "DUMMYINCHIKEY",
                Formula = new Common.DataObj.Property.Formula(),
                PrecursorMz = precursorMz,
                AdductType = AdductIon.GetAdductIon("[M+H]+"),
                ChromXs = new ChromXs(retentionTime, ChromXType.RT, ChromXUnit.Min),
            };
        }
    }
}
