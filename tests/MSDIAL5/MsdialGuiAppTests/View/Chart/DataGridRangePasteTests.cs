using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace CompMs.App.Msdial.View.Chart.Tests;

[TestClass]
public class DataGridRangePasteTests
{
    private static string Show(string text) => string.Join(" / ", DataGridRangePaste.Parse(text).Select(row => "[" + string.Join("|", row) + "]"));

    [TestMethod]
    [DataRow("", "")]
    [DataRow("100", "[100]")]
    [DataRow("100\r\n", "[100]")]
    [DataRow("100\t0.01\r\n101\t0.02\r\n", "[100|0.01] / [101|0.02]")]
    [DataRow("100\t0.01\n101\t0.02", "[100|0.01] / [101|0.02]")]
    [DataRow("a\t\tc\r\n", "[a||c]")]
    [DataRow("\r\n", "[]")]
    [DataRow("x\r\n\r\ny\r\n", "[x] / [] / [y]")]
    public void SplitsRowsAndCells(string text, string expected) {
        Assert.AreEqual(expected, Show(text));
    }

    [TestMethod]
    public void ReadsCellsExcelPutsInQuotes() {
        Assert.AreEqual("[a\tb|c] / [line 1\nline 2|say \"hi\"]", Show("\"a\tb\"\tc\r\n\"line 1\nline 2\"\t\"say \"\"hi\"\"\"\r\n"));
        Assert.AreEqual("[\"open]", Show("\"open"), "A quote that never closes is part of the value.");
        Assert.AreEqual("[5\"]", Show("5\""));
    }
}
