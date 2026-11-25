using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Utility.Tests;
using CompMs.App.Msdial.ViewModel.PeakCuration;
using CompMs.Common.Components;
using CompMs.Common.Interfaces;
using CompMs.Graphics.Legacy;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialLcmsApi.Parameter;
using CompMs.MsdialLcMsApi.Algorithm.Alignment;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media;

namespace CompMs.App.Msdial.View.PeakCuration.Tests;

/// <summary>
/// The curation windows on an alignment the automatic RT correction made (CONF-17, CONF-18): file F's
/// peak lies at 8.30 min on its measured axis, the axis of its chromatogram, and at 8.60 min on the
/// aligned axis of its aligned peak and of the spot. Markers and ranges are drawn and integrated on the
/// measured axis, and written back on the aligned axis; Update without an edit changes nothing.
/// </summary>
[TestClass]
public class AlignedPeakCurationRtAxisTests
{
    private const double Offset = 0.30d;

    // F's model: +0.30 min everywhere (the reference would be the identity).
    private static AlignmentRetentionTimeCorrectionCollection Correction() {
        var model = new PiecewiseLinearAlignmentRetentionTimeCorrectionModel(new[] {
            new AlignmentRetentionTimeCorrectionControlPoint(0, 100d, 2d, 2d + Offset, 1d),
            new AlignmentRetentionTimeCorrectionControlPoint(1, 200d, 12d, 12d + Offset, 1d),
        });
        return new AlignmentRetentionTimeCorrectionCollection(0, new Dictionary<int, IAlignmentRetentionTimeCorrectionModel> { [1] = model });
    }

    // The measured chromatogram: a Gaussian with its apex at 8.30, a point every 0.01 min.
    private static List<ChromatogramPeak> Chromatogram() {
        return Enumerable.Range(0, 61)
            .Select(i => 8.0d + i * 0.01d)
            .Select((rt, i) => new ChromatogramPeak(i, 300d, 10000d * Math.Exp(-Math.Pow((rt - 8.30d) / 0.03d, 2d)) + 10d, new RetentionTime(rt)))
            .ToList();
    }

    private static (AlignmentSpotPropertyModel Spot, AlignmentChromPeakFeatureModel Peak) Spot() {
        var peak = new AlignmentChromPeakFeature
        {
            FileID = 1,
            FileName = "F",
            MasterPeakID = 3,
            PeakID = 3,
            Mass = 300d,
            ChromXsTop = new ChromXs(8.30d + Offset),
            ChromXsLeft = new ChromXs(8.25d + Offset),
            ChromXsRight = new ChromXs(8.35d + Offset),
            PeakHeightTop = 1234d,
            PeakAreaAboveZero = 5678d,
            PeakAreaAboveBaseline = 4321d,
        };
        peak.PeakShape.EstimatedNoise = 10f;
        peak.PeakShape.SignalToNoise = 99f;
        var spot = new AlignmentSpotProperty
        {
            MasterAlignmentID = 0,
            TimesCenter = new ChromXs(8.30d + Offset),
            MassCenter = 300d,
            AlignedPeakProperties = new List<AlignmentChromPeakFeature> { peak },
        };
        var model = new AlignmentSpotPropertyModel(spot);
        Assert.IsTrue(SpinWait.SpinUntil(() => model.AlignedPeakPropertiesModelProperty.Value is not null, TimeSpan.FromSeconds(10)), "the aligned peaks load");
        return (model, model.AlignedPeakPropertiesModelProperty.Value!.Single());
    }

    private static (double Top, double Left, double Right, double Height, double Area, bool Manual, double Center) State(AlignmentSpotPropertyModel spot, AlignmentChromPeakFeatureModel peak)
        => (peak.ChromXsTop.RT.Value, peak.ChromXsLeft.RT.Value, peak.ChromXsRight.RT.Value, peak.PeakHeightTop, peak.PeakAreaAboveZero, peak.IsManuallyModifiedForQuant, spot.TimesCenter);

    private static PeakPropertyLegacy OverlayProperty(AlignmentChromPeakFeatureModel peak, AlignmentSpotPropertyModel spot, AlignedPeakRtAxisMap rtAxis) {
        // As AlignedChromatogramModificationModelLegacy.LoadPeakProperty builds it.
        var property = new PeakPropertyLegacy(peak, Brushes.Blue, Chromatogram(), null) { RtAxis = rtAxis };
        var top = rtAxis.IsCorrected ? rtAxis.ToOriginal(peak.ChromXsTop.Value) : peak.ChromXsTop.Value;
        property.SetAlignOffSet(top - spot.TimesCenter);
        property.AverageRt = spot.TimesCenter;
        return property;
    }

