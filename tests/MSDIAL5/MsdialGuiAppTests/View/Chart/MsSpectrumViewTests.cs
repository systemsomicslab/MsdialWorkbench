using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Utility.Tests;
using CompMs.App.Msdial.View.Chart;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.Common.Components;
using CompMs.Graphics.Chart;
using CompMs.Graphics.Core.Base;
using CompMs.Graphics.Design;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace CompMs.App.Msdial.View.Chart.Tests;

[TestClass]
public class MsSpectrumViewTests {
    [TestMethod]
    public void SpectrumViews_ResolveTheirSharedLabelPlacementResource() {
        TestApplication.Ensure();
        Application.Current.Dispatcher.Invoke(() => {
            // Match App.xaml's theme brushes without starting the application's main window.
            var theme = new ResourceDictionary {
                ["MainThemeColor"] = Brushes.LightBlue,
                ["SubThemeColor"] = Brushes.DarkGreen,
                ["AccentColor"] = Brushes.DeepPink,
            };
            Application.Current.Resources.MergedDictionaries.Add(theme);
            try {
                var views = new FrameworkElement[] {
                    new MsSpectrumView(), new AlignmentMs2SpectrumView(), new MsSpectrumViewWithSubstructureImage(),
                    new RawPurifiedView(), new SingleSpectrumView(), new SpectrumView(), new SurveyScanView(),
                    new Information.SpectraSimilarityMapView(), new Spectra.SpectraGroupingView(),
                    new MsResult.AccumulatedExtractedMs2SpectrumView(), new MsResult.AccumulatedMs1SpectrumView(),
                    new MsResult.AccumulatedMs2SpectrumView(), new MsResult.AccumulatedSpecificExperimentMS2SpectrumView(),
                };
                foreach (var view in views) {
                    Assert.IsInstanceOfType<SpectrumLabelPlacement>(view.FindResource("SpectrumLabelPlacement"), view.GetType().Name);
                }
            }
            finally {
                Application.Current.Resources.MergedDictionaries.Remove(theme);
            }
        });
        TestApplication.WaitForBackgroundWork();
    }

    [TestMethod]
    public void Labels_UseTheFullExperimentAcrossMatchedUnmatchedAndOverlayLayers() {
        TestApplication.Ensure();
        Application.Current.Dispatcher.Invoke(() => {
            using var source = new BehaviorSubject<MsSpectrum>(new MsSpectrum(new[] {
                new SpectrumPeak(10d, 0d), new SpectrumPeak(267d, 1000d), new SpectrumPeak(269d, 700d), new SpectrumPeak(300d, 400d),
            }));
            using var reference = new BehaviorSubject<MsSpectrum>(new MsSpectrum(new[] {
                new SpectrumPeak(10d, 0d), new SpectrumPeak(267d, 1000d), new SpectrumPeak(300d, 400d),
            }));
            using var upperObservable = new ObservableMsSpectrum(source, null, null);
            using var lowerObservable = new ObservableMsSpectrum(reference, null, null);
            using var upper = CreateModel(upperObservable);
            using var lower = CreateModel(lowerObservable);
            using var model = new MsSpectrumModel(upper, lower, null);
            // An unlabelled raw overlay can precede the experiment in the layer collection.
            using var overlay = lower.Clone();
            overlay.IsAnnotationVisible.Value = false;
            model.UpperSpectraModel.Insert(0, overlay);
            using var viewModel = new MsSpectrumViewModel(model);
            var view = new MsSpectrumView { DataContext = viewModel };

            AssertLabels();
            model.SwitchViewToCompareSpectrum();
            AssertLabels();
            viewModel.HorizontalAxis.Value.Focus(new AxisRange(260d, 310d));
            AssertLabels(includeNeighbour: true);
            viewModel.HorizontalAxis.Value.Focus(new AxisRange(0d, 320d));
            source.OnNext(new MsSpectrum(new[] {
                new SpectrumPeak(10d, 0d), new SpectrumPeak(267d, 700d), new SpectrumPeak(269d, 1000d), new SpectrumPeak(300d, 400d),
            }));
            AssertLabels("269.0000");
            model.SwitchViewToAllSpectrum();
            AssertLabels("269.0000");
            viewModel.HorizontalAxis.Value.Focus(new AxisRange(260d, 310d));
            AssertLabels("269.0000", includeNeighbour: true);
            viewModel.HorizontalAxis.Value.Focus(new AxisRange(0d, 320d));
            AssertLabels("269.0000");

            void AssertLabels(string strongest = "267.0000", bool includeNeighbour = false) {
                view.Measure(new Size(450d, 300d));
                view.Arrange(new Rect(0d, 0d, 450d, 300d));
                view.UpdateLayout();
                var frame = new DispatcherFrame();
                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new System.Action(() => frame.Continue = false));
                Dispatcher.PushFrame(frame);
                view.UpdateLayout();
                new RenderTargetBitmap(450, 300, 96d, 96d, PixelFormats.Pbgra32).Render(view);
                var labels = Descendants(view).OfType<Annotator>()
                    .Where(chart => chart.ActualWidth > 0d)
                    .SelectMany(chart => Glyphs(VisualTreeHelper.GetDrawing(chart))).ToArray();
                var expected = new List<string> { strongest, "300.0000", "267.0000", "300.0000" };
                if (includeNeighbour) {
                    expected.Add(strongest == "267.0000" ? "269.0000" : "267.0000");
                }
                CollectionAssert.AreEquivalent(expected, labels);
                Assert.AreEqual(4, source.Value.Spectrum.Count);
            }
        });
        TestApplication.WaitForBackgroundWork();
    }

    private static SingleSpectrumModel CreateModel(ObservableMsSpectrum observable) => new SingleSpectrumModel(
        observable,
        observable.CreateAxisPropertySelectors(new PropertySelector<SpectrumPeak, double>(peak => peak.Mass), "m/z", "m/z"),
        observable.CreateAxisPropertySelectors2(new PropertySelector<SpectrumPeak, double>(peak => peak.Intensity), "abundance"),
        new ChartHueItem(nameof(SpectrumPeak.SpectrumComment), new ConstantBrushMapper<object>(Brushes.Blue)),
        new GraphLabels("Spectrum", "m/z", "Intensity", nameof(SpectrumPeak.Mass), nameof(SpectrumPeak.Intensity)));

    private static IEnumerable<DependencyObject> Descendants(DependencyObject parent) {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is UIElement element && element.Visibility != Visibility.Visible) {
                continue;
            }
            yield return child;
            foreach (var descendant in Descendants(child)) {
                yield return descendant;
            }
        }
    }

    private static IEnumerable<string> Glyphs(Drawing? drawing) => drawing switch {
        GlyphRunDrawing glyph => new[] { new string(glyph.GlyphRun.Characters.ToArray()) },
        DrawingGroup group => group.Children.SelectMany(Glyphs),
        _ => Enumerable.Empty<string>(),
    };
}
