using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Search
{
    public sealed class PeakSpotNavigatorModel : BindableBase
    {
        public PeakSpotNavigatorModel(IReadOnlyList<IFilterable> peakSpots, PeakFilterModel peakFilterModel, IMatchResultEvaluator<MsScanMatchResult> evaluator) {
            PeakSpots = peakSpots ?? throw new System.ArgumentNullException(nameof(peakSpots));
            PeakFilterModel = peakFilterModel ?? throw new System.ArgumentNullException(nameof(peakFilterModel));
            this.evaluator = evaluator ?? throw new System.ArgumentNullException(nameof(evaluator));
            AmplitudeLowerValue = 0d;
            AmplitudeUpperValue = 1d;
            MzLowerValue = peakSpots.DefaultIfEmpty().Min(p => p?.Mass) ?? 0d;
            MzUpperValue = peakSpots.DefaultIfEmpty().Max(p => p?.Mass) ?? 1d;
            RtLowerValue = peakSpots.DefaultIfEmpty().Min(p => p?.ChromXs.RT.Value) ?? 0d;
            RtUpperValue = peakSpots.DefaultIfEmpty().Max(p => p?.ChromXs.RT.Value) ?? 1d;
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
                && RtFilter(peak)
                && AmplitudeFilter(peak)
                && ProteinFilter(peak, ProteinFilterKeywords)
                && MetaboliteFilter(peak, MetaboliteFilterKeywords)
                && CommentFilter(peak, CommentFilterKeywords);
        }

        private bool MzFilter(IChromatogramPeak peak) {
            return MzLowerValue <= peak.Mass && peak.Mass <= MzUpperValue;
        }

        private bool RtFilter(IChromatogramPeak peak) {
            return RtLowerValue <= peak.ChromXs.Value && peak.ChromXs.Value <= RtUpperValue;
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
