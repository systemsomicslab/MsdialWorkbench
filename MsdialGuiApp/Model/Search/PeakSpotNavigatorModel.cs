using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Search
{
    public sealed class PeakSpotNavigatorModel : DisposableModelBase
    {
        public PeakSpotNavigatorModel(IReadOnlyList<IFilterable> peakSpots, PeakFilterModel peakFilterModel, IMatchResultEvaluator<MsScanMatchResult> evaluator, bool useRtFilter = false, bool useDtFilter = false) {
            PeakSpots = peakSpots ?? throw new System.ArgumentNullException(nameof(peakSpots));
            PeakFilterModel = peakFilterModel ?? throw new System.ArgumentNullException(nameof(peakFilterModel));
            this.evaluator = evaluator ?? throw new System.ArgumentNullException(nameof(evaluator));
            UseRtFilter = useRtFilter;
            UseDtFilter = useDtFilter;
            AmplitudeLowerValue = 0d;
            AmplitudeUpperValue = 1d;
            MzLowerValue = peakSpots.DefaultIfEmpty().Min(p => p?.Mass) ?? 0d;
            MzUpperValue = peakSpots.DefaultIfEmpty().Max(p => p?.Mass) ?? 1d;
            RtLowerValue = peakSpots.DefaultIfEmpty().Min(p => p?.ChromXs.RT.Value) ?? 0d;
            RtUpperValue = peakSpots.DefaultIfEmpty().Max(p => p?.ChromXs.RT.Value) ?? 1d;
            DtLowerValue = peakSpots.DefaultIfEmpty().Min(p => p?.ChromXs.Drift.Value) ?? 0d;
            DtUpperValue = peakSpots.DefaultIfEmpty().Max(p => p?.ChromXs.Drift.Value) ?? 1d;
            if (peakSpots is INotifyCollectionChanged notifyCollection) {
                notifyCollection.CollectionChangedAsObservable()
                    .Throttle(TimeSpan.FromSeconds(.1d))
                    .Subscribe(_ =>
                    {
                        MzLowerValue = peakSpots.DefaultIfEmpty().Min(p => p?.Mass) ?? 0d;
                        MzUpperValue = peakSpots.DefaultIfEmpty().Max(p => p?.Mass) ?? 1d;
                        RtLowerValue = peakSpots.DefaultIfEmpty().Min(p => p?.ChromXs.RT.Value) ?? 0d;
                        RtUpperValue = peakSpots.DefaultIfEmpty().Max(p => p?.ChromXs.RT.Value) ?? 1d;
                        DtLowerValue = peakSpots.DefaultIfEmpty().Min(p => p?.ChromXs.Drift.Value) ?? 0d;
                        DtUpperValue = peakSpots.DefaultIfEmpty().Max(p => p?.ChromXs.Drift.Value) ?? 1d;
                    }).AddTo(Disposables);
            }
            metaboliteFilterKeywords = new List<string>();
            MetaboliteFilterKeywords = metaboliteFilterKeywords.AsReadOnly();
            proteinFilterKeywords = new List<string>();
            ProteinFilterKeywords = proteinFilterKeywords.AsReadOnly();
            commentFilterKeywords = new List<string>();
            CommentFilterKeywords = commentFilterKeywords.AsReadOnly();
        }

        public string SelectedAnnotationLabel {
            get => selectedAnnotationLabel;
            set => SetProperty(ref selectedAnnotationLabel, value);
        }
        private string selectedAnnotationLabel;

        public IReadOnlyList<IFilterable> PeakSpots { get; }

        public double AmplitudeLowerValue {
            get => amplitudeLowerValue;
            set => SetProperty(ref amplitudeLowerValue, value);
        }
        private double amplitudeLowerValue;
        public double AmplitudeUpperValue { 
            get => amplitudeUpperValue;
            set => SetProperty(ref amplitudeUpperValue, value);
        }
        private double amplitudeUpperValue;

        public double MzLowerValue {
            get => mzLowerValue;
            set => SetProperty(ref mzLowerValue, value);
        }
        private double mzLowerValue;
        public double MzUpperValue { 
            get => mzUpperValue;
            set => SetProperty(ref mzUpperValue, value);
        }
        private double mzUpperValue;

        public bool UseRtFilter { get; }
        public double RtLowerValue {
            get => rtLowerValue;
            set => SetProperty(ref rtLowerValue, value);
        }
        private double rtLowerValue;
        public double RtUpperValue { 
            get => rtUpperValue;
            set => SetProperty(ref rtUpperValue, value);
        }
        private double rtUpperValue;

        public bool UseDtFilter { get; }
        public double DtLowerValue {
            get => dtLowerValue;
            set => SetProperty(ref dtLowerValue, value);
        }
        private double dtLowerValue;
        public double DtUpperValue { 
            get => dtUpperValue;
            set => SetProperty(ref dtUpperValue, value);
        }
        private double dtUpperValue;

        private readonly IMatchResultEvaluator<MsScanMatchResult> evaluator;

        public ReadOnlyCollection<string> MetaboliteFilterKeywords { get; }
        private readonly List<string> metaboliteFilterKeywords;

        private readonly SemaphoreSlim metaboliteSemaphore = new SemaphoreSlim(1, 1);
        public async Task SetMetaboliteKeywordsAsync(IEnumerable<string> keywords, CancellationToken token) {
            token.ThrowIfCancellationRequested();
            await metaboliteSemaphore.WaitAsync().ConfigureAwait(false);
            try {
                token.ThrowIfCancellationRequested();
                SetMetaboliteKeywords(keywords);
            }
            finally {
                metaboliteSemaphore.Release();
            }
        }

        private void SetMetaboliteKeywords(IEnumerable<string> keywords) {
            metaboliteFilterKeywords.Clear();
            metaboliteFilterKeywords.AddRange(keywords);
        }

        public ReadOnlyCollection<string> ProteinFilterKeywords { get; }
        private readonly List<string> proteinFilterKeywords;

        private readonly SemaphoreSlim proteinSemaphore = new SemaphoreSlim(1, 1);
        public async Task SetProteinKeywordsAsync(IEnumerable<string> keywords, CancellationToken token) {
            token.ThrowIfCancellationRequested();
            await proteinSemaphore.WaitAsync().ConfigureAwait(false);
            try {
                token.ThrowIfCancellationRequested();
                SetProteinKeywords(keywords);
            }
            finally {
                proteinSemaphore.Release();
            }
        }

        private void SetProteinKeywords(IEnumerable<string> keywords) {
            proteinFilterKeywords.Clear();
            proteinFilterKeywords.AddRange(keywords);
        }

        public ReadOnlyCollection<string> CommentFilterKeywords { get; }
        private readonly List<string> commentFilterKeywords;

        private readonly SemaphoreSlim commentSemaphore = new SemaphoreSlim(1, 1);
        public async Task SetCommentKeywordsAsync(IEnumerable<string> keywords, CancellationToken token) {
            token.ThrowIfCancellationRequested();
            await commentSemaphore.WaitAsync().ConfigureAwait(false);
            try {
                token.ThrowIfCancellationRequested();
                SetCommentKeywords(keywords);
            }
            finally {
                commentSemaphore.Release();
            }
        }

        private void SetCommentKeywords(IEnumerable<string> keywords) {
            commentFilterKeywords.Clear();
            commentFilterKeywords.AddRange(keywords);
        }

        public PeakFilterModel PeakFilterModel { get; }

        public bool PeakFilter(IFilterable peak) {
            return PeakFilterModel.PeakFilter(peak, evaluator)
                && MzFilter(peak)
                && (!UseRtFilter || RtFilter(peak))
                && (!UseDtFilter || DtFilter(peak))
                && AmplitudeFilter(peak)
                && ProteinFilter(peak, ProteinFilterKeywords)
                && MetaboliteFilter(peak, MetaboliteFilterKeywords)
                && CommentFilter(peak, CommentFilterKeywords);
        }

        private bool MzFilter(IChromatogramPeak peak) {
            return MzLowerValue <= peak.Mass && peak.Mass <= MzUpperValue;
        }

        private bool RtFilter(IChromatogramPeak peak) {
            return RtLowerValue <= peak.ChromXs.RT.Value && peak.ChromXs.RT.Value <= RtUpperValue;
        }

        private bool DtFilter(IChromatogramPeak peak) {
            return DtLowerValue <= peak.ChromXs.Drift.Value && peak.ChromXs.Drift.Value <= DtUpperValue;
        }

        private bool ProteinFilter(IFilterable peak, IEnumerable<string> keywords) {
            return keywords.All(keyword => peak.Protein?.Contains(keyword) ?? true);
        }

        private bool MetaboliteFilter(IMoleculeProperty peak, IEnumerable<string> keywords) {
            return keywords.All(keyword => peak.Name.Contains(keyword));
        }

        private bool CommentFilter(IFilterable peak, IEnumerable<string> keywords) {
            return keywords.All(keyword => string.IsNullOrEmpty(keyword) || (peak.Comment?.Contains(keyword) ?? false));
        }

        private bool AmplitudeFilter(IFilterable peak) {
            var relative = peak.RelativeAmplitudeValue;
            return AmplitudeLowerValue <= relative && relative <= AmplitudeUpperValue;
        }
    }
}
