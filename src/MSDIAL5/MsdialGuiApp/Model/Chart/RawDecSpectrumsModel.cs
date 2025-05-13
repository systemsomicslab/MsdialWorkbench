using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Model.MsResult;
using CompMs.Common.Algorithm.Scoring;
using CompMs.CommonMVVM;
using Reactive.Bindings.Extensions;
using System;
using System.Linq;

namespace CompMs.App.Msdial.Model.Chart
{
    internal sealed class RawDecSpectrumsModel : DisposableModelBase
    {
        public RawDecSpectrumsModel(SingleSpectrumModel rawSpectrumModel, SingleSpectrumModel? q1DecSpectrumModel, SingleSpectrumModel decSpectrumModel, SingleSpectrumModel referenceSpectrumModel, IObservable<Ms2ScanMatching?> ms2ScanMatching, IMultiMsmsSpectrumLoader<ChromatogramPeakFeatureModel>?[]? loaders) {
            if (rawSpectrumModel is null) {
                throw new ArgumentNullException(nameof(rawSpectrumModel));
            }

            if (decSpectrumModel is null) {
                throw new ArgumentNullException(nameof(decSpectrumModel));
            }

            if (referenceSpectrumModel is null) {
                throw new ArgumentNullException(nameof(referenceSpectrumModel));
            }

            if (ms2ScanMatching is null) {
                throw new ArgumentNullException(nameof(ms2ScanMatching));
            }

            RawRefSpectrumModels = new MsSpectrumModel(rawSpectrumModel, referenceSpectrumModel, ms2ScanMatching)
            {
                GraphTitle = "Measure vs. Reference",
                HorizontalTitle = "m/z",
                VerticalTitle = "Relative abundance",
            }.AddTo(Disposables);
            if (q1DecSpectrumModel is not null) {
                Q1DecRefSpectrumModels = new MsSpectrumModel(q1DecSpectrumModel, referenceSpectrumModel, ms2ScanMatching)
                {
                    GraphTitle = "Q1 Deconvolution vs. Reference",
                    HorizontalTitle = "m/z",
                    VerticalTitle = "Relative abundance",
                }.AddTo(Disposables);
            }
            DecRefSpectrumModels = new MsSpectrumModel(decSpectrumModel, referenceSpectrumModel, ms2ScanMatching)
            {
                GraphTitle =  "Deconvolution vs. Reference",
                HorizontalTitle = "m/z",
                VerticalTitle = "Relative abundacne",
            }.AddTo(Disposables);

            RawLoader = loaders?.ElementAtOrDefault(0);
            Q1DecLoader = loaders?.ElementAtOrDefault(1);
        }

        public MsSpectrumModel RawRefSpectrumModels { get; }
        public MsSpectrumModel? Q1DecRefSpectrumModels { get; }
        public MsSpectrumModel DecRefSpectrumModels { get; }
        public IMultiMsmsSpectrumLoader<ChromatogramPeakFeatureModel>? RawLoader { get; }
        public IMultiMsmsSpectrumLoader<ChromatogramPeakFeatureModel>? Q1DecLoader { get; }

        public ProductIonIntensityMapModel? ProductIonIntensityMapModel { get; set; }
    }
}
