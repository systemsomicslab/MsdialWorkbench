using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Utility;
using CompMs.App.Msdial.View.PeakCuration;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Chart
{
    internal sealed class AlignmentEicModel : DisposableModelBase
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

            EicChromatograms = chromatoramSource.ToReadOnlyReactivePropertySlim().AddTo(Disposables); ;
            var eicChromatograms = chromatoramSource.Throttle(TimeSpan.FromSeconds(.05d)).ToReactiveProperty().AddTo(Disposables);

            var peaksox = eicChromatograms
                .Select(chroms => chroms?.SelectMany(chrom => chrom.Peaks).ToArray() ?? new PeakItem[0]);

            var nopeak = peaksox.Where(peaks => !peaks.Any()).ToConstant(new Range(0, 1));

            var anypeak = peaksox.Where(peaks => peaks.Any());
            var hrox = anypeak
                .Select(peaks => new Range(peaks.Min(horizontalSelector), peaks.Max(horizontalSelector)));
            var vrox = anypeak
                .Select(peaks => new Range(peaks.Min(verticalSelector), peaks.Max(verticalSelector)));

            HorizontalRange = hrox.Merge(nopeak).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            VerticalRange = vrox.Merge(nopeak).ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            var isSelected = model.Select(m => m != null).ToReactiveProperty().AddTo(Disposables);
            IsSelected = isSelected;
            var isLoaded = model.SkipNull().SelectSwitch(m => m.AlignedPeakPropertiesModelProperty).Select(props => props?.Any() ?? false);
            IsPeakLoaded = new[]
            {
                isSelected,
                isLoaded,
            }.CombineLatestValuesAreAllTrue()
            .ToReactiveProperty().AddTo(Disposables);

            var modelAndChromatogram = model.CombineLatest(eicChromatograms).ToReactiveProperty().AddTo(Disposables);
            CanShow = modelAndChromatogram.Select(mc =>
                new[]
                {
                    mc.First?.AlignedPeakPropertiesModelProperty.Select(features => features?.Any() ?? false)
                        ?? Observable.Return(false),
                    Observable.Return(mc.Second?.Any() ?? false),
                }.CombineLatestValuesAreAllTrue().StartWith(false)
            ).Switch().ToReactiveProperty().AddTo(Disposables);

            SampleTableViewerInAlignmentModelLegacy = new SampleTableViewerInAlignmentModelLegacy(model, eicChromatograms, analysisFiles, parameter);
            AlignedChromatogramModificationModelLegacy = new AlignedChromatogramModificationModelLegacy(model, eicChromatograms, analysisFiles, parameter);
        }

        public IObservable<bool> CanShow { get; }
        public IObservable<bool> IsSelected { get; }
        public IObservable<bool> IsPeakLoaded { get; }

        public ReadOnlyReactivePropertySlim<List<Chromatogram>> EicChromatograms { get; }
        public ReadOnlyReactivePropertySlim<Range> HorizontalRange { get; }
        public ReadOnlyReactivePropertySlim<Range> VerticalRange { get; }

        public GraphElements Elements { get; } = new GraphElements();

        public SampleTableViewerInAlignmentModelLegacy SampleTableViewerInAlignmentModelLegacy { get; }
        public AlignedChromatogramModificationModelLegacy AlignedChromatogramModificationModelLegacy { get; }

        public static AlignmentEicModel Create(
            IObservable<AlignmentSpotPropertyModel> source,
            AlignmentEicLoader loader,
            List<AnalysisFileBean> AnalysisFiles,
            ParameterBase Param,
            Func<PeakItem, double> horizontalSelector,
            Func<PeakItem, double> verticalSelector) {

            return new AlignmentEicModel(
                source,
                source.SelectSwitch(loader.LoadEicAsObservable),
                AnalysisFiles,
                Param,
                horizontalSelector, verticalSelector
            );
        }
    }
}