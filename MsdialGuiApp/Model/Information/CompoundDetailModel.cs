using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Information
{
    internal sealed class CompoundDetailModel : DisposableModelBase
    {
        public CompoundDetailModel(IObservable<MsScanMatchResult> result, IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer) {
            result.Subscribe(result_ => {
                Annotation = result_?.Name;
                AnnotatorId = result_?.AnnotatorID;
                var reference_ = refer.Refer(result_);
                Formula = reference_?.Formula;
                Ontology = reference_?.Ontology ?? reference_?.CompoundClass;
                Smiles = reference_?.SMILES;
                InChIKey = reference_?.InChIKey;
            }).AddTo(Disposables);

            _compoundSimilaritiesMaps = new ObservableCollection<Func<MsScanMatchResult, ISimilarity>>();
            var compoundSimilarites = result
                .Select(r => _compoundSimilaritiesMaps.ToReadOnlyReactiveCollection(f => f(r)))
                .DisposePreviousValue()
                .Subscribe(cs => CompoundSimilarities = cs)
                .AddTo(Disposables);
        }

        public string Annotation {
            get => string.IsNullOrEmpty(_annotation) ? "Unknown" : _annotation;
            private set => SetProperty(ref _annotation, value);
        }
        private string _annotation;

        public ReadOnlyObservableCollection<ISimilarity> CompoundSimilarities {
            get => _compoundSimilarities;
            private set => SetProperty(ref _compoundSimilarities, value);
        }
        private ReadOnlyObservableCollection<ISimilarity> _compoundSimilarities;

        private readonly ObservableCollection<Func<MsScanMatchResult, ISimilarity>> _compoundSimilaritiesMaps;
        public void Add(params Func<MsScanMatchResult, ISimilarity>[] maps) {
            foreach (var map in maps) {
                _compoundSimilaritiesMaps.Add(map);
            }
        }

        public Formula Formula {
            get => _formula;
            private set => SetProperty(ref _formula, value);
        }
        private Formula _formula;

        public string Ontology {
            get => _ontology;
            private set => SetProperty(ref _ontology, value);
        }
        private string _ontology;

        public string Smiles {
            get => _smiles;
            private set => SetProperty(ref _smiles, value);
        }
        private string _smiles;

        public string InChIKey {
            get => _inChIKey;
            private set => SetProperty(ref _inChIKey, value);
        }
        private string _inChIKey;

        public string AnnotatorId {
            get => _annotatorId;
            private set => SetProperty(ref _annotatorId, value);
        }
        private string _annotatorId;
    }

    internal interface ISimilarity : INotifyPropertyChanged
    {
        string Label { get; }
        string Score { get; }
    }

    internal sealed class MzSimilarity : BindableBase, ISimilarity
    {
        private const string MZSIMILARITY_LABEL = "m/z similarity";
        private readonly double _score;

        public MzSimilarity(double score) {
            _score = score;
        }

        public string Label => MZSIMILARITY_LABEL;
        public string Score => Math.Round(_score * 1000).ToString("F0");
    }

    internal sealed class RtSimilarity : BindableBase, ISimilarity
    {
        private const string RTSIMILARITY_LABEL = "RT similarity";
        private readonly double _score;

        public RtSimilarity(double score) {
            _score = score;
        }

        public string Label => RTSIMILARITY_LABEL;
        public string Score => Math.Round(_score * 1000).ToString("F0");
    }

    internal sealed class CcsSimilarity : BindableBase, ISimilarity
    {
        private const string CCSSIMILARITY_LABEL = "Ccs similarity";
        private readonly double _score;

        public CcsSimilarity(double score) {
            _score = score;
        }

        public string Label => CCSSIMILARITY_LABEL;
        public string Score => Math.Round(_score * 1000).ToString("F0");
    }

    internal sealed class SpectrumSimilarity : BindableBase, ISimilarity
    {
        private const string SPECTRUMSIMILARITY_LABEL = "Spectrum similarity";
        private readonly double _dotScore;
        private readonly double _reverseScore;

        public SpectrumSimilarity(double dotScore, double reverseScore) {
            _dotScore = dotScore;
            _reverseScore = reverseScore;
        }

        public string Label => SPECTRUMSIMILARITY_LABEL;
        public string Score => $"(Dot){_dotScore * 1000:F0}|(Rev){_reverseScore * 1000:F0}";
    }
}
