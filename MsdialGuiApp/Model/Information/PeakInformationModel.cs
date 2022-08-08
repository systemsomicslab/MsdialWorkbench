using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.DataObj.Property;
using CompMs.CommonMVVM;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace CompMs.App.Msdial.Model.Information
{
    internal sealed class PeakInformationModel : DisposableModelBase
    {
        public PeakInformationModel(ChromatogramPeakFeatureModel model) {
            Annotation = model.Name;
            model.ObserveProperty(m => m.Name, isPushCurrentValueAtFirst: false).Subscribe(m => Annotation = m).AddTo(Disposables);
            AdductIonName = model.AdductIonName;
            model.ObserveProperty(m => m.AdductIonName, isPushCurrentValueAtFirst: false).Subscribe(m => AdductIonName = m).AddTo(Disposables);
            Formula = model.Formula;
            model.ObserveProperty(m => m.Formula, isPushCurrentValueAtFirst: false).Subscribe(m => Formula = m).AddTo(Disposables);
            Ontology = model.Ontology;
            model.ObserveProperty(m => m.Ontology, isPushCurrentValueAtFirst: false).Subscribe(m => Ontology = m).AddTo(Disposables);
            InChIKey = model.InChIKey;
            model.ObserveProperty(m => m.InChIKey, isPushCurrentValueAtFirst: false).Subscribe(m => InChIKey = m).AddTo(Disposables);
            Comment = model.Comment;
            model.ObserveProperty(m => m.Comment, isPushCurrentValueAtFirst: false).Subscribe(m => Comment = m).AddTo(Disposables);

            _peakPoint = new ObservableCollection<IPeakPoint>();
            PeakPoint = new ReadOnlyObservableCollection<IPeakPoint>(_peakPoint);

            _peakAmount = new ObservableCollection<IPeakAmount>();
            PeakAmount = new ReadOnlyObservableCollection<IPeakAmount>(_peakAmount);
        }

        public PeakInformationModel(AlignmentSpotPropertyModel model) {
            Annotation = model.Name;
            model.ObserveProperty(m => m.Name, isPushCurrentValueAtFirst: false).Subscribe(m => Annotation = m).AddTo(Disposables);
            AdductIonName = model.AdductIonName;
            model.ObserveProperty(m => m.AdductIonName, isPushCurrentValueAtFirst: false).Subscribe(m => AdductIonName = m).AddTo(Disposables);
            Formula = model.Formula;
            model.ObserveProperty(m => m.Formula, isPushCurrentValueAtFirst: false).Subscribe(m => Formula = m).AddTo(Disposables);
            Ontology = model.Ontology;
            model.ObserveProperty(m => m.Ontology, isPushCurrentValueAtFirst: false).Subscribe(m => Ontology = m).AddTo(Disposables);
            InChIKey = model.InChIKey;
            model.ObserveProperty(m => m.InChIKey, isPushCurrentValueAtFirst: false).Subscribe(m => InChIKey = m).AddTo(Disposables);
            Comment = model.Comment;
            model.ObserveProperty(m => m.Comment, isPushCurrentValueAtFirst: false).Subscribe(m => Comment = m).AddTo(Disposables);

            _peakPoint = new ObservableCollection<IPeakPoint>();
            PeakPoint = new ReadOnlyObservableCollection<IPeakPoint>(_peakPoint);

            _peakAmount = new ObservableCollection<IPeakAmount>();
            PeakAmount = new ReadOnlyObservableCollection<IPeakAmount>(_peakAmount);
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

        public Formula Formula {
            get => _formula;
            private set => SetProperty(ref _formula, value);
        }
        private Formula _formula;

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

        public ReadOnlyObservableCollection<IPeakPoint> PeakPoint { get; }
        private readonly ObservableCollection<IPeakPoint> _peakPoint;

        public void Add(IPeakPoint point) {
            _peakPoint.Add(point);
        }

        public ReadOnlyObservableCollection<IPeakAmount> PeakAmount { get; }
        private readonly ObservableCollection<IPeakAmount> _peakAmount;

        public void Add(IPeakAmount amount) {
            _peakAmount.Add(amount);
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
        private const string DRIFT_LABEL = "Drift time [1/k0]";

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
