using CompMs.Common.Components;
using CompMs.Graphics.Core.Base;
using CompMs.Graphics.Design;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Chart.Tests;

[TestClass]
public class SingleSpectrumModelTests {
    [STATestMethod]
    public void MinimumAnnotationOrder_TracksTheExperimentIndependentlyOfTheReference() {
        using var source = new BehaviorSubject<MsSpectrum>(new MsSpectrum(new[] {
            new SpectrumPeak(30d, 1000d), new SpectrumPeak(90d, 6d),
        }));
        using var observable = new ObservableMsSpectrum(source, null, null);
        using var model = CreateModel(observable);
        using var referenceObservable = new ObservableMsSpectrum(Observable.Return(new MsSpectrum(new[] {
            new SpectrumPeak(90d, 500d),
        })).Concat(Observable.Never<MsSpectrum>()), null, null);
        using var reference = CreateModel(referenceObservable);
        double minimum = 0d, referenceMinimum = 0d;
        using var subscription = model.MinimumAnnotationOrder.Subscribe(value => minimum = value);
        using var referenceSubscription = reference.MinimumAnnotationOrder.Subscribe(value => referenceMinimum = value);

        Assert.AreEqual(10d, minimum);
        Assert.AreEqual(5d, referenceMinimum);
        source.OnNext(new MsSpectrum(new[] { new SpectrumPeak(30d, 2000d), new SpectrumPeak(90d, 6d) }));
        Assert.AreEqual(20d, minimum);
        Assert.AreEqual(5d, referenceMinimum);
    }

    [STATestMethod]
    public void MinimumAnnotationOrder_DoesNotFilterFragmentationScores() {
        using var observable = new ObservableMsSpectrum(Observable.Return(new MsSpectrum(new[] {
            new SpectrumPeak(30d, 1000d) { FragmentationScore = 100d },
            new SpectrumPeak(90d, 1000d) { FragmentationScore = .5d },
        })).Concat(Observable.Never<MsSpectrum>()), null, null);
        using var model = CreateModel(observable);
        double minimum = 0d;
        using var subscription = model.MinimumAnnotationOrder.Subscribe(value => minimum = value);
        Assert.AreEqual(10d, minimum);

        model.Labels.AnnotationOrderProperty = nameof(SpectrumPeak.FragmentationScore);
        Assert.AreEqual(double.NegativeInfinity, minimum);
    }

    [STATestMethod]
    public void MinimumAnnotationOrder_HidesEmptyZeroAndUnloadedSpectra() {
        using var source = new BehaviorSubject<MsSpectrum?>(null);
        using var observable = new ObservableMsSpectrum(source, null, null);
        using var model = CreateModel(observable);
        double minimum = 0d;
        using var subscription = model.MinimumAnnotationOrder.Subscribe(value => minimum = value);
        Assert.AreEqual(double.PositiveInfinity, minimum);

        source.OnNext(new MsSpectrum(Array.Empty<SpectrumPeak>()));
        Assert.AreEqual(double.PositiveInfinity, minimum);
        source.OnNext(new MsSpectrum(new[] { new SpectrumPeak(30d, 0d) }));
        Assert.AreEqual(double.PositiveInfinity, minimum);
    }

    private static SingleSpectrumModel CreateModel(ObservableMsSpectrum observable) => new SingleSpectrumModel(
        observable,
        observable.CreateAxisPropertySelectors(new PropertySelector<SpectrumPeak, double>(peak => peak.Mass), "m/z", "m/z"),
        observable.CreateAxisPropertySelectors2(new PropertySelector<SpectrumPeak, double>(peak => peak.Intensity), "abundance"),
        new ChartHueItem(nameof(SpectrumPeak.SpectrumComment), new ConstantBrushMapper<object>(Brushes.Blue)),
        new GraphLabels("Spectrum", "m/z", "Intensity", nameof(SpectrumPeak.Mass), nameof(SpectrumPeak.Intensity)));
}
