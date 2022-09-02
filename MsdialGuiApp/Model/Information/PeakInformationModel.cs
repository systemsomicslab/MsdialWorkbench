using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Information
{
    internal interface IPeakInformationModel : INotifyPropertyChanged
    {
        string Annotation { get; }
        string AdductIonName { get; }
        string Formula { get; }
        string Ontology { get; }
        string InChIKey { get; }
        string Comment { get; }
        ReadOnlyObservableCollection<IPeakPoint> PeakPoints { get; }
        ReadOnlyObservableCollection<IPeakAmount> PeakAmounts { get; }
    }

    internal sealed class PeakInformationAnalysisModel : DisposableModelBase, IPeakInformationModel
    {
        public PeakInformationAnalysisModel(IObservable<ChromatogramPeakFeatureModel> model) {
            model.Where(m => !(m is null))
                .Select(m =>
                {
                    var disposables = new CompositeDisposable();
                    disposables.Add(m.ObserveProperty(m_ => m_.Name).Subscribe(m_ => Annotation = m_));
                    disposables.Add(m.ObserveProperty(m_ => m_.AdductIonName).Subscribe(m_ => AdductIonName = m_));
                    disposables.Add(m.ObserveProperty(m_ => m_.Formula).Subscribe(m_ => Formula = m_?.FormulaString));
                    disposables.Add(m.ObserveProperty(m_ => m_.Ontology).Subscribe(m_ => Ontology = m_));
                    disposables.Add(m.ObserveProperty(m_ => m_.InChIKey).Subscribe(m_ => InChIKey = m_));
                    disposables.Add(m.ObserveProperty(m_ => m_.Comment).Subscribe(m_ => Comment = m_));
                    return disposables;
                }).DisposePreviousValue()
                .Subscribe()
                .AddTo(Disposables);

            _peakPointMaps = new ObservableCollection<Func<ChromatogramPeakFeatureModel, IPeakPoint>>();
            var peakPoints = model
                .Select(m => _peakPointMaps.ToReadOnlyReactiveCollection(f => f(m)))
                .DisposePreviousValue()
                .Subscribe(ps => PeakPoints = ps)
                .AddTo(Disposables);

            _peakAmountMaps = new ObservableCollection<Func<ChromatogramPeakFeatureModel, IPeakAmount>>();
            var peakAmounts = model
                .Select(m => _peakAmountMaps.ToReadOnlyReactiveCollection(f => f(m)))
                .DisposePreviousValue()
                .Subscribe(ps => PeakAmounts = ps)
                .AddTo(Disposables);
        }

        public string Annotation {
            get => string.IsNullOrEmpty(_annotation) ? "Unknown" : _annotation;
            private set => SetProperty(ref _annotation, value);
        }
        private string _annotation;

        public string AdductIonName {
            get => string.IsNullOrEmpty(_adductIonName) ? "NA" : _adductIonName;
            private set => SetProperty(ref _adductIonName, value);
        }
        private string _adductIonName;

        public string Formula {
            get => string.IsNullOrEmpty(_formula) ? "NA" : _formula;
            private set => SetProperty(ref _formula, value);
        }
        private string _formula;

        public string Ontology {
            get => string.IsNullOrEmpty(_ontology) ? "NA" : _ontology;
            private set => SetProperty(ref _ontology, value);
        }
        private string _ontology;

        public string InChIKey {
            get => string.IsNullOrEmpty(_inChIKey) ? "NA" : _inChIKey;
            private set => SetProperty(ref _inChIKey, value);
        }
        private string _inChIKey;

        public string Comment {
            get => string.IsNullOrEmpty(_comment) ? "NA" : _comment;
            private set => SetProperty(ref _comment, value);
        }
        private string _comment;

        ReadOnlyObservableCollection<IPeakPoint> IPeakInformationModel.PeakPoints => _peakPoints;
        public ReadOnlyReactiveCollection<IPeakPoint> PeakPoints {
            get => _peakPoints;
            private set => SetProperty(ref _peakPoints, value);
        }
        private ReadOnlyReactiveCollection<IPeakPoint> _peakPoints;
        private readonly ObservableCollection<Func<ChromatogramPeakFeatureModel, IPeakPoint>> _peakPointMaps;

        public void Add(params Func<ChromatogramPeakFeatureModel, IPeakPoint>[] maps) {
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
        private readonly ObservableCollection<Func<ChromatogramPeakFeatureModel, IPeakAmount>> _peakAmountMaps;

        public void Add(params Func<ChromatogramPeakFeatureModel, IPeakAmount>[] maps) {
            foreach (var map in maps) {
                _peakAmountMaps.Add(map);
            }
        }
    }

    internal sealed class PeakInformationAlignmentModel : DisposableModelBase, IPeakInformationModel
    {
        public PeakInformationAlignmentModel(IObservable<AlignmentSpotPropertyModel> model) {
            model.Where(m => !(m is null))
                .Select(m => {
                    var disposables = new CompositeDisposable();
                    disposables.Add(m.ObserveProperty(m_ => m_.Name).Subscribe(m_ => Annotation = m_));
                    disposables.Add(m.ObserveProperty(m_ => m_.AdductIonName).Subscribe(m_ => AdductIonName = m_));
                    disposables.Add(m.ObserveProperty(m_ => m_.Formula).Subscribe(m_ => Formula = m_?.FormulaString));
                    disposables.Add(m.ObserveProperty(m_ => m_.Ontology).Subscribe(m_ => Ontology = m_));
                    disposables.Add(m.ObserveProperty(m_ => m_.InChIKey).Subscribe(m_ => InChIKey = m_));
                    disposables.Add(m.ObserveProperty(m_ => m_.Comment).Subscribe(m_ => Comment = m_));
                    return disposables;
                }).DisposePreviousValue()
                .Subscribe()
                .AddTo(Disposables);

            _peakPointMaps = new ObservableCollection<Func<AlignmentSpotPropertyModel, IPeakPoint>>();
            var peakPoints = model
                .Select(m => _peakPointMaps.ToReadOnlyReactiveCollection(f => f(m)))
                .DisposePreviousValue()
                .Subscribe(ps => PeakPoints = ps)
                .AddTo(Disposables);

            _peakAmountMaps = new ObservableCollection<Func<AlignmentSpotPropertyModel, IPeakAmount>>();
            var peakAmounts = model
                .Select(m => _peakAmountMaps.ToReadOnlyReactiveCollection(f => f(m)))
                .DisposePreviousValue()
                .Subscribe(ps => PeakAmounts = ps)
                .AddTo(Disposables);
        }

        public string Annotation {
            get => string.IsNullOrEmpty(_annotation) ? "Unknown" : _annotation;
            private set => SetProperty(ref _annotation, value);
        }
        private string _annotation;

        public string AdductIonName {
            get => string.IsNullOrEmpty(_adductIonName) ? "NA" : _adductIonName;
            private set => SetProperty(ref _adductIonName, value);
        }
        private string _adductIonName;

        public string Formula {
            get => string.IsNullOrEmpty(_formula) ? "NA" : _formula;
            private set => SetProperty(ref _formula, value);
        }
        private string _formula;

        public string Ontology {
            get => string.IsNullOrEmpty(_ontology) ? "NA" : _ontology;
            private set => SetProperty(ref _ontology, value);
        }
        private string _ontology;

        public string InChIKey {
            get => string.IsNullOrEmpty(_inChIKey) ? "NA" : _inChIKey;
            private set => SetProperty(ref _inChIKey, value);
        }
        private string _inChIKey;

        public string Comment {
            get => string.IsNullOrEmpty(_comment) ? "NA" : _comment;
            private set => SetProperty(ref _comment, value);
        }
        private string _comment;

        ReadOnlyObservableCollection<IPeakPoint> IPeakInformationModel.PeakPoints => _peakPoints;
        public ReadOnlyReactiveCollection<IPeakPoint> PeakPoints {
            get => _peakPoints;
            private set => SetProperty(ref _peakPoints, value);
        }
        private ReadOnlyReactiveCollection<IPeakPoint> _peakPoints;
        private readonly ObservableCollection<Func<AlignmentSpotPropertyModel, IPeakPoint>> _peakPointMaps;

        public void Add(params Func<AlignmentSpotPropertyModel, IPeakPoint>[] maps) {
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
        private readonly ObservableCollection<Func<AlignmentSpotPropertyModel, IPeakAmount>> _peakAmountMaps;

        public void Add(params Func<AlignmentSpotPropertyModel, IPeakAmount>[] maps) {
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

        public RtPoint(double rt) {
            Point = rt.ToString("F3");
        }

        public string Label => RT_LABEL;
        public string Point { get; }
    }

    internal sealed class MzPoint : BindableBase, IPeakPoint
    {
        private const string MZ_LABEL = "m/z";

        public MzPoint(double mz) {
            Point = mz.ToString("F5");
        }

        public string Label => MZ_LABEL;
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

        public CcsPoint(double ccs) {
            Point = ccs.ToString("F3");
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
