using CompMs.App.Msdial.Model.Loader;
using CompMs.Common.Algorithm.Scoring;
using CompMs.CommonMVVM;
using Reactive.Bindings.Extensions;
using System;

namespace CompMs.App.Msdial.Model.Chart
{
    internal sealed class RawDecSpectrumsModel : DisposableModelBase
    {
        public RawDecSpectrumsModel(MsSpectrumModel rawRefSpectrumModels, MsSpectrumModel decRefSpectrumModels, MultiMsmsRawSpectrumLoader loader) {
            RawRefSpectrumModels = rawRefSpectrumModels ?? throw new ArgumentNullException(nameof(rawRefSpectrumModels));
            DecRefSpectrumModels = decRefSpectrumModels ?? throw new ArgumentNullException(nameof(decRefSpectrumModels));
            RawLoader = loader;
        }

        public RawDecSpectrumsModel(SingleSpectrumModel rawSpectrumModel, SingleSpectrumModel decSpectrumModel, SingleSpectrumModel referenceSpectrumModel, IObservable<Ms2ScanMatching?> ms2ScanMatching, MultiMsmsRawSpectrumLoader? loader = null) {
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
            DecRefSpectrumModels = new MsSpectrumModel(decSpectrumModel, referenceSpectrumModel, ms2ScanMatching)
            {
                GraphTitle =  "Deconvolution vs. Reference",
                HorizontalTitle = "m/z",
                VerticalTitle = "Relative abundacne",
            }.AddTo(Disposables);
            RawLoader = loader;
        }

        public MsSpectrumModel RawRefSpectrumModels { get; }
        public MsSpectrumModel DecRefSpectrumModels { get; }
        public MultiMsmsRawSpectrumLoader? RawLoader { get; }
    }
}
