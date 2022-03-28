using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.View.PeakCuration;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Chart
{
    class AlignmentEicModel : DisposableModelBase
    {
        public AlignmentEicModel(
            IObservable<AlignmentSpotPropertyModel> model,
            IObservable<List<Chromatogram>> chromatoramSource,
            List<AnalysisFileBean> analysisFiles,
            ParameterBase parameter,
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

            var legacymodel = model.Where(model_ => model_ != null).CombineLatest(
                chromatoramSource.Where(chromatogram => chromatogram != null && chromatogram.Count > 0),
                (model_, chromatogram) => new AlignedChromatogramModificationModelLegacy(model_, chromatogram, analysisFiles, parameter));
            AlignedChromatogramModificationModel = legacymodel;
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

        public IObservable<AlignedChromatogramModificationModelLegacy> AlignedChromatogramModificationModel { get; }

        public static AlignmentEicModel Create(
            IObservable<AlignmentSpotPropertyModel> source,
            AlignmentEicLoader loader,
            List<AnalysisFileBean> AnalysisFiles,
            ParameterBase Param,
            Func<PeakItem, double> horizontalSelector,
            Func<PeakItem, double> verticalSelector) {

            return new AlignmentEicModel(
                source,
                source.Select(loader.LoadEicAsObservable).Switch(),
                AnalysisFiles,
                Param,
                horizontalSelector, verticalSelector
            );
        }
    }
}