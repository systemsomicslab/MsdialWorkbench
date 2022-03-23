using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.View.PeakCuration;
using CompMs.App.Msdial.ViewModel.PeakCuration;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialCore.Utility;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Chart
{
    class AlignmentEicModel : DisposableModelBase
    {
        public AlignmentEicModel(
            IObservable<AlignmentSpotPropertyModel> model,
            IObservable<List<Chromatogram>> chromatoramSource,
            List<AnalysisFileBean> AnalysisFiles,
            ParameterBase Param,
            Func<PeakItem, double> horizontalSelector,
            Func<PeakItem, double> verticalSelector) {
            
            if (model is null) {
                throw new ArgumentNullException(nameof(model));
            }

            if (chromatoramSource is null) {
                throw new ArgumentNullException(nameof(chromatoramSource));
            }

            if (horizontalSelector is null) {
                throw new ArgumentNullException(nameof(horizontalSelector));
            }

            if (verticalSelector is null) {
                throw new ArgumentNullException(nameof(verticalSelector));
            }

            AlignmentSpotPropertyModel = model;

            EicChromatogramsSource = chromatoramSource;
            EicChromatogramsSource.Subscribe(chromatograms => EicChromatograms = chromatograms);

            HorizontalSelector = horizontalSelector;
            VerticalSelector = verticalSelector;

            var peaksox = EicChromatogramsSource
                .Select(chroms => chroms.SelectMany(chrom => chrom.Peaks).ToArray());

            var nopeak = peaksox.Where(peaks => !peaks.Any()).Select(_ => new Range(0, 1));

            var anypeak = peaksox.Where(peaks => peaks.Any());
            var hrox = anypeak
                .Select(peaks => new Range(peaks.Min(HorizontalSelector), peaks.Max(HorizontalSelector)));
            var vrox = anypeak
                .Select(peaks => new Range(peaks.Min(VerticalSelector), peaks.Max(VerticalSelector)));

            HorizontalRangeSource = hrox.Merge(nopeak).ToReadOnlyReactivePropertySlim();
            VerticalRangeSource = vrox.Merge(nopeak).ToReadOnlyReactivePropertySlim();

            var legacyvm = new AlignedChromatogramModificationViewModelLegacy();
            var legacymodel = 
                model.CombineLatest(chromatoramSource).
                Where(n => n.First != null && n.Second != null && n.Second.Count > 0).
                Select((c) => new AlignedChromatogramModificationModelLegacy(c.First, c.Second, AnalysisFiles, Param));
            legacymodel.Subscribe((m) => legacyvm.UpdateModel(m)).AddTo(Disposables);

            click = new Subject<Unit>();
            // click.WithLatestFrom(model, (a, b) => b).WithLatestFrom(chromatoramSource).Subscribe((c) => CreateAlignedPeakCorrectionWinLegacy(c.First, c.Second, null, null));
            click.Subscribe((c) => CreateAlignedPeakCorrectionWinLegacy(legacyvm));
        }

        public List<Chromatogram> EicChromatograms {
            get => eicChromatogram;
            set {
                if (SetProperty(ref eicChromatogram, value)) {
                    OnPropertyChanged(nameof(HorizontalRange));
                    OnPropertyChanged(nameof(VerticalRange));
                }
            }
        }
        private List<Chromatogram> eicChromatogram;

        public IObservable<AlignmentSpotPropertyModel> AlignmentSpotPropertyModel { get; set; }

        public Range HorizontalRange {
            get {
                if (EicChromatograms.Any() && HorizontalSelector != null) {
                    var minimum = EicChromatograms
                        .SelectMany(chrom => chrom.Peaks)
                        .Select(HorizontalSelector)
                        .DefaultIfEmpty().Min();
                    var maximum = EicChromatograms
                        .SelectMany(chrom => chrom.Peaks)
                        .Select(HorizontalSelector)
                        .DefaultIfEmpty().Max();
                    return new Range(minimum, maximum);
                }
                return new Range(0, 1);
            }
        }

        public Range VerticalRange {
            get {
                if (EicChromatograms.Any() && VerticalSelector != null) {
                    var minimum = EicChromatograms
                        .SelectMany(chrom => chrom.Peaks)
                        .Select(VerticalSelector)
                        .DefaultIfEmpty().Min();
                    var maximum = EicChromatograms
                        .SelectMany(chrom => chrom.Peaks)
                        .Select(VerticalSelector)
                        .DefaultIfEmpty().Max();
                    return new Range(minimum, maximum);
                }
                return new Range(0, 1);
            }
        }

        public IObservable<List<Chromatogram>> EicChromatogramsSource { get; }

        public IObservable<Range> HorizontalRangeSource { get; }

        public IObservable<Range> VerticalRangeSource { get; }


        public GraphElements Elements { get; } = new GraphElements();

        public Func<PeakItem, double> HorizontalSelector { get; }

        public Func<PeakItem, double> VerticalSelector { get; }

        public static AlignmentEicModel Create(
            IObservable<AlignmentSpotPropertyModel> source,
            AlignmentEicLoader loader,
            List<AnalysisFileBean> AnalysisFiles,
            ParameterBase Param,
            Func<PeakItem, double> horizontalSelector,
            Func<PeakItem, double> verticalSelector) {

            return new AlignmentEicModel(
                source,
                source.Select(loader.LoadEic),
                AnalysisFiles, 
                Param,
                horizontalSelector, verticalSelector
            );
            /*
            return new AlignmentEicModel(
                source.SelectMany(src =>
                    Observable.DeferAsync(async token => {
                        var result = await loader.LoadEicAsync(src, token);
                        return Observable.Return(result);
                    })),
                horizontalSelector, verticalSelector
            );
            */
        }

        private readonly Subject<Unit> click;
        public void Click() {
            click.OnNext(Unit.Default);
        }

        private static void CreateAlignedPeakCorrectionWinLegacy(
            AlignedChromatogramModificationViewModelLegacy vm) {
            System.Windows.Application.Current.Dispatcher.Invoke(() => 
                {
                    var window = new AlignedPeakCorrectionWinLegacy(vm);
                    window.Show();
                }
            );
        }
    }

    class AlignmentEicLoader
    {
        public AlignmentEicLoader(
            ChromatogramSerializer<ChromatogramSpotInfo> chromatogramSpotSerializer,
            string eicFile,
            PeakPickBaseParameter peakPickParameter, IReadOnlyDictionary<int, string> id2cls) {

            this.chromatogramSpotSerializer = chromatogramSpotSerializer;
            this.eicFile = eicFile;
            this.id2cls = id2cls;
            this.peakPickParameter = peakPickParameter;
        }

        private readonly ChromatogramSerializer<ChromatogramSpotInfo> chromatogramSpotSerializer;
        private readonly string eicFile;
        private readonly IReadOnlyDictionary<int, string> id2cls;
        private readonly PeakPickBaseParameter peakPickParameter;

        public async Task<List<Chromatogram>> LoadEicAsync(AlignmentSpotPropertyModel target, CancellationToken token) {
            var eicChromatograms = new List<Chromatogram>();
            if (target != null) {
                // maybe using file pointer is better
                eicChromatograms = await Task.Run(() => {
                    var spotinfo = chromatogramSpotSerializer.DeserializeAtFromFile(eicFile, target.MasterAlignmentID);
                    var chroms = new List<Chromatogram>(spotinfo.PeakInfos.Count);
                    foreach (var peakinfo in spotinfo.PeakInfos) {
                        var items = DataAccess.GetSmoothedPeaklist(peakinfo.Chromatogram, peakPickParameter.SmoothingMethod, peakPickParameter.SmoothingLevel).Select(chrom => new PeakItem(chrom)).ToList();
                        var peakitems = items.Where(item => peakinfo.ChromXsLeft.Value <= item.Time && item.Time <= peakinfo.ChromXsRight.Value).ToList();
                        chroms.Add(new Chromatogram
                        {
                            Class = id2cls[peakinfo.FileID],
                            Peaks = items,
                            PeakArea = peakitems,
                        });
                    }
                    return chroms;
                }, token).ConfigureAwait(false);
            }
            return eicChromatograms;
        }

        public List<Chromatogram> LoadEic(AlignmentSpotPropertyModel target) {
            var eicChromatograms = new List<Chromatogram>();
            if (target != null) {
                // maybe using file pointer is better
                var spotinfo = chromatogramSpotSerializer.DeserializeAtFromFile(eicFile, target.MasterAlignmentID);
                eicChromatograms = new List<Chromatogram>(spotinfo.PeakInfos.Count);
                foreach (var peakinfo in spotinfo.PeakInfos) {
                    var items = peakinfo.Chromatogram.Select(chrom => new PeakItem(chrom)).ToList();
                    var peakitems = items.Where(item => peakinfo.ChromXsLeft.Value <= item.Time && item.Time <= peakinfo.ChromXsRight.Value).ToList();
                    eicChromatograms.Add(new Chromatogram
                    {
                        Class = id2cls[peakinfo.FileID],
                        Peaks = items,
                        PeakArea = peakitems,
                    });
                }
            }
            return eicChromatograms;
        }
    }
}