    [TestMethod]
    public void OverlayDrawsTheAlignedPeakWhereItLiesOnTheChromatogram() {
        var (spot, peak) = Spot();
        var property = OverlayProperty(peak, spot, AlignedPeakRtAxisMap.FromCorrection(Correction(), 1));

        Assert.AreEqual(8.30d, property.OriginalTop, 1e-9, "the marker of the Original and Picking panels");
        Assert.AreEqual(8.25d, property.OriginalLeft, 1e-9);
        Assert.AreEqual(8.35d, property.OriginalRight, 1e-9);
        // The Aligned panel puts the chromatogram's apex at the spot RT.
        var apex = property.AlignedPeakList.OrderByDescending(p => p.Intensity).First();
        Assert.AreEqual(spot.TimesCenter, apex.ChromXs.Value, 1e-9);
    }

    [TestMethod]
    public void OverlayUpdateWithoutARangeChangesNothing() {
        var (spot, peak) = Spot();
        var before = State(spot, peak);
        var property = OverlayProperty(peak, spot, AlignedPeakRtAxisMap.FromCorrection(Correction(), 1));

        new PeakPropertiesLegacy(spot, new[] { property }).UpdatePeakInfo();

        Assert.AreEqual(before, State(spot, peak));
    }

    [TestMethod]
    public void OverlayIntegratesOnTheChromatogramAndWritesTheAlignedAxis() {
        var (spot, peak) = Spot();
        var property = OverlayProperty(peak, spot, AlignedPeakRtAxisMap.FromCorrection(Correction(), 1));

        // The person drags 8.50-8.70 on the Aligned panel, around the shifted apex at 8.60.
        property.ModifyPeakEdge(8.50d, 8.70d);
        Assert.AreEqual(8.30d, property.Accessory!.Chromatogram.RtTop, 1e-6, "the range is moved back onto the chromatogram's axis");
        new PeakPropertiesLegacy(spot, new[] { property }).UpdatePeakInfo();

        Assert.AreEqual(8.60d, peak.ChromXsTop.RT.Value, 1e-6, "the aligned peak stays on the aligned axis");
        Assert.AreEqual(8.50d, peak.ChromXsLeft.RT.Value, 1e-6);
        Assert.AreEqual(8.70d, peak.ChromXsRight.RT.Value, 1e-6);
        Assert.AreEqual(Chromatogram().Max(p => p.Intensity), peak.PeakHeightTop, 1e-6, "the height of the measured apex");
        Assert.IsTrue(peak.IsManuallyModifiedForQuant);
        Assert.AreEqual(8.60d, spot.TimesCenter, 1e-6, "the spot RT is the aligned tops' mean");
    }

    [TestMethod]
    public void OverlayDoesNotWriteAPeakWhosePlaceOnTheMeasuredAxisIsUnknown() {
        var (spot, peak) = Spot();
        var before = State(spot, peak);
        var property = OverlayProperty(peak, spot, AlignedPeakRtAxisMap.Unavailable("edited by hand, no map", Offset));
        property.ModifyPeakEdge(8.50d, 8.70d);

        new PeakPropertiesLegacy(spot, new[] { property }).UpdatePeakInfo();

        Assert.AreEqual(before.Top, peak.ChromXsTop.RT.Value);
        Assert.AreEqual(before.Height, peak.PeakHeightTop);
        Assert.IsFalse(peak.IsManuallyModifiedForQuant);
        StringAssert.Contains(new PeakPropertiesLegacy(spot, new[] { property }).AxisNote, "Not written: F");
    }

    [TestMethod]
    public void OverlayWithoutTheCorrectionBehavesAsBefore() {
        var (spot, peak) = Spot();
        var property = OverlayProperty(peak, spot, AlignedPeakRtAxisMap.NotCorrected);
        Assert.AreEqual(peak.ChromXsTop.Value, property.OriginalTop);
        Assert.AreEqual(0d, property.AlignOffset, 1e-12);
        Assert.AreEqual(string.Empty, new PeakPropertiesLegacy(spot, new[] { property }).AxisNote);
    }

    private static SampleTableRow Row(AlignmentSpotPropertyModel spot, AlignmentChromPeakFeatureModel peak, AlignedPeakRtAxisMap rtAxis) {
        Func<double, double> onChromatogram = value => rtAxis.IsCorrected ? rtAxis.ToOriginal(value) : value;
        var brush = new SolidColorBrush(Colors.Blue);
        brush.Freeze();
        var bean = new ChromatogramBeanLegacy(true, brush, 1.0, "F", 300f, 0.01f, Chromatogram().Cast<IChromatogramPeak>().ToList());
        var chromatogram = new ChromatogramXicViewModelLegacy(
            bean, ChromatogramEditMode.Display, ChromatogramDisplayLabel.None, ChromatogramQuantitativeMode.Height, ChromatogramIntensityMode.Absolute,
            0, "", 300f, 0.01f,
            (float)onChromatogram(peak.ChromXsTop.Value), (float)onChromatogram(peak.ChromXsLeft.Value), (float)onChromatogram(peak.ChromXsRight.Value));
        return new SampleTableRow(spot, peak, chromatogram, "A", 0) { RtAxis = rtAxis };
    }

