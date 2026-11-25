using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.Query;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reactive.Bindings;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace CompMs.App.Msdial.ViewModel.Setting.Tests
{
    /// <summary>
    /// The exclusion mass list of a peak detection page: the mass and tolerance typed to add an entry, and the grid of
    /// entries, whose edits must reach the parameter the run reads. The LC-MS and GC-MS pages each have one.
    /// </summary>
    public abstract class ExclusionMassListInputTests<TViewModel> : PeakDetectionPageTests<TViewModel> where TViewModel : ViewModelBase, ISettingViewModel
    {
        protected abstract void SetNewMass(TViewModel viewModel, double mass);
        protected abstract void SetNewTolerance(TViewModel viewModel, double tolerance);
        protected abstract ReactiveCommand GetAddCommand(TViewModel viewModel);
        protected abstract IReadOnlyList<MzSearchQueryViewModel> GetExcludedMassList(TViewModel viewModel);

        [DataTestMethod]
        [DataRow(149.0233)] // phthalate C8H5O3+; the old pattern \d\.?\d* matched "149" and refused the rest
        [DataRow(281.0517)]
        [DataRow(1000d)]
        [DataRow(5.5)]
        public void AnAccurateMassIsAdded(double mass) {
            var parameter = new PeakPickBaseParameter();
            var viewModel = CreateViewModel(parameter, out var model);

            SetNewMass(viewModel, mass);
            SetNewTolerance(viewModel, 0.01);
            Assert.IsFalse(viewModel.ContainsError("NewMass"));
            Assert.IsTrue(GetAddCommand(viewModel).CanExecute());

            GetAddCommand(viewModel).Execute();
            model.Commit();
            Assert.AreEqual(mass, parameter.ExcludedMassList.Single().Mass);
            Assert.AreEqual(0.01, parameter.ExcludedMassList.Single().MassTolerance);
        }

        // A tolerance has no upper bound (Hiroshi Tsugawa, 2026-10-21); the old pattern 0?\.\d+ refused 1 or more.
        [DataTestMethod]
        [DataRow(1d)]
        [DataRow(2.5)]
        [DataRow(10d)]
        public void AToleranceOfOneOrMoreIsAdded(double tolerance) {
            var parameter = new PeakPickBaseParameter();
            var viewModel = CreateViewModel(parameter, out var model);

            SetNewMass(viewModel, 149d);
            SetNewTolerance(viewModel, tolerance);
            Assert.IsFalse(viewModel.ContainsError("NewTolerance"));
            Assert.IsTrue(GetAddCommand(viewModel).CanExecute());

            GetAddCommand(viewModel).Execute();
            model.Commit();
            Assert.AreEqual(tolerance, parameter.ExcludedMassList.Single().MassTolerance);
        }

        [DataTestMethod]
        [DataRow(0d)]
        [DataRow(0.00001)]
        [DataRow(-0.01)]
        [DataRow(double.NaN)]
        [DataRow(double.PositiveInfinity)]
        public void AToleranceBelowTheMinimumOrNotFiniteIsRefused(double tolerance) {
            var viewModel = CreateViewModel(new PeakPickBaseParameter(), out _);

            SetNewMass(viewModel, 149.0233);
            SetNewTolerance(viewModel, 0.5);
            SetNewTolerance(viewModel, tolerance);
            Assert.IsTrue(viewModel.ContainsError("NewTolerance"));
            Assert.IsFalse(GetAddCommand(viewModel).CanExecute());
        }

        [DataTestMethod]
        [DataRow(-149.0233)]
        [DataRow(double.NaN)]
        [DataRow(double.PositiveInfinity)]
        public void AMassBelowZeroOrNotFiniteIsRefused(double mass) {
            var viewModel = CreateViewModel(new PeakPickBaseParameter(), out _);

            SetNewTolerance(viewModel, 0.5);
            SetNewMass(viewModel, mass);
            Assert.IsTrue(viewModel.ContainsError("NewMass"));
            Assert.IsFalse(GetAddCommand(viewModel).CanExecute());
        }

        // The tolerance starts at 0, which was never validated, so an entry that removes nothing could be added.
        [TestMethod]
        public void NothingIsAddedBeforeAToleranceIsTyped() {
            var viewModel = CreateViewModel(new PeakPickBaseParameter(), out _);
            Assert.IsFalse(GetAddCommand(viewModel).CanExecute());

            SetNewMass(viewModel, 149.0233);
            Assert.IsFalse(GetAddCommand(viewModel).CanExecute());
        }

        // The old validation formatted the value with the current culture, so "149,0233" failed a pattern written for a point.
        [DataTestMethod]
        [DataRow("de-DE")]
        [DataRow("fr-FR")]
        [DataRow("ja-JP")]
        public void ValidationDoesNotDependOnTheCulture(string culture) {
            var original = Thread.CurrentThread.CurrentCulture;
            try {
                Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);
                var parameter = new PeakPickBaseParameter();
                parameter.ExcludedMassList.Add(new MzSearchQuery { Mass = 207d, MassTolerance = 0.5, });
                var viewModel = CreateViewModel(parameter, out var model);

                SetNewMass(viewModel, 149.0233);
                SetNewTolerance(viewModel, 0.01);
                Assert.IsFalse(viewModel.ContainsError("NewMass"));
                Assert.IsFalse(viewModel.ContainsError("NewTolerance"));
                Assert.IsTrue(GetAddCommand(viewModel).CanExecute());
                GetAddCommand(viewModel).Execute();

                var row = GetExcludedMassList(viewModel)[0];
                row.Mass = 207.0329;
                row.MassTolerance = 0.005;
                Assert.IsFalse(row.HasValidationErrors);

                model.Commit();
                CollectionAssert.AreEqual(
                    new[] { (207.0329, 0.005), (149.0233, 0.01), },
                    parameter.ExcludedMassList.Select(q => (q.Mass, q.MassTolerance)).ToArray());
            }
            finally {
                Thread.CurrentThread.CurrentCulture = original;
            }
        }

        // Adding an entry makes the page report its errors, and the page's other numbers were formatted and parsed with
        // the current culture: with a decimal comma the mass slice width "0,1" failed its pattern and blocked the run,
        // and "0.05", typed to fix it, passed the pattern and was parsed as 5.
        [DataTestMethod]
        [DataRow("de-DE")]
        [DataRow("en-US")]
        public void AddingAnEntryLeavesThePageWithoutErrorsInAnyCulture(string culture) {
            var original = Thread.CurrentThread.CurrentCulture;
            try {
                Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);
                var parameter = new PeakPickBaseParameter();
                var viewModel = CreateViewModel(parameter, out var model);

                SetNewMass(viewModel, 149.0233);
                SetNewTolerance(viewModel, 0.01);
                GetAddCommand(viewModel).Execute();
                Assert.AreEqual("0.1", GetMassSliceWidth(viewModel).Value);
                Assert.IsFalse(HasErrors(viewModel));

                GetMassSliceWidth(viewModel).Value = "0.05";
                Assert.IsFalse(HasErrors(viewModel));
                model.Commit();
                Assert.AreEqual(0.05f, parameter.MassSliceWidth);
            }
            finally {
                Thread.CurrentThread.CurrentCulture = original;
            }
        }

        [TestMethod]
        public void AnEditInTheGridReachesTheParameterOnCommit() {
            var parameter = new PeakPickBaseParameter();
            parameter.ExcludedMassList.Add(new MzSearchQuery { Mass = 149d, MassTolerance = 0.5, });
            var viewModel = CreateViewModel(parameter, out var model);

            var row = GetExcludedMassList(viewModel).Single();
            row.Mass = 149.0233;
            row.MassTolerance = 0.01;
            // As with the page's other settings, the parameter changes on commit and not before.
            Assert.AreEqual(149d, parameter.ExcludedMassList.Single().Mass);
            Assert.AreEqual(0.5, parameter.ExcludedMassList.Single().MassTolerance);

            model.Commit();
            Assert.AreEqual(149.0233, parameter.ExcludedMassList.Single().Mass);
            Assert.AreEqual(0.01, parameter.ExcludedMassList.Single().MassTolerance);
        }

        [TestMethod]
        public void AnInvalidEditInTheGridIsReportedAndNotCommitted() {
            var parameter = new PeakPickBaseParameter();
            parameter.ExcludedMassList.Add(new MzSearchQuery { Mass = 149.0233, MassTolerance = 0.01, });
            var viewModel = CreateViewModel(parameter, out var model);

            var row = GetExcludedMassList(viewModel).Single();
            row.MassTolerance = -0.01;
            Assert.IsTrue(row.HasValidationErrors);
            Assert.IsTrue(HasErrors(viewModel));
            model.Commit();
            Assert.AreEqual(0.01, parameter.ExcludedMassList.Single().MassTolerance);

            row.MassTolerance = 0.02;
            Assert.IsFalse(row.HasValidationErrors);
            Assert.IsFalse(HasErrors(viewModel));
            model.Commit();
            Assert.AreEqual(0.02, parameter.ExcludedMassList.Single().MassTolerance);
        }
    }
}
