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
    abstract class CompoundSearchModel : BindableBase, IDisposable
    {
        public CompoundSearchModel(
            IFileBean file,
            IMSIonProperty msIonProperty,
            IMoleculeProperty moleculeProperty,
            MsRefSearchParameterBase parameter) {
            File = file ?? throw new ArgumentNullException(nameof(file));
            MSIonProperty = msIonProperty ?? throw new ArgumentNullException(nameof(msIonProperty));
            MoleculeProperty = moleculeProperty ?? throw new ArgumentNullException(nameof(moleculeProperty));
            Parameter = parameter;
        }
        
        public IFileBean File { get; }

        public IMSIonProperty MSIonProperty { get; }

        public IMoleculeProperty MoleculeProperty { get; }

        public MsRefSearchParameterBase Parameter { get; }

        public MsSpectrumModel MsSpectrumModel { get; protected set; }

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

        protected abstract IEnumerable<CompoundResult> SearchCore();

        public abstract void SetConfidence();

        public abstract void SetUnsettled();

        public abstract void SetUnknown();


        private bool disposedValue;
        protected CompositeDisposable disposables = new CompositeDisposable();

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

    class CompoundSearchModel<T> : CompoundSearchModel where T: IMSIonProperty, IMoleculeProperty
    {
        public CompoundSearchModel(
            IFileBean fileBean,
            T property, MSDecResult msdecResult,
            IReadOnlyList<IsotopicPeak> isotopes,
            IAnnotator<T, MSDecResult> annotator,
            MsRefSearchParameterBase parameter = null)
            : base(
                  fileBean,
                  property,
                  property,
                  parameter ?? new MsRefSearchParameterBase()) {
            if (property == null) {
                throw new ArgumentException(nameof(property));
            }

            this.Property = property;
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

        public T Property { get; }

        protected override IEnumerable<CompoundResult> SearchCore() {
            var candidates = annotator.FindCandidates(Property, msdecResult, isotopes, Parameter);
            foreach (var candidate in candidates) {
                candidate.IsManuallyModified = true;
                candidate.Source |= SourceType.Manual;
            }
            return candidates.OrderByDescending(result => result.TotalScore)
                .Select(result => new CompoundResult(annotator.Refer(result), result));
        }

        public override void SetConfidence() {
            var reference = SelectedReference;
            var result = SelectedMatchResult;
            DataAccess.SetMoleculeMsPropertyAsConfidence(Property, reference, result);
            if (Property is IAnnotatedObject obj) {
                obj.MatchResults.RemoveManuallyResults();
                obj.MatchResults.AddResult(result);
            }
        }

        public override void SetUnsettled() {
            var reference = SelectedReference;
            var result = SelectedMatchResult;
            DataAccess.SetMoleculeMsPropertyAsUnsettled(Property, reference, result);
            if (Property is IAnnotatedObject obj) {
                obj.MatchResults.RemoveManuallyResults();
                obj.MatchResults.AddResult(result);
            }
        }

        public override void SetUnknown() {
            DataAccess.ClearMoleculePropertyInfomation(Property);
            if (Property is IAnnotatedObject obj) {
                obj.MatchResults.RemoveManuallyResults();
                obj.MatchResults.AddResult(new MsScanMatchResult { Source = SourceType.Manual | SourceType.Unknown });
            }
        }
    }
}
