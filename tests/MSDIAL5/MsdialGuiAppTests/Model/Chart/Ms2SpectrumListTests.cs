using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
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
        public void SpectraAreScaledToTheirBasePeak() {
            var spectrum = new MsSpectrum(Peaks((200, 50), (100, 200), (300, 0)));

            var relative = Ms2SpectrumList.ToRelativeSpectrum(spectrum);
            CollectionAssert.AreEqual(new[] { 100d, 200d, 300d }, relative.Select(p => p.Mass).ToArray());
            CollectionAssert.AreEqual(new[] { 100d, 25d, 0d }, relative.Select(p => p.Intensity).ToArray());

            var mirrored = Ms2SpectrumList.ToRelativeSpectrum(spectrum, mirrored: true);
            CollectionAssert.AreEqual(new[] { -100d, -25d, -0d }, mirrored.Select(p => p.Intensity).ToArray());

            Assert.AreEqual(0, Ms2SpectrumList.ToRelativeSpectrum(new MsSpectrum(Peaks((100, 0)))).Count);
            Assert.AreEqual(0, Ms2SpectrumList.ToRelativeSpectrum(null).Count);
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
            CollectionAssert.AreEqual(new[] { 50d, 100d }, vm.Tiles.Value[1].Peaks.Select(p => p.Intensity).ToArray());
            Assert.AreEqual(0d, vm.Tiles.Value[0].VerticalAxis.Range.Minimum.Value);

            vm.ShowReference.Value = true;
            CollectionAssert.AreEqual(new[] { -100d, -50d }, vm.Tiles.Value[0].ReferencePeaks.Select(p => p.Intensity).ToArray());
            Assert.IsTrue(vm.Tiles.Value[0].VerticalAxis.Range.Minimum.Value < 0d, "The mirrored reference needs the lower half of the axis.");

            reference.Value = null;
            Assert.IsFalse(vm.HasReference.Value);
            Assert.IsTrue(vm.Tiles.Value.All(tile => tile.ReferencePeaks.Count == 0));
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
