using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.CommonMVVM;
using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Chart
{
    class RawDecSpectrumsModel : BindableBase
    {
        public RawDecSpectrumsModel(
            IMsSpectrumLoader<ChromatogramPeakFeatureModel> rawLoader,
            IMsSpectrumLoader<ChromatogramPeakFeatureModel> decLoader,
            IMsSpectrumLoader<ChromatogramPeakFeatureModel> refLoader,
            Func<SpectrumPeak, double> horizontalSelector,
            Func<SpectrumPeak, double> verticalSelector)
            : this(
              Observable.Return<ChromatogramPeakFeatureModel>(null),
              rawLoader, decLoader, refLoader,
              horizontalSelector, verticalSelector) {

        }

        public RawDecSpectrumsModel(
            IObservable<ChromatogramPeakFeatureModel> targetSource,
            IMsSpectrumLoader<ChromatogramPeakFeatureModel> rawLoader,
            IMsSpectrumLoader<ChromatogramPeakFeatureModel> decLoader,
            IMsSpectrumLoader<ChromatogramPeakFeatureModel> refLoader,
            Func<SpectrumPeak, double> horizontalSelector,
            Func<SpectrumPeak, double> verticalSelector) {

            RawLoader = rawLoader;
            DecLoader = decLoader;
            RefLoader = refLoader;

            var rawSource = targetSource.Select(target => rawLoader.LoadSpectrum(target));
            var decSource = targetSource.Select(target => decLoader.LoadSpectrum(target));
            var refSource = targetSource.Select(target => refLoader.LoadSpectrum(target));

            RawRefSpectrumModels = new MsSpectrumModel(rawSource, refSource, horizontalSelector, verticalSelector);
            DecRefSpectrumModels = new MsSpectrumModel(decSource, refSource, horizontalSelector, verticalSelector);
        }

        public IMsSpectrumLoader<ChromatogramPeakFeatureModel> RawLoader { get; }

        public IMsSpectrumLoader<ChromatogramPeakFeatureModel> DecLoader { get; }

        public IMsSpectrumLoader<ChromatogramPeakFeatureModel> RefLoader { get; }

        public MsSpectrumModel RawRefSpectrumModels {
            get => rawRefSpectrumModels;
            set => SetProperty(ref rawRefSpectrumModels, value);
        }
        private MsSpectrumModel rawRefSpectrumModels;

        public MsSpectrumModel DecRefSpectrumModels {
            get => decRefSpectrumModels;
            set => SetProperty(ref decRefSpectrumModels, value);
        }
        private MsSpectrumModel decRefSpectrumModels;

        public string GraphTitle {
            get => graphTitle;
            set {
                if (SetProperty(ref graphTitle, value)) {
                    RawRefSpectrumModels.GraphTitle = value;
                    DecRefSpectrumModels.GraphTitle = value;
                }
            }
        }
        private string graphTitle;

        public string HorizontalTitle {
            get => horizontalTitle;
            set {
                if (SetProperty(ref horizontalTitle, value)) {
                    RawRefSpectrumModels.HorizontalTitle = value;
                    DecRefSpectrumModels.HorizontalTitle = value;
                }
            }
        }
        private string horizontalTitle;

        public string VerticalTitle {
            get => verticalTitle;
            set {
                if (SetProperty(ref verticalTitle, value)) {
                    RawRefSpectrumModels.VerticalTitle = value;
                    DecRefSpectrumModels.VerticalTitle = value;
                }
            }
        }
        private string verticalTitle;

        public string HorizontaProperty {
            get => horizontalProperty;
            set {
                if (SetProperty(ref horizontalProperty, value)) {
                    RawRefSpectrumModels.HorizontalProperty = value;
                    DecRefSpectrumModels.HorizontalProperty = value;
                }
            }
        }
        private string horizontalProperty;

        public string VerticalProperty {
            get => verticalProperty;
            set {
                if (SetProperty(ref verticalProperty, value)) {
                    RawRefSpectrumModels.VerticalProperty = value;
                    DecRefSpectrumModels.VerticalProperty = value;
                }
            }
        }
        private string verticalProperty;

        public string LabelProperty {
            get => labelProperty;
            set {
                if (SetProperty(ref labelProperty, value)) {
                    RawRefSpectrumModels.LabelProperty = value;
                    DecRefSpectrumModels.LabelProperty = value;
                }
            }
        }
        private string labelProperty;

        public string OrderingProperty {
            get => orderingProperty;
            set {
                if (SetProperty(ref orderingProperty, value)) {
                    RawRefSpectrumModels.OrderingProperty = value;
                    DecRefSpectrumModels.OrderingProperty = value;
                }
            }
        }
        private string orderingProperty;

        public async Task LoadSpectrumAsync(ChromatogramPeakFeatureModel target, CancellationToken token) {
            var spectrums = await Task.WhenAll(
                RawLoader.LoadSpectrumAsync(target, token),
                DecLoader.LoadSpectrumAsync(target, token),
                RefLoader.LoadSpectrumAsync(target, token));

            RawRefSpectrumModels.UpperSpectrum = spectrums[0];
            DecRefSpectrumModels.UpperSpectrum = spectrums[1];
            RawRefSpectrumModels.LowerSpectrum = DecRefSpectrumModels.LowerSpectrum = spectrums[2];
        }
    }
}
