using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Globalization;
using System.Threading;

namespace CompMs.App.Msdial.ViewModel.Chart.Tests;

[TestClass]
public class EicSettingTests {
    [TestMethod]
    public void ClearingARowResetsItsTextToTheDefaults() {
        var model = new PeakFeatureSearchValueModel(new PeakFeatureSearchValue { Title = "a", Mass = 100d, MassTolerance = 0.005d });
        using var row = new PeakFeatureSearchValueViewModel(model);
        row.Mass.Value = "abc";
        Assert.IsTrue(row.HasErrors.Value);

        CheckChromatogramsViewModel.ClearEicQuery(row);

        Assert.AreEqual(string.Empty, row.Title.Value);
        Assert.AreEqual("0", row.Mass.Value);
        Assert.AreEqual("0.01", row.MassTolerance.Value);
        Assert.IsFalse(row.HasErrors.Value, "Clearing removes a value that could not be read.");
        Assert.AreEqual(100d, model.Mass, "The cleared row is saved only on Apply.");

        row.Commit();
        Assert.AreEqual(0d, model.Mass);
        Assert.AreEqual(CheckChromatogramsModel.DefaultEicMassTolerance, model.MassTolerance);
    }

    [TestMethod]
    public void DeleteClearsOnlyTheSelectedValue() {
        var model = new PeakFeatureSearchValueModel(new PeakFeatureSearchValue { Title = "a", Mass = 100d, MassTolerance = 0.005d });
        using var row = new PeakFeatureSearchValueViewModel(model);

        CheckChromatogramsViewModel.ClearEicQueryValue(row, "Mass");
        Assert.AreEqual("a", row.Title.Value);
        Assert.AreEqual("0", row.Mass.Value);
        Assert.AreEqual("0.005", row.MassTolerance.Value);

        CheckChromatogramsViewModel.ClearEicQueryValue(row, "Title");
        CheckChromatogramsViewModel.ClearEicQueryValue(row, "MassTolerance");
        Assert.AreEqual(string.Empty, row.Title.Value);
        Assert.AreEqual("0.01", row.MassTolerance.Value);
        Assert.IsFalse(row.HasErrors.Value);
    }

    [TestMethod]
    public void NumbersUseAPeriodWhateverTheCulture() {
        var culture = Thread.CurrentThread.CurrentCulture;
        try {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
            var model = new PeakFeatureSearchValueModel(new PeakFeatureSearchValue { Mass = 100.5d, MassTolerance = CheckChromatogramsModel.DefaultEicMassTolerance });
            using var row = new PeakFeatureSearchValueViewModel(model);

            Assert.AreEqual("100.5", row.Mass.Value);
            Assert.AreEqual("0.01", row.MassTolerance.Value);
            Assert.IsFalse(row.HasErrors.Value, "The default rows are valid with a comma decimal separator too.");

            row.Mass.Value = "200.25";
            row.Commit();
            Assert.AreEqual(200.25d, model.Mass);
        }
        finally {
            Thread.CurrentThread.CurrentCulture = culture;
        }
    }
}
