using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Parser;
using CompMs.MsdialCore.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace CompMs.App.Msdial.ViewModel.Setting.Tests
{
    /// <summary>
    /// Option &gt; Project property setting: free text as before while "Constrain to MassBank format" is off; with it
    /// on, only values that make valid MassBank records, and Finish disabled until they are.
    /// </summary>
    [TestClass]
    public class ProjectPropertySettingViewModelTests
    {
        // What an older project typically holds: the old tooltip examples, which MassBank's Validator rejects.
        private static ProjectBaseParameter FreeTextProject() {
            return new ProjectBaseParameter
            {
                InstrumentType = "LC-MS/MS",
                Instrument = string.Empty,
                Authors = string.Empty,
                License = "CC-BY",
                CollisionEnergy = "35 +/- 15 V",
                Comment = "an older project",
            };
        }

        private static ProjectBaseParameter MassBankProject() {
            return new ProjectBaseParameter
            {
                InstrumentType = "LC-ESI-QFT",
                Instrument = "Q Exactive Plus, Thermo Scientific",
                Authors = "MS-DIAL test",
                License = "CC BY-SA",
                CollisionEnergy = "NCE 30",
                ConstrainToMassBankFormat = true,
                MassBankContributorId = "MSDIAL_TEST",
            };
        }

        private static MassBankAnalyzerPreset Preset(MassBankInstrumentTypeViewModel composer, string analyzer) {
            return composer.Analyzers.Single(p => p.Analyzer == analyzer);
        }

        [TestMethod]
        public void UnconstrainedFieldsStayFreeText() {
            var parameter = FreeTextProject();
            using var vm = new ProjectPropertySettingViewModel(new StudyContextModel(parameter));

            Assert.IsFalse(vm.ConstrainToMassBankFormat.Value);
            Assert.IsFalse(vm.Authors.HasErrors, "the inert [Required] attributes never applied, and still do not");
            Assert.IsFalse(vm.Instrument.HasErrors);
            Assert.IsFalse(vm.InstrumentType.HasErrors);
            Assert.IsFalse(vm.SelectedLicense.HasErrors);
            Assert.IsFalse(vm.InstrumentTypeComposer.ObserveHasErrors.Value);
            Assert.IsTrue(vm.FinishCommand.CanExecute(null));

            vm.InstrumentType.Value = "anything at all";
            vm.FinishCommand.Execute(null);

            Assert.AreEqual("anything at all", parameter.InstrumentType);
            Assert.AreEqual("CC-BY", parameter.License, "a free-text licence is kept as it is");
            Assert.IsFalse(parameter.ConstrainToMassBankFormat);
        }

        [TestMethod]
        public void ConstrainingShowsWhatMassBankNeedsAndKeepsTheStoredValues() {
            var parameter = FreeTextProject();
            using var vm = new ProjectPropertySettingViewModel(new StudyContextModel(parameter));

            vm.ConstrainToMassBankFormat.Value = true;

            Assert.IsTrue(vm.Authors.HasErrors);
            Assert.AreEqual(ProjectPropertySettingViewModel.AuthorsRequired, vm.Authors.GetErrors(null).Cast<string>().Single());
            Assert.IsTrue(vm.Instrument.HasErrors);
            Assert.IsTrue(vm.SelectedLicense.HasErrors);
            StringAssert.Contains(vm.SelectedLicense.GetErrors(null).Cast<string>().Single(), "\"CC-BY\"");
            Assert.IsTrue(vm.InstrumentType.HasErrors);
            StringAssert.Contains(vm.InstrumentType.GetErrors(null).Cast<string>().Single(), "\"LC-MS/MS\" is not a MassBank instrument type");
            Assert.IsTrue(vm.InstrumentTypeComposer.Ionisation.HasErrors);
            Assert.IsTrue(vm.InstrumentTypeComposer.Analyzer.HasErrors);
            Assert.IsFalse(vm.InstrumentTypeComposer.IsValid.Value);
            StringAssert.Contains(vm.InstrumentTypeComposer.Preview.Value, "\"LC-MS/MS\"");
            Assert.IsFalse(vm.CollisionEnergy.HasErrors);
            Assert.IsFalse(vm.FinishCommand.CanExecute(null));

            // Nothing is mapped to a guess: the stored values stay until they are replaced.
            Assert.AreEqual("LC-MS/MS", vm.InstrumentType.Value);
            Assert.IsNull(vm.InstrumentTypeComposer.Ionisation.Value);
            Assert.IsNull(vm.InstrumentTypeComposer.Analyzer.Value);
            Assert.AreEqual("CC-BY", vm.License.Value);
            Assert.IsNull(vm.SelectedLicense.Value);
        }

        [TestMethod]
        public void FinishIsEnabledOnceEveryConstrainedFieldIsValid() {
            var parameter = FreeTextProject();
            using var vm = new ProjectPropertySettingViewModel(new StudyContextModel(parameter));
            vm.ConstrainToMassBankFormat.Value = true;
            var composer = vm.InstrumentTypeComposer;

            composer.Separation.Value = "LC";
            composer.Ionisation.Value = "ESI";
            Assert.AreEqual("LC-MS/MS", vm.InstrumentType.Value, "nothing is written before the analyzer is chosen");
            composer.Analyzer.Value = Preset(composer, "QTOF");
            Assert.AreEqual("LC-ESI-QTOF", vm.InstrumentType.Value);
            Assert.IsFalse(vm.InstrumentType.HasErrors);
            Assert.AreEqual("AC$INSTRUMENT_TYPE: LC-ESI-QTOF", composer.Preview.Value);

            vm.SelectedLicense.Value = "CC BY";
            Assert.AreEqual("CC BY", vm.License.Value);
            Assert.IsFalse(vm.SelectedLicense.HasErrors);
            vm.Authors.Value = "MS-DIAL test";
            Assert.IsFalse(vm.FinishCommand.CanExecute(null), "the instrument is still missing");
            vm.Instrument.Value = "TripleTOF 6600, SCIEX";
            Assert.IsTrue(vm.FinishCommand.CanExecute(null));

            vm.FinishCommand.Execute(null);

            Assert.IsTrue(parameter.ConstrainToMassBankFormat);
            Assert.AreEqual("LC-ESI-QTOF", parameter.InstrumentType);
            Assert.AreEqual("CC BY", parameter.License);
            Assert.AreEqual("MS-DIAL test", parameter.Authors);
            Assert.AreEqual("TripleTOF 6600, SCIEX", parameter.Instrument);
            Assert.IsTrue(MassBankVocabulary.IsValidInstrumentType(parameter.InstrumentType));
            Assert.IsTrue(MassBankVocabulary.IsValidLicense(parameter.License));
        }

        [TestMethod]
        public void AStoredMassBankProjectSelectsItsChoices() {
            using var vm = new ProjectPropertySettingViewModel(new StudyContextModel(MassBankProject()));
            var composer = vm.InstrumentTypeComposer;

            Assert.IsTrue(vm.ConstrainToMassBankFormat.Value);
            Assert.AreEqual("LC", composer.Separation.Value);
            Assert.AreEqual("ESI", composer.Ionisation.Value);
            Assert.AreEqual("QFT", composer.Analyzer.Value?.Analyzer);
            Assert.AreEqual("CC BY-SA", vm.SelectedLicense.Value);
            Assert.AreEqual("MSDIAL_TEST", vm.MassBankContributorId.Value);
            Assert.IsTrue(vm.FinishCommand.CanExecute(null));
        }

        [TestMethod]
        public void TheCollisionEnergyMustFitOnOneLineWhenConstrained() {
            using var vm = new ProjectPropertySettingViewModel(new StudyContextModel(MassBankProject()));

            vm.CollisionEnergy.Value = string.Empty;
            Assert.IsFalse(vm.CollisionEnergy.HasErrors, "an empty collision energy is left out of the record");
            vm.CollisionEnergy.Value = "20 V\n40 V";
            Assert.IsTrue(vm.CollisionEnergy.HasErrors);
            Assert.IsFalse(vm.FinishCommand.CanExecute(null));
            vm.CollisionEnergy.Value = " 35 V";
            Assert.IsTrue(vm.CollisionEnergy.HasErrors);
            vm.CollisionEnergy.Value = "Ramp 20-40 V";
            Assert.IsFalse(vm.CollisionEnergy.HasErrors);
            Assert.IsTrue(vm.FinishCommand.CanExecute(null));

            vm.ConstrainToMassBankFormat.Value = false;
            vm.CollisionEnergy.Value = "20 V\n40 V";
            Assert.IsFalse(vm.CollisionEnergy.HasErrors, "free text again once the constraint is off");
        }

        [TestMethod]
        public void TheContributorIdIsEmptyOrInMassBanksFormat() {
            using var vm = new ProjectPropertySettingViewModel(new StudyContextModel(FreeTextProject()));

            Assert.AreEqual(string.Empty, vm.MassBankContributorId.Value);
            Assert.IsFalse(vm.MassBankContributorId.HasErrors, "empty until MassBank has assigned one");
            vm.MassBankContributorId.Value = "riken-ims";
            Assert.IsTrue(vm.MassBankContributorId.HasErrors, "checked even when the project is not constrained: it is only used for MassBank");
            Assert.AreEqual(ProjectPropertySettingViewModel.ContributorIdInvalid, vm.MassBankContributorId.GetErrors(null).Cast<string>().Single());
            Assert.IsFalse(vm.FinishCommand.CanExecute(null));
            vm.MassBankContributorId.Value = "RIKEN_IMS";
            Assert.IsFalse(vm.MassBankContributorId.HasErrors);
            Assert.IsTrue(vm.FinishCommand.CanExecute(null));
        }

        [TestMethod]
        public void TurningTheConstraintOffAllowsFreeTextAgain() {
            using var vm = new ProjectPropertySettingViewModel(new StudyContextModel(FreeTextProject()));

            vm.ConstrainToMassBankFormat.Value = true;
            Assert.IsFalse(vm.FinishCommand.CanExecute(null));
            vm.ConstrainToMassBankFormat.Value = false;

            Assert.IsFalse(vm.Authors.HasErrors);
            Assert.IsFalse(vm.Instrument.HasErrors);
            Assert.IsFalse(vm.InstrumentType.HasErrors);
            Assert.IsFalse(vm.SelectedLicense.HasErrors);
            Assert.IsFalse(vm.InstrumentTypeComposer.ObserveHasErrors.Value);
            Assert.IsTrue(vm.FinishCommand.CanExecute(null));
        }

        [TestMethod]
        public void CancelRestoresTheSavedValuesAndChoices() {
            var parameter = MassBankProject();
            using var vm = new ProjectPropertySettingViewModel(new StudyContextModel(parameter));
            var composer = vm.InstrumentTypeComposer;

            composer.Separation.Value = "GC";
            composer.Ionisation.Value = "EI";
            composer.Analyzer.Value = Preset(composer, "TOF");
            vm.SelectedLicense.Value = "CC0";
            vm.MassBankContributorId.Value = "OTHER";
            vm.ConstrainToMassBankFormat.Value = false;
            Assert.AreEqual("GC-EI-TOF", vm.InstrumentType.Value);

            vm.CancelCommand.Execute(null);

            Assert.AreEqual("LC-ESI-QFT", parameter.InstrumentType, "nothing reaches the project without Finish");
            Assert.AreEqual("LC-ESI-QFT", vm.InstrumentType.Value);
            Assert.AreEqual("LC", composer.Separation.Value);
            Assert.AreEqual("ESI", composer.Ionisation.Value);
            Assert.AreEqual("QFT", composer.Analyzer.Value?.Analyzer);
            Assert.AreEqual("CC BY-SA", vm.SelectedLicense.Value);
            Assert.AreEqual("MSDIAL_TEST", vm.MassBankContributorId.Value);
            Assert.IsTrue(vm.ConstrainToMassBankFormat.Value);
            Assert.IsTrue(vm.FinishCommand.CanExecute(null));
        }
    }
}
