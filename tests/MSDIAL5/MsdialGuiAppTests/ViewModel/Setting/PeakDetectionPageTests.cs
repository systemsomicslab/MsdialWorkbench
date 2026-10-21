using CompMs.App.Msdial.Model.Setting;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reactive.Bindings;
using System;
using System.Globalization;

namespace CompMs.App.Msdial.ViewModel.Setting.Tests
{
    /// <summary>
    /// The numbers of a peak detection page and the page's error state, which decides whether the run can start. The
    /// LC-MS and GC-MS pages each have one; the GC-MS page calls its minimum peak width the average peak width.
    /// </summary>
    public abstract class PeakDetectionPageTests<TViewModel> where TViewModel : ViewModelBase, ISettingViewModel
    {
        protected abstract TViewModel CreateViewModel(PeakPickBaseParameter parameter, out IPeakDetectionSettingModel model);
        protected abstract bool HasErrors(TViewModel viewModel);
        protected abstract ReactiveProperty<string> GetMinimumAmplitude(TViewModel viewModel);
        protected abstract ReactiveProperty<string> GetMassSliceWidth(TViewModel viewModel);
        protected abstract ReactiveProperty<string> GetSmoothingLevel(TViewModel viewModel);
        protected abstract ReactiveProperty<string> GetPeakWidth(TViewModel viewModel);

        [TestMethod]
        public void ThePageOpensWithoutErrorsForTheDefaults() {
            var viewModel = CreateViewModel(new PeakPickBaseParameter(), out _);
            Assert.IsFalse(HasErrors(viewModel));
        }

        // The page combined its fields' errors with the exclusion list's, which says nothing until the list changes, so
        // until then it reported no error at all and the run started with the last valid values.
        // A peak height and a peak width are whole numbers (Hiroshi Tsugawa, 2026-10-21).
        [DataTestMethod]
        [DataRow("MinimumAmplitude", "abc")]
        [DataRow("MinimumAmplitude", "1500.5")]
        [DataRow("MinimumAmplitude", "-1000")]
        [DataRow("MassSliceWidth", "0")]
        [DataRow("SmoothingLevel", "-1")]
        [DataRow("PeakWidth", "5.5")]
        [DataRow("PeakWidth", "10.5")]
        [DataRow("PeakWidth", "0")]
        public void ATypedValueThePageRefusesIsReportedBeforeTheExclusionListChanges(string field, string text) {
            var viewModel = CreateViewModel(new PeakPickBaseParameter(), out _);
            var valid = GetField(viewModel, field).Value;

            GetField(viewModel, field).Value = text;
            Assert.IsTrue(GetField(viewModel, field).HasErrors);
            Assert.IsTrue(HasErrors(viewModel));

            GetField(viewModel, field).Value = valid;
            Assert.IsFalse(HasErrors(viewModel));
        }

        // A saved value the page refuses blocks the run from the moment the page opens (Hiroshi Tsugawa, 2026-10-21).
        // Neither the page nor the console writes one, but a parameter loaded from elsewhere can carry one.
        [DataTestMethod]
        [DataRow("MinimumAmplitude", 1500.5)]
        [DataRow("MinimumAmplitude", 3e9)] // the int range threw OverflowException, so the page did not open
        [DataRow("MinimumAmplitude", 1e15)]
        [DataRow("MassSliceWidth", 0.00005)]
        [DataRow("SmoothingLevel", -1d)]
        [DataRow("PeakWidth", 5.5)]
        [DataRow("PeakWidth", 10.5)]
        [DataRow("PeakWidth", 0d)]
        public void ASavedValueThePageRefusesIsReportedWhenItOpens(string field, double value) {
            var parameter = new PeakPickBaseParameter();
            Save(parameter, field, value);
            var viewModel = CreateViewModel(parameter, out _);

            Assert.IsTrue(GetField(viewModel, field).HasErrors);
            Assert.IsTrue(HasErrors(viewModel));
        }

        // The height was parsed as a float, so opening the page changed a saved height a float cannot hold.
        [DataTestMethod]
        [DataRow(0d)]
        [DataRow(1000d)]
        [DataRow(10_000_000d)]
        [DataRow(123_456_789d)] // came back as 123456792
        [DataRow(2_147_483_647d)]
        public void ASavedPeakHeightOpensWithoutErrorsAndUnchanged(double height) {
            var parameter = new PeakPickBaseParameter { MinimumAmplitude = height, };
            var viewModel = CreateViewModel(parameter, out var model);

            Assert.AreEqual(height.ToString(CultureInfo.InvariantCulture), GetMinimumAmplitude(viewModel).Value);
            Assert.IsFalse(HasErrors(viewModel));
            model.Commit();
            Assert.AreEqual(height, parameter.MinimumAmplitude);
        }

        // Past int.MaxValue the int ranges threw OverflowException; int.MaxValue itself passed as a height, came back
        // from the float as 2147483648, and threw then.
        [DataTestMethod]
        [DataRow("MinimumAmplitude", "2147483648")]
        [DataRow("MinimumAmplitude", "3000000000")]
        [DataRow("SmoothingLevel", "3000000000")]
        [DataRow("PeakWidth", "3000000000")]
        public void ANumberPastTheLargestIsRefusedWithoutThrowing(string field, string text) {
            var parameter = new PeakPickBaseParameter();
            var viewModel = CreateViewModel(parameter, out var model);

            GetField(viewModel, field).Value = text;
            Assert.IsTrue(GetField(viewModel, field).HasErrors);
            Assert.IsTrue(HasErrors(viewModel));

            model.Commit();
            var defaults = new PeakPickBaseParameter();
            Assert.AreEqual(defaults.MinimumAmplitude, parameter.MinimumAmplitude);
            Assert.AreEqual(defaults.SmoothingLevel, parameter.SmoothingLevel);
            Assert.AreEqual(defaults.MinimumDatapoints, parameter.MinimumDatapoints);
        }

        [TestMethod]
        public void TheLargestPeakHeightIsKeptExactly() {
            var parameter = new PeakPickBaseParameter();
            var viewModel = CreateViewModel(parameter, out var model);

            GetMinimumAmplitude(viewModel).Value = "2147483647";
            Assert.IsFalse(HasErrors(viewModel));
            model.Commit();
            Assert.AreEqual(2147483647d, parameter.MinimumAmplitude);
        }

        private ReactiveProperty<string> GetField(TViewModel viewModel, string field) {
            return field switch
            {
                "MinimumAmplitude" => GetMinimumAmplitude(viewModel),
                "MassSliceWidth" => GetMassSliceWidth(viewModel),
                "SmoothingLevel" => GetSmoothingLevel(viewModel),
                "PeakWidth" => GetPeakWidth(viewModel),
                _ => throw new ArgumentException($"Unknown field {field}.", nameof(field)),
            };
        }

        private static void Save(PeakPickBaseParameter parameter, string field, double value) {
            switch (field) {
                case "MinimumAmplitude": parameter.MinimumAmplitude = value; break;
                case "MassSliceWidth": parameter.MassSliceWidth = (float)value; break;
                case "SmoothingLevel": parameter.SmoothingLevel = (int)value; break;
                case "PeakWidth": parameter.MinimumDatapoints = value; break;
                default: throw new ArgumentException($"Unknown field {field}.", nameof(field));
            }
        }
    }
}