    [TestMethod]
    public void ManualPeakPickShowsTheMeasuredAxisAndUpdateWithoutAnEditChangesNothing() {
        TestApplication.Ensure();
        Application.Current.Dispatcher.Invoke(() => {
            var (spot, peak) = Spot();
            var before = State(spot, peak);
            var vm = new ChromatogramManualPeakPickViewModelLegacy(Row(spot, peak, AlignedPeakRtAxisMap.FromCorrection(Correction(), 1)), new MsdialLcmsParameter());

            Assert.IsTrue(vm.IsOnMeasuredAxis);
            Assert.IsFalse(vm.IsEdited);
            Assert.AreEqual(8.30f, vm.RtTop!.Value, 0.006f, "integrated where the peak lies on the chromatogram");
            Assert.AreEqual(8.25f, vm.RtLeft!.Value, 0.006f);
            Assert.AreEqual(8.35f, vm.RtRight!.Value, 0.006f);

            Assert.IsFalse(vm.UpdateAlignedProp(), "nothing is written");
            Assert.AreEqual(before, State(spot, peak));
        });
        TestApplication.WaitForBackgroundWork();
    }

    [TestMethod]
    public void ManualPeakPickWritesAnEditOnTheAlignedAxis() {
        TestApplication.Ensure();
        Application.Current.Dispatcher.Invoke(() => {
            var (spot, peak) = Spot();
            var vm = new ChromatogramManualPeakPickViewModelLegacy(Row(spot, peak, AlignedPeakRtAxisMap.FromCorrection(Correction(), 1)), new MsdialLcmsParameter());

            // The person widens the peak to 8.20-8.40 on the measured axis.
            var drawing = vm.ChromUC.drawing!;
            var series = drawing.SeriesList.Series[0];
            series.Accessory.SetChromatogram(8.30f, 8.20f, 8.40f, 10f, 99f);
            drawing.CalculateChromPeakProperties(series);
            Assert.IsTrue(vm.IsEdited);

            Assert.IsTrue(vm.UpdateAlignedProp());
            Assert.AreEqual(8.30d + Offset, peak.ChromXsTop.RT.Value, 0.006d, "the top on the aligned axis");
            Assert.AreEqual(8.20d + Offset, peak.ChromXsLeft.RT.Value, 0.006d);
            Assert.AreEqual(8.40d + Offset, peak.ChromXsRight.RT.Value, 0.006d);
            Assert.AreEqual(Chromatogram().Max(p => p.Intensity), peak.PeakHeightTop, 1d, "the measured apex's height");
            Assert.IsTrue(peak.IsManuallyModifiedForQuant);
        });
        TestApplication.WaitForBackgroundWork();
    }

    [TestMethod]
    public void ManualPeakPickOfAPeakWithoutAKnownPlaceIsReadOnly() {
        TestApplication.Ensure();
        Application.Current.Dispatcher.Invoke(() => {
            var (spot, peak) = Spot();
            var before = State(spot, peak);
            var vm = new ChromatogramManualPeakPickViewModelLegacy(Row(spot, peak, AlignedPeakRtAxisMap.Unavailable("edited by hand, no map", Offset)), new MsdialLcmsParameter());
            var drawing = vm.ChromUC.drawing!;
            var series = drawing.SeriesList.Series[0];
            series.Accessory.SetChromatogram(8.30f, 8.20f, 8.40f, 10f, 99f);
            drawing.CalculateChromPeakProperties(series);

            Assert.AreEqual("edited by hand, no map", vm.AxisNote);
            Assert.IsFalse(vm.UpdateAlignedProp());
            Assert.AreEqual(before, State(spot, peak));
        });
        TestApplication.WaitForBackgroundWork();
    }

    [TestMethod]
    public void ManualPeakPickWithoutTheCorrectionWritesAsBefore() {
        TestApplication.Ensure();
        Application.Current.Dispatcher.Invoke(() => {
            var (spot, peak) = Spot();
            var vm = new ChromatogramManualPeakPickViewModelLegacy(Row(spot, peak, AlignedPeakRtAxisMap.NotCorrected), new MsdialLcmsParameter());

            Assert.IsFalse(vm.IsOnMeasuredAxis);
            Assert.IsTrue(vm.UpdateAlignedProp(), "an alignment without the correction re-integrates on Update, as it always did");
            Assert.IsTrue(peak.IsManuallyModifiedForQuant);
        });
        TestApplication.WaitForBackgroundWork();
    }
}
