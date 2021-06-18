using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
using CompMs.Common.Parameter;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Utility;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Search
{
    class CompoundSearchModel<T> : BindableBase, IDisposable where T: IMSProperty, IMoleculeProperty, IIonProperty
    {
        public CompoundSearchModel(
            IFileBean fileBean,
            T property, MSDecResult msdecResult,
            IReadOnlyList<IsotopicPeak> isotopes,
            IAnnotator<T, MSDecResult> annotator,
            MsRefSearchParameterBase parameter = null) {
            if (property == null) {
                throw new ArgumentException(nameof(property));
            }

            this.File = fileBean ?? throw new ArgumentNullException(nameof(fileBean));
            this.Property = property;
            this.Parameter = parameter ?? new MsRefSearchParameterBase();

            this.msdecResult = msdecResult ?? throw new ArgumentNullException(nameof(msdecResult));
            this.isotopes = isotopes;
            this.annotator = annotator ?? throw new ArgumentNullException(nameof(annotator));

            var referenceSpectrum = this.ObserveProperty(m => m.SelectedReference)
                .Where(c => c != null)
                .Select(c => c.Spectrum)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(disposables);
            MsSpectrumModel = new MsSpectrumModel(
                Observable.Return(this.msdecResult.Spectrum),
                referenceSpectrum,
                peak => peak.Mass,
                peak => peak.Intensity)
            {
                HorizontalTitle = "m/z",
                VerticalTitle = "Abundance",
                HorizontalProperty = nameof(SpectrumPeak.Mass),
                VerticalProperty = nameof(SpectrumPeak.Intensity),
                LabelProperty = nameof(SpectrumPeak.Mass),
                OrderingProperty = nameof(SpectrumPeak.Intensity)
            };
        }

        private readonly MSDecResult msdecResult;
        private readonly IReadOnlyList<IsotopicPeak> isotopes;
        private readonly IAnnotator<T, MSDecResult> annotator;

        public MsSpectrumModel MsSpectrumModel { get; }

        public IFileBean File { get; }

        public T Property { get; }

        public MsRefSearchParameterBase Parameter { get; }

        public MoleculeMsReference SelectedReference { 
            get => selectedReference;
            set => SetProperty(ref selectedReference, value);
        }
        private MoleculeMsReference selectedReference;

        public MsScanMatchResult SelectedMatchResult {
            get => selectedMatchResult;
            set => SetProperty(ref selectedMatchResult, value);
        }
        private MsScanMatchResult selectedMatchResult;

        public IReadOnlyList<CompoundResult> Search() {
            return new ObservableCollection<CompoundResult>(SearchCore());
        }

        protected virtual IEnumerable<CompoundResult> SearchCore() {
            var candidates = annotator.FindCandidates(Property, msdecResult, isotopes, Parameter);
            foreach (var candidate in candidates) {
                candidate.IsManuallyModified = true;
                candidate.Source |= SourceType.Manual;
            }
            return candidates.OrderByDescending(result => result.TotalScore)
                .Select(result => new CompoundResult(annotator.Refer(result), result));
        }

        public void SetConfidence() {
            var reference = SelectedReference;
            var result = SelectedMatchResult;
            DataAccess.SetMoleculeMsPropertyAsConfidence(Property, reference, result);
            if (Property is IAnnotatedObject obj) {
                obj.MatchResults.RemoveManuallyResults();
                obj.MatchResults.AddResult(result);
            }
        }

        public void SetUnsettled() {
            var reference = SelectedReference;
            var result = SelectedMatchResult;
            DataAccess.SetMoleculeMsPropertyAsUnsettled(Property, reference, result);
            if (Property is IAnnotatedObject obj) {
                obj.MatchResults.RemoveManuallyResults();
                obj.MatchResults.AddResult(result);
            }
        }

        public void SetUnknown() {
            DataAccess.ClearMoleculePropertyInfomation(Property);
            if (Property is IAnnotatedObject obj) {
                obj.MatchResults.RemoveManuallyResults();
                obj.MatchResults.AddResult(new MsScanMatchResult { Source = SourceType.Manual | SourceType.Unknown });
            }
        }

        private bool disposedValue;
        private CompositeDisposable disposables = new CompositeDisposable();

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    disposables.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
