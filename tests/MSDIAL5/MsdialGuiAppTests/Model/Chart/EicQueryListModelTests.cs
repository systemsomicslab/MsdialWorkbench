using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace CompMs.App.Msdial.Model.Chart.Tests;

[TestClass]
public class EicQueryListModelTests {
    [TestMethod]
    public void StartsWithTheSavedRowsAndOneEmptyRow() {
        var list = new EicQueryListModel([
            new PeakFeatureSearchValue { Title = "a", Mass = 100d, MassTolerance = 0.005d },
            new PeakFeatureSearchValue { Mass = 0d, MassTolerance = 0d },
        ]);

        Assert.AreEqual(2, list.Queries.Count);
        Assert.AreEqual(100d, list.Queries[0].Mass);
        Assert.IsTrue(list.Queries[1].IsEmpty);
        Assert.AreEqual(0.005d, list.Queries[1].MassTolerance, "A new row takes the tolerance of the row above.");
    }

    [TestMethod]
    public void NoSavedRowsGivesOneEmptyRowWithTheDefaultTolerance() {
        var list = new EicQueryListModel(null);

        Assert.AreEqual(1, list.Queries.Count);
        Assert.AreEqual(EicQueryModel.DefaultMassTolerance, list.Queries[0].MassTolerance);
    }

    [TestMethod]
    public void TypingInTheLastRowAddsAnEmptyRowAndClearingItRemovesIt() {
        var list = new EicQueryListModel(null);

        list.Queries[0].Mass = 200d;
        Assert.AreEqual(2, list.Queries.Count);

        list.Queries[0].Mass = null;
        Assert.AreEqual(1, list.Queries.Count);
    }

    [TestMethod]
    public void RemoveAndClearKeepOneEmptyRow() {
        var list = new EicQueryListModel([
            new PeakFeatureSearchValue { Mass = 100d, MassTolerance = 0.01d },
            new PeakFeatureSearchValue { Mass = 200d, MassTolerance = 0.01d },
        ]);

        list.Remove(list.Queries[0]);
        CollectionAssert.AreEqual(new double?[] { 200d, null }, list.Queries.Select(q => q.Mass).ToArray());

        list.Clear();
        Assert.AreEqual(1, list.Queries.Count);
        Assert.IsTrue(list.Queries[0].IsEmpty);
    }

    [TestMethod]
    public void OnlyCompleteRowsAreDrawn() {
        var list = new EicQueryListModel(null);
        list.Queries[0].Title = "named only";
        list.Queries[1].Mass = 300d;

        var values = list.ToSearchValues();

        Assert.AreEqual(1, values.Count);
        Assert.AreEqual(300d, values[0].Mass);
    }

    [TestMethod]
    public void PastingExcelRowsAddsRowsAndSkipsTheHeader() {
        var list = new EicQueryListModel(null);

        var failed = list.Paste(0, 0, "Name\tExact mass\tMass tolerance [Da]\r\nA\t100.5\t0.005\r\n\r\nB\t200.25\t0.01\r\nC\tabc\t0.01\r\n");

        CollectionAssert.AreEqual(new[] { 5 }, failed);
        Assert.AreEqual(3, list.Queries.Count);
        Assert.AreEqual("A", list.Queries[0].Title);
        Assert.AreEqual(100.5d, list.Queries[0].Mass);
        Assert.AreEqual(0.005d, list.Queries[0].MassTolerance);
        Assert.AreEqual("B", list.Queries[1].Title);
        Assert.AreEqual(200.25d, list.Queries[1].Mass);
        Assert.IsTrue(list.Queries[2].IsEmpty);
    }

    [TestMethod]
    public void PastingASingleColumnOfMassesKeepsTheTolerance() {
        var list = new EicQueryListModel(null);

        list.Paste(0, 1, "100\n200\n300");

        CollectionAssert.AreEqual(new double?[] { 100d, 200d, 300d, null }, list.Queries.Select(q => q.Mass).ToArray());
        Assert.IsTrue(list.Queries.All(q => q.MassTolerance == EicQueryModel.DefaultMassTolerance));
    }

    [TestMethod]
    public void CopiedTableCanBePastedBack() {
        var source = new EicQueryListModel([
            new PeakFeatureSearchValue { Title = "x", Mass = 123.4567d, MassTolerance = 0.002d },
        ]);
        var target = new EicQueryListModel(null);

        target.Paste(0, 0, source.ToTableText());

        Assert.AreEqual("x", target.Queries[0].Title);
        Assert.AreEqual(123.4567d, target.Queries[0].Mass);
        Assert.AreEqual(0.002d, target.Queries[0].MassTolerance);
    }

    [TestMethod]
    public void RowViewModelValidatesAndWritesThrough() {
        var list = new EicQueryListModel(null);
        using var row = new EicQueryViewModel(list.Queries[0]);
        Assert.IsFalse(row.HasErrors.Value, "An empty row is not an error.");
        Assert.AreEqual("", row.Mass.Value, "An empty row shows no 0.");

        row.Mass.Value = "abc";
        Assert.IsTrue(row.HasErrors.Value);
        Assert.IsNull(list.Queries[0].Mass);

        row.Mass.Value = "150.1";
        Assert.IsFalse(row.HasErrors.Value);
        Assert.AreEqual(150.1d, list.Queries[0].Mass);

        row.MassTolerance.Value = "";
        Assert.IsTrue(row.HasErrors.Value, "An m/z needs a tolerance.");

        row.MassTolerance.Value = "0.02";
        Assert.IsFalse(row.HasErrors.Value);

        list.Queries[0].Mass = 99d;
        Assert.AreEqual("99", row.Mass.Value, "Pasted values show in the text box.");
    }

    [TestMethod]
    public void ANameWithoutAnMzIsAnError() {
        var list = new EicQueryListModel(null);
        using var row = new EicQueryViewModel(list.Queries[0]);

        row.Title.Value = "only a name";

        Assert.IsTrue(row.HasErrors.Value);
    }
}
