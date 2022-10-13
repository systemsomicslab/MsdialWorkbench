using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
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

            EicChromatograms = chromatoramSource.ToReadOnlyReactivePropertySlim().AddTo(Disposables); ;

            var peaksox = EicChromatograms
                .Select(chroms => chroms.SelectMany(chrom => chrom.Peaks).ToArray());

            var nopeak = peaksox.Where(peaks => !peaks.Any()).Select(_ => new Range(0, 1));

            var anypeak = peaksox.Where(peaks => peaks.Any());
            var hrox = anypeak
                .Select(peaks => new Range(peaks.Min(horizontalSelector), peaks.Max(horizontalSelector)));
            var vrox = anypeak
                .Select(peaks => new Range(peaks.Min(verticalSelector), peaks.Max(verticalSelector)));

            HorizontalRange = hrox.Merge(nopeak).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            VerticalRange = vrox.Merge(nopeak).ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            var alignedChromatogramModificationModel = model.Where(model_ => model_ != null)
                .Select(model_ => model_.AlignedPeakPropertiesModelAsObservable.Where(props => props?.Any() ?? false).Select(_ => model_))
                .Switch()
                .CombineLatest(
                    EicChromatograms.Where(chromatogram => chromatogram != null && chromatogram.Count > 0),
                    (model_, chromatogram) => new AlignedChromatogramModificationModelLegacy(model_, chromatogram, analysisFiles, parameter));
            AlignedChromatogramModificationModel = alignedChromatogramModificationModel;

            var sampleTableViewerInAlignmentModelLegacy = model.Where(model_ => model_ != null)
                .Select(model_ => model_.AlignedPeakPropertiesModelAsObservable.Where(props => props?.Any() ?? false).Select(_ => model_))
                .Switch()
                .CombineLatest(
                    EicChromatograms.Where(chromatogram => chromatogram != null && chromatogram.Count > 0),
                    (model_, chromatogram) => new SampleTableViewerInAlignmentModelLegacy(model_, chromatogram, analysisFiles, parameter));
            SampleTableViewerInAlignmentModel = sampleTableViewerInAlignmentModelLegacy;
        }

        public ReadOnlyReactivePropertySlim<List<Chromatogram>> EicChromatograms { get; }
        public ReadOnlyReactivePropertySlim<Range> HorizontalRange { get; }
        public ReadOnlyReactivePropertySlim<Range> VerticalRange { get; }

        public GraphElements Elements { get; } = new GraphElements();

        public IObservable<AlignedChromatogramModificationModelLegacy> AlignedChromatogramModificationModel { get; }
        public IObservable<SampleTableViewerInAlignmentModelLegacy> SampleTableViewerInAlignmentModel { get; }

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