using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
using CompMs.Graphics.Design;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reactive.Bindings;
using Reactive.Bindings.Notifiers;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Chart.Tests
{
    [TestClass()]
    public class AlignmentMs2SpectrumModelTests
    {
        [TestMethod()]
        public void TheVerticalTitlesFollowTheChosenAxisTypes() {
            var files = new AnalysisFileBeanModelCollection(new[] { new AnalysisFileBean { AnalysisFileId = 0, AnalysisFileName = "QC_01" } });
            var target = new ReactivePropertySlim<AlignmentSpotPropertyModel?>(null);
            var loader = new AlignmentSpotSpectraLoader(files, new NoReferenceLoader(), new CompoundSearcherCollection([]), files);
            var hue = new ChartHueItem(nameof(SpectrumPeak.SpectrumComment), new ConstantBrushMapper(Brushes.Blue));
            using var model = new AlignmentMs2SpectrumModel(
                target,
                Observable.Return<MsScanMatchResult?>(null),
                files,
                new PropertySelector<SpectrumPeak, double>(peak => peak.Mass),
                new PropertySelector<SpectrumPeak, double>(peak => peak.Intensity),
                hue,
                hue,
                new GraphLabels("Representative vs. Reference", "m/z", "Relative abundance", nameof(SpectrumPeak.Mass), nameof(SpectrumPeak.Intensity)),
                Observable.Never<ISpectraExporter>(),
                Observable.Never<ISpectraExporter>(),
                null,
                loader);
            using var vm = new AlignmentMs2SpectrumViewModel(model, new MessageBroker());

            CollectionAssert.AreEqual(new[] { "Relative abundance" }, vm.VerticalTitles.Value.Titles);

            model.UpperVerticalAxisSelector.Select(model.UpperVerticalAxisSelector.AxisItems.Single(item => item.Label == "Absolute"));
            CollectionAssert.AreEqual(new[] { "Absolute abundance", "Relative abundance" }, vm.VerticalTitles.Value.Titles, "The upper and the lower spectrum get their own titles once they differ.");

            model.LowerVerticalAxisSelector.Select(model.LowerVerticalAxisSelector.AxisItems.Single(item => item.Label == "Absolute"));
            CollectionAssert.AreEqual(new[] { "Absolute abundance" }, vm.VerticalTitles.Value.Titles);

            model.UpperVerticalAxisSelector.Select(model.UpperVerticalAxisSelector.AxisItems.Single(item => item.Label == "Log10"));
            CollectionAssert.AreEqual(new[] { "Relative abundance (log10)", "Absolute abundance" }, vm.VerticalTitles.Value.Titles);
        }

        private sealed class NoReferenceLoader : IMsSpectrumLoader<MsScanMatchResult>
        {
            public IObservable<IMSScanProperty?> LoadScanAsObservable(MsScanMatchResult target) => Observable.Return<IMSScanProperty?>(null);
        }
    }
}
