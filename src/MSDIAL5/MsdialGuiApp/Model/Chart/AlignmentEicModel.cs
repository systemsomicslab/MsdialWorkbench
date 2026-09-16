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
            IObservable<AlignedChromatograms?> spotChromatograms,
            List<AnalysisFileBean> analysisFiles,
            ParameterBase parameter,
            FilePropertiesModel filePropertiesModel,
            Func<PeakItem, double> horizontalSelector,
            Func<PeakItem, double> verticalSelector) {

            if (spotChromatograms is null) {
                throw new ArgumentNullException(nameof(spotChromatograms));
            }

            if (horizontalSelector is null) {
                throw new ArgumentNullException(nameof(horizontalSelector));
            }

            if (verticalSelector is null) {
                throw new ArgumentNullException(nameof(verticalSelector));
            }

            var spotChromatogramsHot = spotChromatograms.Publish();
            var chromatogramSource = spotChromatogramsHot.DefaultIfNull(s => s.Chromatograms, Observable.Return(new List<PeakChromatogram>(0))).Switch();
            EicChromatograms = chromatogramSource.ToReadOnlyReactivePropertySlim().AddTo(Disposables); ;

            var peaksox = EicChromatograms
                .Select(chroms => chroms?.SelectMany(chrom => chrom.Peaks).ToArray() ?? new PeakItem[0]);

            var nopeak = peaksox.Where(peaks => !peaks.Any()).ToConstant(new AxisRange(0, 1));

            var anypeak = peaksox.Where(peaks => peaks.Any());
            var hrox = anypeak
                .Select(peaks => new AxisRange(peaks.Min(horizontalSelector), peaks.Max(horizontalSelector)));
            var vrox = anypeak
                .Select(peaks => new AxisRange(peaks.Min(verticalSelector), peaks.Max(verticalSelector)));

            HorizontalRange = hrox.Merge(nopeak).ToReadOnlyReactivePropertySlim(new AxisRange(0d, 1d)).AddTo(Disposables);
            VerticalRange = vrox.Merge(nopeak).ToReadOnlyReactivePropertySlim(new AxisRange(0d, 1d)).AddTo(Disposables);

            var isSelected = spotChromatogramsHot.Select(m => m is not null).ToReactiveProperty().AddTo(Disposables);
            IsSelected = isSelected;
            var isLoaded = spotChromatogramsHot.SkipNull().SelectSwitch(m => m.Spot.AlignedPeakPropertiesModelProperty).Select(props => props?.Any() ?? false);
            IsPeakLoaded = new[]
            {
                isSelected,
                isLoaded,
            }.CombineLatestValuesAreAllTrue()
            .ToReactiveProperty().AddTo(Disposables);

            CanShow = spotChromatogramsHot.DefaultIfNull(
                s => new[] {
                    s.Spot.AlignedPeakPropertiesModelProperty.Select(features => features?.Any() ?? false),
                    s.Chromatograms.Select(c => c.Any()),
                }.CombineLatestValuesAreAllTrue().StartWith(false), Observable.Return(false))
                .Switch().ToReactiveProperty().AddTo(Disposables);

            SampleTableViewerInAlignmentModelLegacy = new SampleTableViewerInAlignmentModelLegacy(spotChromatogramsHot, analysisFiles, parameter, filePropertiesModel).AddTo(Disposables);
            AlignedChromatogramModificationModelLegacy = new AlignedChromatogramModificationModelLegacy(spotChromatogramsHot, analysisFiles, parameter, filePropertiesModel).AddTo(Disposables);

            Disposables.Add(spotChromatogramsHot.Connect());
        }

        public IObservable<bool> CanShow { get; }
        public IObservable<bool> IsSelected { get; }
        public IObservable<bool> IsPeakLoaded { get; }

        public ReadOnlyReactivePropertySlim<List<PeakChromatogram>> EicChromatograms { get; }
        public ReadOnlyReactivePropertySlim<AxisRange> HorizontalRange { get; }
        public ReadOnlyReactivePropertySlim<AxisRange> VerticalRange { get; }

        public GraphElements Elements { get; } = new GraphElements();

        public SampleTableViewerInAlignmentModelLegacy SampleTableViewerInAlignmentModelLegacy { get; }
        public AlignedChromatogramModificationModelLegacy AlignedChromatogramModificationModelLegacy { get; }

        public static AlignmentEicModel Create(
            IObservable<AlignmentSpotPropertyModel?> source,
            AlignmentEicLoader loader,
            List<AnalysisFileBean> AnalysisFiles,
            ParameterBase Param,
            FilePropertiesModel filePropertiesModel,
            Func<PeakItem, double> horizontalSelector,
            Func<PeakItem, double> verticalSelector) {

            return new AlignmentEicModel(
                source.DefaultIfNull(s => new AlignedChromatograms(s, loader.LoadEicAsObservable(s))),
                AnalysisFiles,
                Param,
                filePropertiesModel,
                horizontalSelector,
                verticalSelector);
        }
    }
}