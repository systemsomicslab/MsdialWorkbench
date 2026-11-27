using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.Graphics.AxisManager.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Chart.Tests;

[TestClass]
public class ChromatogramsModelVisibilityTests {
    private static DisplayChromatogram Create(string name, double height) {
        var chromatogram = new Chromatogram(new[] {
            new ValuePeak(0, 1d, 100d, 0d),
            new ValuePeak(1, 2d, 100d, height),
            new ValuePeak(2, 3d, 100d, 0d),
        }, ChromXType.RT, ChromXUnit.Min);
        return new DisplayChromatogram(chromatogram, name: name);
    }

    private static double AbundanceMax(ChromatogramsModel model) {
        var axis = (ContinuousAxisManager<double>)model.AbundanceAxisItemSelector.SelectedAxisItem!.AxisManager;
        return axis.InitialRange.Maximum.Value;
    }

    [TestMethod]
    public void HidingTheTallestChromatogramRescalesTheAbundanceAxis() {
        var small = Create("small", 100d);
        var large = Create("large", 10000d);
        using var model = new ChromatogramsModel("test", new List<DisplayChromatogram> { small, large }, "", "RT", "Abundance");
        Assert.AreEqual(10000d, AbundanceMax(model), 1e-6);

        large.Visible = false;
        Assert.AreEqual(100d, AbundanceMax(model), 1e-6);

        large.Visible = true;
        Assert.AreEqual(10000d, AbundanceMax(model), 1e-6);
    }

    [TestMethod]
    public void SetAllVisibleTogglesEveryChromatogram() {
        var a = Create("a", 100d);
        var b = Create("b", 200d);
        using var model = new ChromatogramsModel("test", new List<DisplayChromatogram> { a, b }, "", "RT", "Abundance");

        model.SetAllVisible(false);
        Assert.IsTrue(model.DisplayChromatograms.All(c => !c.Visible));
        Assert.AreEqual(200d, AbundanceMax(model), 1e-6, "The axis keeps its last range when nothing is drawn.");

        model.SetAllVisible(true);
        Assert.IsTrue(model.DisplayChromatograms.All(c => c.Visible));
    }

    [TestMethod]
    public async Task ExportWritesOnlyVisibleChromatograms() {
        var shown = Create("shown", 100d);
        var hidden = Create("hidden", 200d);
        hidden.Visible = false;
        using var model = new ChromatogramsModel("test", new List<DisplayChromatogram> { shown, hidden }, "", "RT", "Abundance");

        using var stream = new MemoryStream();
        await model.ExportAsync(stream, "\t");
        var text = Encoding.UTF8.GetString(stream.ToArray());

        StringAssert.Contains(text, "shown\t");
        Assert.IsFalse(text.Contains("hidden\t"));
    }
}
