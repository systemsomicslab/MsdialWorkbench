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
            IReadOnlyList<IAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult>> annotators) {
            if (file is null) {
                throw new ArgumentNullException(nameof(file));
            }

            if (msIonProperty is null) {
                throw new ArgumentNullException(nameof(msIonProperty));
            }

            if (moleculeProperty is null) {
                throw new ArgumentNullException(nameof(moleculeProperty));
            }

            if (annotators is null) {
                throw new ArgumentNullException(nameof(annotators));
            }

            File = file;
            MSIonProperty = msIonProperty;
            MoleculeProperty = moleculeProperty;
            Annotators = annotators.Select(
                annotator => new AnnotatorContainer(
                    annotator.Annotator,
                    new MsRefSearchParameterBase(annotator.Parameter)
                )).ToList();
            Annotator = Annotators.FirstOrDefault();
        }

        public IReadOnlyList<IAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult>> Annotators { get; } 
        public IAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult> Annotator {
            get => annotator;
            set => SetProperty(ref annotator, value);
        }
        private IAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult> annotator;
        
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
            IReadOnlyList<IAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult>> annotators)
            : base(fileBean, property, property, annotators){

            if (property == null) {
                throw new ArgumentException(nameof(property));
            }

            this.Property = property;
            this.msdecResult = msdecResult ?? throw new ArgumentNullException(nameof(msdecResult));
            this.isotopes = isotopes;

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

        [Obsolete]
        public CompoundSearchModel(
            IFileBean fileBean,
            T property, MSDecResult msdecResult,
            IReadOnlyList<IsotopicPeak> isotopes,
            IAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult> annotator,
            MsRefSearchParameterBase parameter = null)
            : this(fileBean,
                  property,
                  msdecResult,
                  isotopes,
                  new List<IAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult>> {
                      new AnnotatorContainer(annotator, parameter ?? new MsRefSearchParameterBase())
                  }) {

        }

        private readonly MSDecResult msdecResult;
        private readonly IReadOnlyList<IsotopicPeak> isotopes;

        public T Property { get; }

        protected override IEnumerable<CompoundResult> SearchCore() {
            var annotator = Annotator.Annotator;
            var candidates = annotator.FindCandidates(new AnnotationQuery(Property, msdecResult, isotopes, Annotator.Parameter));
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
