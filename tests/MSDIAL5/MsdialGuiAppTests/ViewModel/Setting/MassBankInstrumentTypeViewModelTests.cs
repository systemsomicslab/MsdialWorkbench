using CompMs.Common.Enum;
using CompMs.Common.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reactive.Bindings;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Setting.Tests
{
    /// <summary>
    /// The separation, ionisation and analyzer choices that compose AC$INSTRUMENT_TYPE, as the project wizard uses
    /// them: an optional helper that fills the free-text instrument type.
    /// </summary>
    [TestClass]
    public class MassBankInstrumentTypeViewModelTests
    {
        [TestMethod]
        public void TheChoicesAreMassBanksVocabulary() {
            using var composer = new MassBankInstrumentTypeViewModel(new ReactivePropertySlim<string>(string.Empty), Observable.Return(false));

            CollectionAssert.AreEqual(new[] { string.Empty, "LC", "GC", "CE", }, composer.Separations.Select(s => s.Value).ToArray());
            Assert.AreEqual("(none)", composer.Separations[0].Label);
            CollectionAssert.AreEqual(MassBankVocabulary.Ionisations.ToArray(), composer.Ionisations.ToArray());
            CollectionAssert.AreEqual(MassBankVocabulary.AnalyzerPresets.ToArray(), composer.Analyzers.ToArray());
        }

        [TestMethod]
        public void ChoosingFillsTheInstrumentType() {
            var instrumentType = new ReactivePropertySlim<string>(string.Empty);
            using var composer = new MassBankInstrumentTypeViewModel(instrumentType, Observable.Return(false));

            Assert.IsFalse(composer.ObserveHasErrors.Value, "nothing is required in the wizard");
            composer.Ionisation.Value = "ESI";
            Assert.AreEqual(string.Empty, instrumentType.Value, "nothing is written before the analyzer is chosen");
            composer.Analyzer.Value = composer.Analyzers.Single(p => p.Analyzer == "QTOF");
            Assert.AreEqual("ESI-QTOF", instrumentType.Value);
            composer.Separation.Value = "LC";
            Assert.AreEqual("LC-ESI-QTOF", instrumentType.Value);
            composer.Separation.Value = string.Empty;
            Assert.AreEqual("ESI-QTOF", instrumentType.Value);
            Assert.IsTrue(composer.IsValid.Value);
            Assert.IsFalse(composer.ObserveHasErrors.Value);
        }

        [TestMethod]
        public void TypingAValidValueSelectsTheChoicesAndAnyOtherClearsThem() {
            var instrumentType = new ReactivePropertySlim<string>("LC-ESI-QFT");
            using var composer = new MassBankInstrumentTypeViewModel(instrumentType, Observable.Return(false));

            Assert.AreEqual("LC", composer.Separation.Value);
            Assert.AreEqual("ESI", composer.Ionisation.Value);
            Assert.AreEqual("QFT", composer.Analyzer.Value?.Analyzer);

            instrumentType.Value = "LC-MS/MS";
            Assert.AreEqual("LC-MS/MS", instrumentType.Value, "the typed text is left as it is");
            Assert.AreEqual(string.Empty, composer.Separation.Value);
            Assert.IsNull(composer.Ionisation.Value);
            Assert.IsNull(composer.Analyzer.Value);
            Assert.IsFalse(composer.IsValid.Value);
            Assert.IsFalse(composer.ObserveHasErrors.Value, "not required: an invalid text is not an error in the wizard");

            instrumentType.Value = "GC-EI-TOF";
            Assert.AreEqual("GC", composer.Separation.Value);
            Assert.AreEqual("EI", composer.Ionisation.Value);
            Assert.AreEqual("TOF", composer.Analyzer.Value?.Analyzer);
            Assert.AreEqual("GC-EI-TOF", instrumentType.Value);
        }

        [TestMethod]
        public void AValidAnalyzerOutsideThePresetsIsKept() {
            var instrumentType = new ReactivePropertySlim<string>("LC-ESI-QQTOF");
            using var composer = new MassBankInstrumentTypeViewModel(instrumentType, Observable.Return(false));

            Assert.AreEqual("QQTOF", composer.Analyzer.Value?.Analyzer);
            Assert.AreEqual(MassBankInstrumentTypeViewModel.AsEntered, composer.Analyzer.Value?.Description);
            Assert.AreEqual("LC-ESI-QQTOF", instrumentType.Value);
            Assert.AreEqual(MassBankVocabulary.AnalyzerPresets.Count + 1, composer.Analyzers.Count);

            instrumentType.Value = "EI-EB";
            Assert.AreEqual("EB", composer.Analyzer.Value?.Analyzer);
            Assert.IsFalse(composer.Analyzers.Any(p => p.Analyzer == "QQTOF"), "one entered analyzer at a time");
            Assert.AreEqual(MassBankVocabulary.AnalyzerPresets.Count + 1, composer.Analyzers.Count);
        }

        [TestMethod]
        public void TheEnteredAnalyzerLeavesTheListWithItsValue() {
            var instrumentType = new ReactivePropertySlim<string>("LC-ESI-QQTOF");
            using var composer = new MassBankInstrumentTypeViewModel(instrumentType, Observable.Return(false));
            Assert.AreEqual(MassBankVocabulary.AnalyzerPresets.Count + 1, composer.Analyzers.Count);

            composer.Analyzer.Value = composer.Analyzers.Single(p => p.Analyzer == "QTOF");
            Assert.AreEqual("LC-ESI-QTOF", instrumentType.Value);
            CollectionAssert.AreEqual(MassBankVocabulary.AnalyzerPresets.ToArray(), composer.Analyzers.ToArray(), "a preset was chosen");

            instrumentType.Value = "LC-ESI-QQTOF";
            Assert.AreEqual(MassBankInstrumentTypeViewModel.AsEntered, composer.Analyzer.Value?.Description);
            instrumentType.Value = "LC-MS/MS";
            Assert.IsNull(composer.Analyzer.Value);
            CollectionAssert.AreEqual(MassBankVocabulary.AnalyzerPresets.ToArray(), composer.Analyzers.ToArray(), "the value no longer parses");
        }

        [TestMethod]
        public void AValueThatDoesNotParseStartsFromTheDefaultSeparation() {
            var instrumentType = new ReactivePropertySlim<string>("LC-MS/MS");
            using var composer = new MassBankInstrumentTypeViewModel(instrumentType, Observable.Return(true), "LC");

            Assert.AreEqual("LC", composer.Separation.Value);
            Assert.AreEqual("LC-MS/MS", instrumentType.Value, "a preselected separation writes nothing");
            composer.Ionisation.Value = "ESI";
            composer.Analyzer.Value = composer.Analyzers.Single(p => p.Analyzer == "QTOF");
            Assert.AreEqual("LC-ESI-QTOF", instrumentType.Value);

            instrumentType.Value = "ESI-QTOF";
            Assert.AreEqual(string.Empty, composer.Separation.Value, "a value that parses keeps its own separation");

            Assert.AreEqual("LC", MassBankInstrumentTypeViewModel.SeparationFor(MachineCategory.LCMS));
            Assert.AreEqual("LC", MassBankInstrumentTypeViewModel.SeparationFor(MachineCategory.LCIMMS));
            Assert.AreEqual("GC", MassBankInstrumentTypeViewModel.SeparationFor(MachineCategory.GCMS));
            Assert.AreEqual(string.Empty, MassBankInstrumentTypeViewModel.SeparationFor(MachineCategory.IFMS));
            Assert.AreEqual(string.Empty, MassBankInstrumentTypeViewModel.SeparationFor(MachineCategory.IMMS));
            Assert.ThrowsException<System.ArgumentException>(() => new MassBankInstrumentTypeViewModel(instrumentType, Observable.Return(false), "HPLC"));
        }

        [TestMethod]
        public void ResynchronizeSelectsTheChoicesOfTheValueStillHeld() {
            var instrumentType = new ReactivePropertySlim<string>("LC-MS/MS");
            using var composer = new MassBankInstrumentTypeViewModel(instrumentType, Observable.Return(false), "LC");

            composer.Separation.Value = "GC";
            composer.Ionisation.Value = "EI";
            Assert.AreEqual("LC-MS/MS", instrumentType.Value, "nothing is written before the analyzer is chosen");
            instrumentType.Value = "LC-MS/MS";
            Assert.AreEqual("GC", composer.Separation.Value, "setting the value it already holds raises no change");

            composer.Resynchronize();

            Assert.AreEqual("LC", composer.Separation.Value);
            Assert.IsNull(composer.Ionisation.Value);
            Assert.IsNull(composer.Analyzer.Value);
            Assert.AreEqual("LC-MS/MS", instrumentType.Value, "selecting the choices again writes nothing");

            // A valid value selects the same choices again, without a second "as entered" analyzer.
            instrumentType.Value = "LC-ESI-QQTOF";
            composer.Resynchronize();
            Assert.AreEqual("LC", composer.Separation.Value);
            Assert.AreEqual("ESI", composer.Ionisation.Value);
            Assert.AreEqual(MassBankInstrumentTypeViewModel.AsEntered, composer.Analyzer.Value?.Description);
            Assert.AreEqual(MassBankVocabulary.AnalyzerPresets.Count + 1, composer.Analyzers.Count, "one entered analyzer at a time");
            Assert.AreEqual("LC-ESI-QQTOF", instrumentType.Value);
        }

        [TestMethod]
        public void MissingChoicesAreErrorsOnlyWhenRequired() {
            var required = new ReactivePropertySlim<bool>(false);
            var instrumentType = new ReactivePropertySlim<string>("LC-MS/MS");
            using var composer = new MassBankInstrumentTypeViewModel(instrumentType, required);

            Assert.IsFalse(composer.ObserveHasErrors.Value);
            required.Value = true;
            Assert.IsTrue(composer.Ionisation.HasErrors);
            Assert.IsTrue(composer.Analyzer.HasErrors);
            Assert.IsTrue(composer.ObserveHasErrors.Value);
            StringAssert.Contains(composer.Preview.Value, "\"LC-MS/MS\" is not a MassBank instrument type");

            composer.Ionisation.Value = "APCI";
            composer.Analyzer.Value = composer.Analyzers.Single(p => p.Analyzer == "QQ");
            Assert.IsFalse(composer.ObserveHasErrors.Value);
            Assert.AreEqual("APCI-QQ", instrumentType.Value);
            Assert.AreEqual("AC$INSTRUMENT_TYPE: APCI-QQ", composer.Preview.Value);

            Assert.IsNull(MassBankInstrumentTypeViewModel.Validate("LC-ESI-QTOF"));
            Assert.IsNotNull(MassBankInstrumentTypeViewModel.Validate(string.Empty));
            Assert.IsNotNull(MassBankInstrumentTypeViewModel.Validate("lc-esi-qtof"), "the grammar is case-sensitive");
        }
    }
}
