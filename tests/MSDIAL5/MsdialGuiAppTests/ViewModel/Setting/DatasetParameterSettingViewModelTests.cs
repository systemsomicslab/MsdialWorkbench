using CompMs.App.Msdial.Model.Setting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reactive.Bindings.Notifiers;
using System;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Setting.Tests
{
    /// <summary>
    /// The measurement page of the project wizard: the MassBank-style choices only help to fill the instrument type,
    /// which stays free text.
    /// </summary>
    [TestClass]
    public class DatasetParameterSettingViewModelTests
    {
        [TestMethod]
        public void TheMassBankChoicesFillTheInstrumentTypeWithoutConstrainingIt() {
            var date = new DateTime(2026, 11, 6);
            var model = new DatasetParameterSettingModel(date, new DatasetFileSettingModel(date), _ => { }, new MessageBroker());
            using var vm = new DatasetParameterSettingViewModel(model, Observable.Return(true));
            var composer = vm.InstrumentTypeComposer;

            Assert.AreEqual(string.Empty, model.InstrumentType);
            Assert.IsFalse(composer.IsRequired.Value);
            Assert.IsFalse(vm.ObserveHasErrors.Value);

            composer.Separation.Value = "LC";
            composer.Ionisation.Value = "ESI";
            composer.Analyzer.Value = composer.Analyzers.Single(p => p.Analyzer == "ITFT");
            Assert.AreEqual("LC-ESI-ITFT", vm.InstrumentType.Value);
            Assert.AreEqual("LC-ESI-ITFT", model.InstrumentType);

            vm.InstrumentType.Value = "Orbitrap Fusion Lumos";
            Assert.AreEqual("Orbitrap Fusion Lumos", model.InstrumentType, "free text is still accepted");
            Assert.IsNull(composer.Analyzer.Value);
            Assert.IsFalse(vm.ObserveHasErrors.Value, "and is not an error");
        }
    }
}
