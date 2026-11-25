using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Graphics.AxisManager;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Chart.Tests
{
    [TestClass()]
    public class Ms2SpectrumListTests
    {
        private static List<SpectrumPeak> Peaks(params (double mz, double intensity)[] peaks) => peaks.Select(p => new SpectrumPeak(p.mz, p.intensity)).ToList();

        [TestMethod()]
        public void TheRepresentativeComesFirstAndTheRestKeepTheirOrder() {
            var a = new Ms2SpectrumListItem("A", "", false, Peaks((100, 1)));
            var b = new Ms2SpectrumListItem("B", "", false, Peaks((100, 1)));
            var rep = new Ms2SpectrumListItem("Rep", "", true, Peaks((100, 1)));
            var c = new Ms2SpectrumListItem("C", "", false, Peaks((100, 1)));

            var arranged = Ms2SpectrumList.Arrange([a, b, rep, c], showEmpty: false);

            CollectionAssert.AreEqual(new[] { "Rep", "A", "B", "C" }, arranged.Select(item => item.Title).ToArray());
        }

        [TestMethod()]
        public void SpectraWithoutPeaksAreHiddenUnlessAsked() {
            var a = new Ms2SpectrumListItem("A", "", false, Peaks((100, 1)));
            var empty = new Ms2SpectrumListItem("Empty", "", false, []);
            var noSpectrum = new Ms2SpectrumListItem("Null", "", false, null);
            var rep = new Ms2SpectrumListItem("Rep", "", true, Peaks((100, 1)));

            CollectionAssert.AreEqual(new[] { "Rep", "A" }, Ms2SpectrumList.Arrange([a, empty, noSpectrum, rep], showEmpty: false).Select(item => item.Title).ToArray());
            CollectionAssert.AreEqual(new[] { "Rep", "A", "Empty", "Null" }, Ms2SpectrumList.Arrange([a, empty, noSpectrum, rep], showEmpty: true).Select(item => item.Title).ToArray());
        }

        [TestMethod()]
        public void TheMassRangeCoversEverySpectrum() {
            var spectra = new[] { new MsSpectrum(Peaks((150, 1), (300, 2))), null, new MsSpectrum([]), new MsSpectrum(Peaks((80, 5), (200, 1))) };

            Assert.AreEqual((80d, 300d), Ms2SpectrumList.GetMassRange(spectra));
            Assert.AreEqual((0d, 1d), Ms2SpectrumList.GetMassRange([null, new MsSpectrum([])]));
            Assert.AreEqual((99.5d, 100.5d), Ms2SpectrumList.GetMassRange([new MsSpectrum(Peaks((100, 1)))]));
        }

        [TestMethod()]
        public void TheIntensityRangeCoversTheSpectrum() {
            Assert.AreEqual((50d, 200d), Ms2SpectrumList.GetIntensityRange(new MsSpectrum(Peaks((200, 50), (100, 200), (300, 120)))));
            Assert.AreEqual((0d, 1d), Ms2SpectrumList.GetIntensityRange(new MsSpectrum([])));
            Assert.AreEqual((0d, 1d), Ms2SpectrumList.GetIntensityRange(null));
        }

        [DataTestMethod]
        [DataRow("Relative", "Relative")]
        [DataRow("Absolute", "Absolute")]
        [DataRow("Log10", "Log10")]
        [DataRow("Sqrt", "Sqrt")]
        [DataRow(" absolute ", "Absolute")]
        public void TheAxisLabelsOfTheMainViewAreUnderstood(string label, string expected) {
            Assert.IsTrue(Ms2SpectrumList.TryParseAxisType(label, out var type));
            Assert.AreEqual(expected, type.ToString());
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("Percentile")]
        [DataRow("7")]
        public void UnknownAxisLabelsAreIgnored(string? label) {
            Assert.IsFalse(Ms2SpectrumList.TryParseAxisType(label, out _));
        }

        [TestMethod()]
        public void TheAxisTitlesMatchTheMainView() {
            Assert.AreEqual("Relative abundance", Ms2SpectrumList.GetAxisTitle(Ms2SpectrumAxisType.Relative));
            Assert.AreEqual("Absolute abundance", Ms2SpectrumList.GetAxisTitle(Ms2SpectrumAxisType.Absolute));
            Assert.AreEqual("Relative abundance (log10)", Ms2SpectrumList.GetAxisTitle(Ms2SpectrumAxisType.Log10));
            Assert.AreEqual("Relative abundance (^1/2)", Ms2SpectrumList.GetAxisTitle(Ms2SpectrumAxisType.Sqrt));
        }

        [TestMethod()]
        public void EachAxisTypeGetsItsKindOfAxis() {
            var spectrum = new MsSpectrum(Peaks((100, 20), (200, 5000)));

            var relative = Ms2SpectrumListViewModel.CreateVerticalAxis(Ms2SpectrumAxisType.Relative, spectrum);
            Assert.IsInstanceOfType(relative, typeof(ContinuousAxisManager<double>));
            Assert.AreEqual(LabelType.Percent, ((ContinuousAxisManager<double>)relative).LabelType);
            Assert.AreEqual(0d, relative.Range.Minimum.Value, "Peaks stand on zero.");

            var absolute = Ms2SpectrumListViewModel.CreateVerticalAxis(Ms2SpectrumAxisType.Absolute, spectrum);
            Assert.AreEqual(LabelType.Standard, ((ContinuousAxisManager<double>)absolute).LabelType);
            Assert.AreEqual(5000d, ((ContinuousAxisManager<double>)absolute).InitialRange.Maximum.Value, 1e-9);

            Assert.IsInstanceOfType(Ms2SpectrumListViewModel.CreateVerticalAxis(Ms2SpectrumAxisType.Log10, spectrum), typeof(LogScaleAxisManager<double>));
            Assert.IsInstanceOfType(Ms2SpectrumListViewModel.CreateVerticalAxis(Ms2SpectrumAxisType.Sqrt, spectrum), typeof(SqrtAxisManager));
            Assert.IsInstanceOfType(Ms2SpectrumListViewModel.CreateVerticalAxis(Ms2SpectrumAxisType.Log10, null), typeof(LogScaleAxisManager<double>), "An empty spectrum still gets an axis.");
        }

        [TestMethod()]
        public async Task TheAxisTypeFollowsTheMainViewAndCanBeChangedInTheWindow() {
            var target = new ReactivePropertySlim<string?>("spot");
            var mainAxis = new ReactivePropertySlim<string?>("Absolute");
            using var model = Ms2SpectrumListModel.Create("title", "samples", target, t => t,
                _ => Task.FromResult<IReadOnlyList<Ms2SpectrumListItem>>([new Ms2SpectrumListItem("A", "", true, Peaks((120, 4), (130, 8)))]),
                Observable.Return<MsSpectrum?>(null),
                mainAxis);
            using var vm = new Ms2SpectrumListViewModel(model);
            await WaitUntilAsync(() => vm.Tiles.Value.Count == 1);

            Assert.AreEqual(Ms2SpectrumAxisType.Absolute, vm.AxisType.Value, "The window opens with the axis of the main view.");
            Assert.AreEqual(Ms2SpectrumAxisType.Absolute, vm.Tiles.Value[0].AxisType);
            Assert.AreEqual("Absolute abundance", vm.Tiles.Value[0].VerticalTitle);

            vm.AxisType.Value = Ms2SpectrumAxisType.Log10;
            Assert.AreEqual("Relative abundance (log10)", vm.Tiles.Value[0].VerticalTitle);
            Assert.IsInstanceOfType(vm.Tiles.Value[0].VerticalAxis, typeof(LogScaleAxisManager<double>));

            mainAxis.Value = "Sqrt";
            Assert.AreEqual(Ms2SpectrumAxisType.Sqrt, vm.AxisType.Value, "Changing the main view changes the window too.");
            Assert.IsInstanceOfType(vm.Tiles.Value[0].VerticalAxis, typeof(SqrtAxisManager));

            mainAxis.Value = "Unknown";
            Assert.AreEqual(Ms2SpectrumAxisType.Sqrt, vm.AxisType.Value);
            CollectionAssert.AreEqual(new[] { Ms2SpectrumAxisType.Relative, Ms2SpectrumAxisType.Absolute, Ms2SpectrumAxisType.Log10, Ms2SpectrumAxisType.Sqrt }, vm.AxisTypes.ToArray());
        }

        [TestMethod()]
        public void AlignedSamplesAreMarkedByTheRepresentativeFile() {
            var items = Ms2SpectrumList.FromAlignedSamples(
                [
                    new AlignedSampleMs2(0, "QC_01", "QC", Peaks((100, 1), (120, 2))),
                    new AlignedSampleMs2(1, "Sample_01", "", Peaks((100, 1))),
                    new AlignedSampleMs2(2, "Blank_01", "Blank", null),
                ],
                representativeFileId: 1);

            CollectionAssert.AreEqual(new[] { "QC_01", "Sample_01", "Blank_01" }, items.Select(item => item.Title).ToArray());
            CollectionAssert.AreEqual(new[] { false, true, false }, items.Select(item => item.IsRepresentative).ToArray());
            CollectionAssert.AreEqual(new[] { "Class: QC, 2 peaks", "1 peak", "Class: Blank, no MS2" }, items.Select(item => item.Detail).ToArray());
            CollectionAssert.AreEqual(new[] { true, true, false }, items.Select(item => item.HasSpectrum).ToArray());
            Assert.AreEqual("2 of 3 samples have an MS2 spectrum.", Ms2SpectrumList.Summarize(items.ToList(), "samples"));
        }

        [TestMethod()]
        public void PeakScansAreDescribedByTimePrecursorAndCollisionEnergy() {
            var items = Ms2SpectrumList.FromPeakScans(
                [
                    new PeakMs2Scan(10, 35d, 5.1234, 760.5851, Peaks((184.07, 100))),
                    new PeakMs2Scan(12, 0d, null, null, null),
                ],
                representativeScanId: 10);

            CollectionAssert.AreEqual(new[] { "Scan 10", "Scan 12" }, items.Select(item => item.Title).ToArray());
            Assert.AreEqual("RT 5.123 min, Precursor m/z 760.5851, CE 35 eV, 1 peak", items[0].Detail);
            Assert.AreEqual("no MS2", items[1].Detail);
            CollectionAssert.AreEqual(new[] { true, false }, items.Select(item => item.IsRepresentative).ToArray());
        }

        [TestMethod()]
        public async Task TheListFollowsTheSelectedSpot() {
            var target = new ReactivePropertySlim<string?>(null);
            var reference = new ReactivePropertySlim<MsSpectrum?>(null);
            var loaded = new List<string>();
            Task<IReadOnlyList<Ms2SpectrumListItem>> Load(string t) {
                lock (loaded) { loaded.Add(t); }
                IReadOnlyList<Ms2SpectrumListItem> items = t == "spot1"
                    ? [new Ms2SpectrumListItem("S1", "", false, Peaks((100, 1))), new Ms2SpectrumListItem("S2", "", true, Peaks((300, 1))), new Ms2SpectrumListItem("S3", "", false, [])]
                    : [new Ms2SpectrumListItem("T1", "", true, Peaks((50, 1)))];
                return Task.FromResult(items);
            }
            using var model = Ms2SpectrumListModel.Create("title", "samples", target, t => $"Header {t}", Load, reference);

            Assert.AreEqual(0, model.VisibleItems.Value.Count);
            Assert.AreEqual("No spot is selected.", model.Header.Value);

            target.Value = "spot1";
            await WaitUntilAsync(() => !model.IsLoading.Value && model.Items.Value.Count == 3);
            Assert.AreEqual("Header spot1", model.Header.Value);
            CollectionAssert.AreEqual(new[] { "S2", "S1" }, model.VisibleItems.Value.Select(item => item.Title).ToArray());
            Assert.AreEqual("2 of 3 samples have an MS2 spectrum.", model.Summary.Value);
            Assert.AreEqual((100d, 300d), model.MassRange.Value);

            model.ShowEmpty.Value = true;
            CollectionAssert.AreEqual(new[] { "S2", "S1", "S3" }, model.VisibleItems.Value.Select(item => item.Title).ToArray());

            reference.Value = new MsSpectrum(Peaks((40, 1), (500, 1)));
            Assert.IsTrue(model.HasReference.Value);
            Assert.AreEqual((100d, 300d), model.MassRange.Value, "The reference widens the range only when it is drawn.");
            model.ShowReference.Value = true;
            Assert.AreEqual((40d, 500d), model.MassRange.Value);

            target.Value = "spot2";
            await WaitUntilAsync(() => !model.IsLoading.Value && model.Items.Value.Count == 1);
            Assert.AreEqual("Header spot2", model.Header.Value);
            CollectionAssert.AreEqual(new[] { "T1" }, model.VisibleItems.Value.Select(item => item.Title).ToArray());

            target.Value = null;
            await WaitUntilAsync(() => model.Items.Value.Count == 0);
            Assert.AreEqual(0, model.VisibleItems.Value.Count);
            CollectionAssert.AreEqual(new[] { "spot1", "spot2" }, loaded);
        }

        [TestMethod()]
        public async Task AFailedReadIsReportedAndTheNextSpotStillLoads() {
            var target = new ReactivePropertySlim<string?>(null);
            Task<IReadOnlyList<Ms2SpectrumListItem>> Load(string t) {
                if (t == "broken") {
                    throw new InvalidOperationException("no dcl");
                }
                return Task.FromResult<IReadOnlyList<Ms2SpectrumListItem>>([new Ms2SpectrumListItem("OK", "", true, Peaks((100, 1)))]);
            }
            using var model = Ms2SpectrumListModel.Create("title", "samples", target, t => t, Load, Observable.Return<MsSpectrum?>(null));

            target.Value = "broken";
            await WaitUntilAsync(() => model.Error.Value is not null);
            StringAssert.Contains(model.Error.Value, "no dcl");
            Assert.AreEqual(0, model.Items.Value.Count);

            target.Value = "fine";
            await WaitUntilAsync(() => model.Items.Value.Count == 1);
            Assert.IsNull(model.Error.Value);
        }

        [TestMethod()]
        public async Task TheViewModelMirrorsTheReferenceOnlyWhenAsked() {
            var target = new ReactivePropertySlim<string?>("spot");
            var reference = new ReactivePropertySlim<MsSpectrum?>(new MsSpectrum(Peaks((100, 10), (150, 5))));
            using var model = Ms2SpectrumListModel.Create("title", "samples", target, t => t,
                _ => Task.FromResult<IReadOnlyList<Ms2SpectrumListItem>>([new Ms2SpectrumListItem("A", "", false, Peaks((120, 4), (130, 8))), new Ms2SpectrumListItem("Rep", "", true, Peaks((110, 2)))]),
                reference);
            using var vm = new Ms2SpectrumListViewModel(model);
            await WaitUntilAsync(() => vm.Tiles.Value.Count == 2);

            CollectionAssert.AreEqual(new[] { "Rep", "A" }, vm.Tiles.Value.Select(tile => tile.Title).ToArray());
            Assert.IsTrue(vm.Tiles.Value.All(tile => tile.ReferencePeaks.Count == 0));
            Assert.AreSame(vm.Tiles.Value[0].HorizontalAxis, vm.Tiles.Value[1].HorizontalAxis, "Every tile shares the m/z axis.");
            CollectionAssert.AreEqual(new[] { 4d, 8d }, vm.Tiles.Value[1].Peaks.Select(p => p.Intensity).ToArray());
            Assert.AreEqual(0d, vm.Tiles.Value[0].VerticalAxis.Range.Minimum.Value);
            Assert.AreNotSame(vm.Tiles.Value[0].VerticalAxis, vm.Tiles.Value[1].VerticalAxis, "Each spectrum is scaled to its own intensities.");

            Assert.IsTrue(vm.Tiles.Value.All(tile => !tile.IsMirrored));

            vm.ShowReference.Value = true;
            Assert.IsTrue(vm.Tiles.Value.All(tile => tile.IsMirrored));
            // The reference is drawn downward in its own pane, so its values stay positive and the m/z axis stays at the baseline of both.
            CollectionAssert.AreEqual(new[] { 10d, 5d }, vm.Tiles.Value[0].ReferencePeaks.Select(p => p.Intensity).ToArray());
            Assert.AreEqual(0d, vm.Tiles.Value[0].VerticalAxis.Range.Minimum.Value);
            Assert.AreEqual(0d, vm.Tiles.Value[0].ReferenceVerticalAxis.Range.Minimum.Value);
            Assert.AreNotSame(vm.Tiles.Value[0].VerticalAxis, vm.Tiles.Value[0].ReferenceVerticalAxis);

            reference.Value = null;
            Assert.IsFalse(vm.HasReference.Value);
            Assert.IsTrue(vm.Tiles.Value.All(tile => !tile.IsMirrored && tile.ReferencePeaks.Count == 0));
        }

        [TestMethod()]
        public async Task AClickedTileIsEnlargedAndTheArrowsStepThroughTheTiles() {
            var target = new ReactivePropertySlim<string?>("spot1");
            Task<IReadOnlyList<Ms2SpectrumListItem>> Load(string t) => Task.FromResult<IReadOnlyList<Ms2SpectrumListItem>>(t == "spot1"
                ? [new Ms2SpectrumListItem("A", "", false, Peaks((100, 1))), new Ms2SpectrumListItem("Rep", "", true, Peaks((110, 1))), new Ms2SpectrumListItem("B", "", false, Peaks((120, 1)))]
                : [new Ms2SpectrumListItem("B", "", true, Peaks((130, 1))), new Ms2SpectrumListItem("C", "", false, Peaks((140, 1)))]);
            using var model = Ms2SpectrumListModel.Create("title", "samples", target, t => t, Load, Observable.Return<MsSpectrum?>(null));
            using var vm = new Ms2SpectrumListViewModel(model);
            await WaitUntilAsync(() => vm.Tiles.Value.Count == 3);

            Assert.IsFalse(vm.IsEnlarged.Value);
            Assert.IsFalse(vm.ShowNextCommand.CanExecute(), "The arrows do nothing while the tiles are shown.");

            vm.EnlargeCommand.Execute(vm.Tiles.Value[1]);
            Assert.IsTrue(vm.IsEnlarged.Value);
            Assert.AreEqual("A", vm.EnlargedTile.Value?.Title);
            Assert.AreEqual("2 / 3", vm.EnlargedPosition.Value);

            vm.ShowNextCommand.Execute();
            Assert.AreEqual("B", vm.EnlargedTile.Value?.Title);
            Assert.AreEqual("3 / 3", vm.EnlargedPosition.Value);
            vm.ShowNextCommand.Execute();
            Assert.AreEqual("Rep", vm.EnlargedTile.Value?.Title, "Stepping past the last tile wraps around to the first.");
            vm.ShowPreviousCommand.Execute();
            Assert.AreEqual("B", vm.EnlargedTile.Value?.Title);

            // Selecting another spot keeps the same sample enlarged when it is still listed.
            target.Value = "spot2";
            await WaitUntilAsync(() => vm.Tiles.Value.Count == 2 && vm.EnlargedTile.Value is not null);
            Assert.AreEqual("B", vm.EnlargedTile.Value?.Title);
            Assert.AreSame(vm.Tiles.Value[0], vm.EnlargedTile.Value);
            Assert.AreEqual("1 / 2", vm.EnlargedPosition.Value);

            vm.CloseEnlargedCommand.Execute();
            Assert.IsFalse(vm.IsEnlarged.Value);
            Assert.AreEqual(string.Empty, vm.EnlargedPosition.Value);
        }

        [TestMethod()]
        public async Task EveryMs2ScanOfThePeakIsRead() {
            var spectra = Enumerable.Range(0, 5).Select(i => new RawSpectrum
            {
                Index = i,
                MsLevel = i == 0 ? 1 : 2,
                ScanStartTime = 5d + i * .01d,
                Precursor = new RawPrecursorIon { SelectedIonMz = 500d },
                Spectrum = [new RawPeakElement { Mz = 100d + i, Intensity = 10d }, new RawPeakElement { Mz = 600d, Intensity = 5d }],
            }).ToList();
            var parameter = new ParameterBase { MS2DataType = MSDataType.Centroid, RemoveAfterPrecursor = false };
            using var loader = new MultiMsmsRawSpectrumLoader(new StubProvider(spectra), parameter);
            var feature = new ChromatogramPeakFeature
            {
                Mass = 500d,
                MS2RawSpectrumID = 3,
                MS2RawSpectrumID2CE = new Dictionary<int, double> { [3] = 20d, [1] = 35d, [42] = 20d },
            };
            var peak = new ChromatogramPeakFeatureModel(feature);

            var items = await loader.LoadMs2SpectrumListAsync(peak);

            CollectionAssert.AreEqual(new[] { "Scan 1", "Scan 3", "Scan 42" }, items.Select(item => item.Title).ToArray());
            CollectionAssert.AreEqual(new[] { false, true, false }, items.Select(item => item.IsRepresentative).ToArray());
            CollectionAssert.AreEqual(new[] { 101d, 600d }, items[0].Spectrum.Spectrum.Select(p => p.Mass).ToArray());
            Assert.AreEqual("RT 5.010 min, Precursor m/z 500.0000, CE 35 eV, 2 peaks", items[0].Detail);
            Assert.IsFalse(items[2].HasSpectrum, "A scan the raw data does not have is listed with no spectrum.");

            parameter.RemoveAfterPrecursor = true;
            parameter.KeptIsotopeRange = 5f;
            items = await loader.LoadMs2SpectrumListAsync(peak);
            CollectionAssert.AreEqual(new[] { 101d }, items[0].Spectrum.Spectrum.Select(p => p.Mass).ToArray());

            Assert.AreEqual(0, (await loader.LoadMs2SpectrumListAsync(new ChromatogramPeakFeatureModel(new ChromatogramPeakFeature()))).Count);
        }

        private static async Task WaitUntilAsync(Func<bool> condition) {
            var watch = Stopwatch.StartNew();
            while (!condition()) {
                if (watch.Elapsed > TimeSpan.FromSeconds(10)) {
                    Assert.Fail("Timed out.");
                }
                await Task.Delay(10).ConfigureAwait(false);
            }
        }

        private sealed class StubProvider : IDataProvider
        {
            private readonly ReadOnlyCollection<RawSpectrum> _spectra;

            public StubProvider(List<RawSpectrum> spectra) {
                _spectra = spectra.AsReadOnly();
            }

            public ReadOnlyCollection<RawSpectrum> LoadMsSpectrums() => _spectra;
            public ReadOnlyCollection<RawSpectrum> LoadMs1Spectrums() => _spectra.Where(s => s.MsLevel == 1).ToList().AsReadOnly();
            public ReadOnlyCollection<RawSpectrum> LoadMsNSpectrums(int level) => _spectra.Where(s => s.MsLevel == level).ToList().AsReadOnly();
            public Task<ReadOnlyCollection<RawSpectrum>> LoadMsSpectrumsAsync(CancellationToken token) => Task.FromResult(LoadMsSpectrums());
            public Task<ReadOnlyCollection<RawSpectrum>> LoadMs1SpectrumsAsync(CancellationToken token) => Task.FromResult(LoadMs1Spectrums());
            public Task<ReadOnlyCollection<RawSpectrum>> LoadMsNSpectrumsAsync(int level, CancellationToken token) => Task.FromResult(LoadMsNSpectrums(level));
        }
    }
}
