using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.CommonMVVM;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace CompMs.App.Msdial.Model.Information
{
    internal sealed class CompoundDetailModel : DisposableModelBase
    {
        private readonly MsScanMatchResult _result;
        private readonly MoleculeMsReference _reference;

        public CompoundDetailModel(MsScanMatchResult result, MoleculeMsReference reference) {
            _result = result;
            _reference = reference;

            Annotation = result?.Name;
            Formula = reference?.Formula;
            Ontology = reference?.Ontology ?? reference?.CompoundClass;
            Smiles = reference?.SMILES;
            InChIKey = reference?.InChIKey;
            AnnotatorId = result.AnnotatorID;

            _compoundSimilarities = new ObservableCollection<ISimilarity>();
            CompoundSimilarities = new ReadOnlyObservableCollection<ISimilarity>(_compoundSimilarities);
        }

        public string Annotation {
            get => string.IsNullOrEmpty(_annotation) ? "Unknown" : _annotation;
            private set => SetProperty(ref _annotation, value);
        }
        private string _annotation;

        public ReadOnlyObservableCollection<ISimilarity> CompoundSimilarities { get; }
        private readonly ObservableCollection<ISimilarity> _compoundSimilarities;

        public void Add(ISimilarity similarityScore) {
            _compoundSimilarities.Add(similarityScore);
        }

        public void Add(Func<MsScanMatchResult, ISimilarity> map) {
            _compoundSimilarities.Add(map(_result));
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
