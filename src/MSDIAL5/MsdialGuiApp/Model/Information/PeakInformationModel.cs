using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Utility;
using CompMs.Common.DataObj.Property;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Information
{
    internal interface IPeakInformationModel : INotifyPropertyChanged
    {
        string? Annotation { get; }
        string? AdductIonName { get; }
        string? Formula { get; }
        string? Ontology { get; }
        string? InChIKey { get; }
        string? Comment { get; }
        ReadOnlyObservableCollection<IPeakPoint>? PeakPoints { get; }
        ReadOnlyObservableCollection<IPeakAmount>? PeakAmounts { get; }
    }

    internal sealed class PeakInformationAnalysisModel : DisposableModelBase, IPeakInformationModel
    {
        public PeakInformationAnalysisModel(IObservable<ChromatogramPeakFeatureModel?> model) {
            model.Select(m =>
            {
                return new CompositeDisposable
                {
                    (m?.ObserveProperty(m_ => m_.Name) ?? Observable.Return(string.Empty)).Subscribe(m_ => Annotation = m_),
                    (m?.ObserveProperty(m_ => m_.AdductType) ?? Observable.Return(AdductIon.Default)).Subscribe(m_ => AdductIonName = m_?.AdductIonName),
                    (m?.ObserveProperty(m_ => m_.Formula) ?? Observable.Return(new Formula())).Subscribe(m_ => Formula = m_?.FormulaString),
                    (m?.ObserveProperty(m_ => m_.Ontology) ?? Observable.Return(string.Empty)).Subscribe(m_ => Ontology = m_),
                    (m?.ObserveProperty(m_ => m_.InChIKey) ?? Observable.Return(string.Empty)).Subscribe(m_ => InChIKey = m_),
                    (m?.ObserveProperty(m_ => m_.Comment) ?? Observable.Return(string.Empty)).Subscribe(m_ => Comment = m_)
                };
            }).DisposePreviousValue()
            .Subscribe()
            .AddTo(Disposables);

            _peakPointMaps = new ObservableCollection<Func<ChromatogramPeakFeatureModel?, IPeakPoint>>();
            _peakPoints = new ReadOnlyReactiveCollection<IPeakPoint>(Observable.Never<IPeakPoint>(), new ObservableCollection<IPeakPoint>());
            var peakPoints = model
                .Select(m => _peakPointMaps.ToReadOnlyReactiveCollection(f => f(m)))
                .DisposePreviousValue()
                .Subscribe(ps => PeakPoints = ps)
                .AddTo(Disposables);

            _peakAmountMaps = new ObservableCollection<Func<ChromatogramPeakFeatureModel?, IPeakAmount>>();
            _peakAmounts = new ReadOnlyReactiveCollection<IPeakAmount>(Observable.Never<IPeakAmount>(), new ObservableCollection<IPeakAmount>());
            var peakAmounts = model
                .Select(m => _peakAmountMaps.ToReadOnlyReactiveCollection(f => f(m)))
                .DisposePreviousValue()
                .Subscribe(ps => PeakAmounts = ps)
                .AddTo(Disposables);
        }

        public string? Annotation {
            get => string.IsNullOrEmpty(_annotation) ? "Unknown" : _annotation;
            private set => SetProperty(ref _annotation, value);
        }
        private string? _annotation;

        public string? AdductIonName {
            get => string.IsNullOrEmpty(_adductIonName) ? "NA" : _adductIonName;
            private set => SetProperty(ref _adductIonName, value);
        }
        private string? _adductIonName;

        public string? Formula {
            get => string.IsNullOrEmpty(_formula) ? "NA" : _formula;
            private set => SetProperty(ref _formula, value);
        }
        private string? _formula;

        public string? Ontology {
            get => string.IsNullOrEmpty(_ontology) ? "NA" : _ontology;
            private set => SetProperty(ref _ontology, value);
        }
        private string? _ontology;

        public string? InChIKey {
            get => string.IsNullOrEmpty(_inChIKey) ? "NA" : _inChIKey;
            private set => SetProperty(ref _inChIKey, value);
        }
        private string? _inChIKey;

        public string? Comment {
            get => string.IsNullOrEmpty(_comment) ? "NA" : _comment;
            private set => SetProperty(ref _comment, value);
        }
        private string? _comment;

        ReadOnlyObservableCollection<IPeakPoint>? IPeakInformationModel.PeakPoints => _peakPoints;
        public ReadOnlyReactiveCollection<IPeakPoint>? PeakPoints {
            get => _peakPoints;
            private set => SetProperty(ref _peakPoints, value);
        }
        private ReadOnlyReactiveCollection<IPeakPoint>? _peakPoints;
        private readonly ObservableCollection<Func<ChromatogramPeakFeatureModel?, IPeakPoint>> _peakPointMaps;

        public void Add(params Func<ChromatogramPeakFeatureModel?, IPeakPoint>[] maps) {
            foreach (var map in maps) {
                _peakPointMaps.Add(map);
            }
        }

        ReadOnlyObservableCollection<IPeakAmount>? IPeakInformationModel.PeakAmounts => _peakAmounts;
        public ReadOnlyReactiveCollection<IPeakAmount>? PeakAmounts {
            get => _peakAmounts;
            private set => SetProperty(ref _peakAmounts, value);
        }
        private ReadOnlyReactiveCollection<IPeakAmount>? _peakAmounts;
        private readonly ObservableCollection<Func<ChromatogramPeakFeatureModel?, IPeakAmount>> _peakAmountMaps;

        public void Add(params Func<ChromatogramPeakFeatureModel?, IPeakAmount>[] maps) {
            foreach (var map in maps) {
                _peakAmountMaps.Add(map);
            }
        }
    }

    internal sealed class PeakInformationAlignmentModel : DisposableModelBase, IPeakInformationModel
    {
        public PeakInformationAlignmentModel(IObservable<AlignmentSpotPropertyModel?> model) {
            model.Select(m => {
                if (m is null) {
                    return null;
                }
                return new CompositeDisposable
                {
                    m.ObserveProperty(m_ => m_.Name).Subscribe(m_ => Annotation = m_),
                    m.ObserveProperty(m_ => m_.AdductType).Subscribe(m_ => AdductIonName = m_?.AdductIonName),
                    m.ObserveProperty(m_ => m_.Formula).Subscribe(m_ => Formula = m_?.FormulaString),
                    m.ObserveProperty(m_ => m_.Ontology).Subscribe(m_ => Ontology = m_),
                    m.ObserveProperty(m_ => m_.InChIKey).Subscribe(m_ => InChIKey = m_),
                    m.ObserveProperty(m_ => m_.Comment).Subscribe(m_ => Comment = m_)
                };
            }).DisposePreviousValue()
            .Subscribe()
            .AddTo(Disposables);

            _peakPointMaps = new ObservableCollection<Func<AlignmentSpotPropertyModel?, IPeakPoint>>();
            _peakPoints = new ReadOnlyReactiveCollection<IPeakPoint>(Observable.Never<IPeakPoint>(), new ObservableCollection<IPeakPoint>());
            var peakPoints = model
                .Select(m => _peakPointMaps.ToReadOnlyReactiveCollection(f => f(m)))
                .DisposePreviousValue()
                .Subscribe(ps => PeakPoints = ps)
                .AddTo(Disposables);

            _peakAmountMaps = new ObservableCollection<Func<AlignmentSpotPropertyModel?, IPeakAmount>>();
            _peakAmounts = new ReadOnlyReactiveCollection<IPeakAmount>(Observable.Never<IPeakAmount>(), new ObservableCollection<IPeakAmount>());
            var peakAmounts = model
                .Select(m => _peakAmountMaps.ToReadOnlyReactiveCollection(f => f(m)))
                .DisposePreviousValue()
                .Subscribe(ps => PeakAmounts = ps)
                .AddTo(Disposables);
        }

        public string? Annotation {
            get => string.IsNullOrEmpty(_annotation) ? "Unknown" : _annotation;
            private set => SetProperty(ref _annotation, value);
        }
        private string? _annotation;

        public string? AdductIonName {
            get => string.IsNullOrEmpty(_adductIonName) ? "NA" : _adductIonName;
            private set => SetProperty(ref _adductIonName, value);
        }
        private string? _adductIonName;

        public string? Formula {
            get => string.IsNullOrEmpty(_formula) ? "NA" : _formula;
            private set => SetProperty(ref _formula, value);
        }
        private string? _formula;

        public string? Ontology {
            get => string.IsNullOrEmpty(_ontology) ? "NA" : _ontology;
            private set => SetProperty(ref _ontology, value);
        }
        private string? _ontology;

        public string? InChIKey {
            get => string.IsNullOrEmpty(_inChIKey) ? "NA" : _inChIKey;
            private set => SetProperty(ref _inChIKey, value);
        }
        private string? _inChIKey;

        public string? Comment {
            get => string.IsNullOrEmpty(_comment) ? "NA" : _comment;
            private set => SetProperty(ref _comment, value);
        }
        private string? _comment;

        ReadOnlyObservableCollection<IPeakPoint> IPeakInformationModel.PeakPoints => _peakPoints;
        public ReadOnlyReactiveCollection<IPeakPoint> PeakPoints {
            get => _peakPoints;
            private set => SetProperty(ref _peakPoints, value);
        }
        private ReadOnlyReactiveCollection<IPeakPoint> _peakPoints;
        private readonly ObservableCollection<Func<AlignmentSpotPropertyModel?, IPeakPoint>> _peakPointMaps;

        public void Add(params Func<AlignmentSpotPropertyModel?, IPeakPoint>[] maps) {
            foreach (var map in maps) {
                _peakPointMaps.Add(map);
            }
        }

        ReadOnlyObservableCollection<IPeakAmount> IPeakInformationModel.PeakAmounts => _peakAmounts;
        public ReadOnlyReactiveCollection<IPeakAmount> PeakAmounts {
            get => _peakAmounts;
            private set => SetProperty(ref _peakAmounts, value);
        }
        private ReadOnlyReactiveCollection<IPeakAmount> _peakAmounts;
        private readonly ObservableCollection<Func<AlignmentSpotPropertyModel?, IPeakAmount>> _peakAmountMaps;

        public void Add(params Func<AlignmentSpotPropertyModel?, IPeakAmount>[] maps) {
            foreach (var map in maps) {
                _peakAmountMaps.Add(map);
            }
        }
    }

    internal sealed class PeakInformationMs1BasedModel : DisposableModelBase, IPeakInformationModel {
        public PeakInformationMs1BasedModel(IObservable<Ms1BasedSpectrumFeature?> source) {
            source.DefaultIfNull(m => m.Molecule.ObserveProperty(m_ => m_.Name), Observable.Return(string.Empty)).Switch().Subscribe(m => Annotation = m).AddTo(Disposables);
            source.DefaultIfNull(m => m.Molecule.ObserveProperty(m_ => m_.Formula), Observable.Return(string.Empty)).Switch().Subscribe(m => Formula = m).AddTo(Disposables);
            source.DefaultIfNull(m => m.Molecule.ObserveProperty(m_ => m_.Ontology), Observable.Return(string.Empty)).Switch().Subscribe(m => Ontology = m).AddTo(Disposables);
            source.DefaultIfNull(m => m.Molecule.ObserveProperty(m_ => m_.InChIKey), Observable.Return(string.Empty)).Switch().Subscribe(m => InChIKey = m).AddTo(Disposables);
            source.DefaultIfNull(m => m.ObserveProperty(m_ => m_.Comment), Observable.Return(string.Empty)).Switch().Subscribe(m => Comment = m).AddTo(Disposables);

            _peakPointMaps = new ObservableCollection<Func<Ms1BasedSpectrumFeature?, IPeakPoint>>();
            var peakPoints = source
                .Select(m => _peakPointMaps.ToReadOnlyReactiveCollection(f => f(m)))
                .DisposePreviousValue()
                .Subscribe(ps => PeakPoints = ps)
                .AddTo(Disposables);

            _peakAmountMaps = new ObservableCollection<Func<Ms1BasedSpectrumFeature?, IPeakAmount>>();
            var peakAmounts = source
                .Select(m => _peakAmountMaps.ToReadOnlyReactiveCollection(f => f(m)))
                .DisposePreviousValue()
                .Subscribe(ps => PeakAmounts = ps)
                .AddTo(Disposables);
        }

        public string Annotation {
            get => string.IsNullOrEmpty(_annotation) ? "Unknown" : _annotation;
            private set => SetProperty(ref _annotation, value);
        }
        private string _annotation = string.Empty;

        public string AdductIonName {
            get => string.IsNullOrEmpty(_adductIonName) ? "NA" : _adductIonName;
            private set => SetProperty(ref _adductIonName, value);
        }
        private string _adductIonName = string.Empty;

        public string Formula {
            get => string.IsNullOrEmpty(_formula) ? "NA" : _formula;
            private set => SetProperty(ref _formula, value);
        }
        private string _formula = string.Empty;

        public string Ontology {
            get => string.IsNullOrEmpty(_ontology) ? "NA" : _ontology;
            private set => SetProperty(ref _ontology, value);
        }
        private string _ontology = string.Empty;

        public string InChIKey {
            get => string.IsNullOrEmpty(_inChIKey) ? "NA" : _inChIKey;
            private set => SetProperty(ref _inChIKey, value);
        }
        private string _inChIKey = string.Empty;

        public string Comment {
            get => string.IsNullOrEmpty(_comment) ? "NA" : _comment;
            private set => SetProperty(ref _comment, value);
        }
        private string _comment = string.Empty;

        ReadOnlyObservableCollection<IPeakPoint>? IPeakInformationModel.PeakPoints => _peakPoints;
        public ReadOnlyReactiveCollection<IPeakPoint>? PeakPoints {
            get => _peakPoints;
            private set => SetProperty(ref _peakPoints, value);
        }
        private ReadOnlyReactiveCollection<IPeakPoint>? _peakPoints;
        private readonly ObservableCollection<Func<Ms1BasedSpectrumFeature?, IPeakPoint>> _peakPointMaps;

        public void Add(params Func<Ms1BasedSpectrumFeature?, IPeakPoint>[] maps) {
            foreach (var map in maps) {
                _peakPointMaps.Add(map);
            }
        }

        ReadOnlyObservableCollection<IPeakAmount>? IPeakInformationModel.PeakAmounts => _peakAmounts;
        public ReadOnlyReactiveCollection<IPeakAmount>? PeakAmounts {
            get => _peakAmounts;
            private set => SetProperty(ref _peakAmounts, value);
        }
        private ReadOnlyReactiveCollection<IPeakAmount>? _peakAmounts;
        private readonly ObservableCollection<Func<Ms1BasedSpectrumFeature?, IPeakAmount>> _peakAmountMaps;

        public void Add(params Func<Ms1BasedSpectrumFeature?, IPeakAmount>[] maps) {
            foreach (var map in maps) {
                _peakAmountMaps.Add(map);
            }
        }
    }

    internal interface IPeakPoint : INotifyPropertyChanged
    {
        string Label { get; }
        string Point { get; }
    }

    internal sealed class RtPoint : BindableBase, IPeakPoint
    {
        private const string RT_LABEL = "RT [min]";

        public RtPoint(double rt, double? reference = null) {
            if (reference is null) {
                Point = rt.ToString("F3");
            }
            else {
                Point = $"{rt:F3}|ref={reference:F3}|diff={Math.Abs(rt - reference.Value):F2}";
            }
        }

        public string Label => RT_LABEL;
        public string Point { get; }
    }

    internal sealed class RiPoint : BindableBase, IPeakPoint
    {
        private const string RI_LABEL = "RI";

        public RiPoint(double ri, double? reference = null) {
            if (reference is null) {
                Point = ri.ToString("F3");
            }
            else {
                Point = $"{ri:F3}|ref={reference:F3}|diff={Math.Abs(ri - reference.Value):F2}";
            }
        }

        public string Label => RI_LABEL;
        public string Point { get; }
    }

    internal sealed class MzPoint : BindableBase, IPeakPoint
    {
        private const string MZ_LABEL = "m/z";

        public MzPoint(double mz, double? reference = null) {
            if (reference is null) {
                Point = mz.ToString("F5");
            }
            else {
                Point = $"{mz:F5}|ref={reference:F5}|diff(mDa)={Math.Abs(mz-reference.Value) * 1000:F3}";
            }
        }

        public string Label => MZ_LABEL;
        public string Point { get; }
    }

    internal sealed class QuantMassPoint : BindableBase, IPeakPoint
    {
        private const string QUANTMASS_LABEL = "Quant mass";

        public QuantMassPoint(double mz, double? reference = null) {
            if (reference is null) {
                Point = mz.ToString("F2");
            }
            else {
                Point = $"{mz:F2}|ref={reference:F2}";
            }
        }

        public string Label => QUANTMASS_LABEL;
        public string Point { get; }
    }

    internal sealed class DriftPoint : BindableBase, IPeakPoint
    {
        private const string DRIFT_LABEL = "Mobility [1/K0]";

        public DriftPoint(double dt) {
            Point = dt.ToString("F4");
        }

        public string Label => DRIFT_LABEL;
        public string Point { get; }
    }

    internal sealed class CcsPoint : BindableBase, IPeakPoint
    {
        private const string CCS_LABEL = "CCS";

        public CcsPoint(double ccs, double? reference = null) {
            if (reference is null) {
                Point = ccs.ToString("F3");
            }
            else {
                Point = $"{ccs:F3}|ref={reference:F3}|diff={Math.Abs(ccs - reference.Value):F2}";
            }
        }

        public string Label => CCS_LABEL;
        public string Point { get; }
    }

    internal interface IPeakAmount : INotifyPropertyChanged
    {
        string Label { get; }
        double Amount { get; }
    }

    internal sealed class HeightAmount : BindableBase, IPeakAmount
    {
        private const string HEIGHT_LABEL = "Peak height";

        public HeightAmount(double height) {
            Amount = height;
        }

        public string Label => HEIGHT_LABEL;
        public double Amount { get; }
    }

    internal sealed class AreaAmount : BindableBase, IPeakAmount
    {
        private const string AREA_LABEL = "Peak area";

        public AreaAmount(double area) {
            Amount = area;
        }

        public string Label => AREA_LABEL;
        public double Amount { get; }
    }

    internal sealed class NormalizedHeightAmount : BindableBase, IPeakAmount
    {
        private const string NORMALIZEDHEIGHT_LABEL = "Normalized peak height";

        public NormalizedHeightAmount(double height) {
            Amount = height;
        }

        public string Label => NORMALIZEDHEIGHT_LABEL;
        public double Amount { get; }
    }

    internal sealed class NormalizedAreaAmount : BindableBase, IPeakAmount
    {
        private const string NORMALIZEDAREA_LABEL = "Normalized peak area";

        public NormalizedAreaAmount(double area) {
            Amount = area;
        }

        public string Label => NORMALIZEDAREA_LABEL;
        public double Amount { get; }
    }
}
