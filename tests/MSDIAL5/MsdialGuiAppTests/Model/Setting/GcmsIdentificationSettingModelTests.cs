using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Enum;
using CompMs.MsdialGcMsApi.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reactive.Bindings.Notifiers;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Model.Setting.Tests
{
    /// <summary>
    /// The RI tolerance offered by the GC-MS identification setting follows the RI compound type.
    /// </summary>
    /// <remarks>
    /// RiTolerance is one field used on two scales of very different size. Kovats units run 100 per
    /// carbon; the Fiehn scale is FAME retention in milliseconds and runs near 39,350 per carbon, a
    /// factor of about 390. The GC-MS default of 20 is a fifth of a Kovats carbon and twenty
    /// milliseconds of FAME retention -- a window nothing falls inside, so selecting FAMEs used to
    /// silently reject every candidate.
    ///
    /// This is the SEARCH WINDOW. RetentionMatchPolicy caps the match VERDICT on the same two scales
    /// and is a different bound; RetentionIndexMatchCapTests covers that side.
    ///
    /// Requested by the author of MS-DIAL, 2026-09-10.
    /// </remarks>
    [TestClass()]
    public class GcmsIdentificationSettingModelTests
    {
        [TestMethod()]
        public void SelectingFamesMovesTheToleranceOntoTheFiehnScale() {
            var model = Create(RiCompoundType.Alkanes, RetentionIndexToleranceDefault.Kovats);

            model.RiDictionarySettingModel.CompoundType = RiCompoundType.Fames;

            Assert.AreEqual(RetentionIndexToleranceDefault.Fiehn, model.SearchParameter.RiTolerance,
                "20 Kovats units carried onto the Fiehn scale is twenty milliseconds and matches nothing");
        }

        [TestMethod()]
        public void SelectingAlkanesAgainBringsItBack() {
            var model = Create(RiCompoundType.Alkanes, RetentionIndexToleranceDefault.Kovats);

            model.RiDictionarySettingModel.CompoundType = RiCompoundType.Fames;
            model.RiDictionarySettingModel.CompoundType = RiCompoundType.Alkanes;

            Assert.AreEqual(RetentionIndexToleranceDefault.Kovats, model.SearchParameter.RiTolerance,
                "and 20000 Fiehn units is just as meaningless as a Kovats window");
        }

        [TestMethod()]
        public void AValueTheAnalystTypedIsNeverReplaced() {
            var model = Create(RiCompoundType.Alkanes, RetentionIndexToleranceDefault.Kovats);
            model.SearchParameter.RiTolerance = 35f; // what the text box does when the analyst types.

            model.RiDictionarySettingModel.CompoundType = RiCompoundType.Fames;

            Assert.AreEqual(35f, model.SearchParameter.RiTolerance);
        }

        [TestMethod()]
        public void AndIsStillNotReplacedAfterTheScaleHasMovedTwice() {
            // The scale the tolerance was chosen on has to be remembered, not re-derived from the
            // selection: after one ignored change, 35 is no more the Fiehn default than the Kovats one.
            var model = Create(RiCompoundType.Alkanes, RetentionIndexToleranceDefault.Kovats);
            model.SearchParameter.RiTolerance = 35f;

            model.RiDictionarySettingModel.CompoundType = RiCompoundType.Fames;
            model.RiDictionarySettingModel.CompoundType = RiCompoundType.Alkanes;

            Assert.AreEqual(35f, model.SearchParameter.RiTolerance);
        }

        [TestMethod()]
        public void AProjectThatAlreadyUsesFamesIsLeftAlone() {
            // Opening the setting is not a change of scale, so nothing is re-seeded on the way in --
            // whatever the project holds is what the analyst last agreed to.
            var model = Create(RiCompoundType.Fames, 12345f);

            Assert.AreEqual(12345f, model.SearchParameter.RiTolerance);
        }

        [TestMethod()]
        public void TheNewValueIsAnnouncedSoTheTextBoxCanFollow() {
            // MsRefSearchParameterBaseViewModel seeds its box once and thereafter only writes into
            // MsRefSearchParameterBase, which is a plain MessagePack object with no change
            // notification. Without this event the analyst would still be reading 20.
            var model = Create(RiCompoundType.Alkanes, RetentionIndexToleranceDefault.Kovats);
            var announced = new List<string>();
            model.PropertyChanged += (_, e) => announced.Add(e.PropertyName);

            model.RiDictionarySettingModel.CompoundType = RiCompoundType.Fames;

            CollectionAssert.Contains(announced, nameof(GcmsIdentificationSettingModel.RiTolerance));
            Assert.AreEqual(RetentionIndexToleranceDefault.Fiehn, model.RiTolerance);
        }

        [TestMethod()]
        public void TheDefaultsThemselves() {
            // Stated once, so that changing either number is a visible decision. 20 Kovats units is a
            // fifth of a carbon; 20000 Fiehn units is about half of one.
            Assert.AreEqual(20f, RetentionIndexToleranceDefault.For(RiCompoundType.Alkanes));
            Assert.AreEqual(20000f, RetentionIndexToleranceDefault.For(RiCompoundType.Fames));
        }

        private static GcmsIdentificationSettingModel Create(RiCompoundType riCompoundType, float riTolerance) {
            var parameter = new MsdialGcmsParameter
            {
                RiCompoundType = riCompoundType,
                RetentionType = RetentionType.RI,
            };
            parameter.RefSpecMatchBaseParam.MspSearchParam.RiTolerance = riTolerance;
            return new GcmsIdentificationSettingModel(
                parameter,
                new AnalysisFileBeanModelCollection(),
                ProcessOption.All,
                new MessageBroker());
        }
    }
}
