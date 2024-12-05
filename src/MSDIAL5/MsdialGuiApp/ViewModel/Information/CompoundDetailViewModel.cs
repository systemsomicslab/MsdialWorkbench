using CompMs.App.Msdial.Model.Information;
using CompMs.Common.DataObj.Property;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Information
{
    internal sealed class CompoundDetailViewModel : ViewModelBase
    {
        public CompoundDetailViewModel(CompoundDetailModel model) {
            Annotation = model.ObserveProperty(m => m.Annotation).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            Formula = model.ObserveProperty(m => m.Formula).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            Ontology = model.ObserveProperty(m => m.Ontology).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            Smiles = model.ObserveProperty(m => m.Smiles).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            InChIKey = model.ObserveProperty(m => m.InChIKey).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            AnnotatorId = model.ObserveProperty(m => m.AnnotatorId).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            CompoundSimilarities = model
                .ObserveProperty(m => m.CompoundSimilarities)
                .Select(cs => cs.ToReadOnlyReactiveCollection(m => new SimilarityScoreViewModel(m)))
                .DisposePreviousValue()
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
        }

        public ReadOnlyReactivePropertySlim<string?> Annotation { get; }
        public ReadOnlyReactivePropertySlim<ReadOnlyReactiveCollection<SimilarityScoreViewModel>> CompoundSimilarities { get; }
        public ReadOnlyReactivePropertySlim<string?> Formula { get; }
        public ReadOnlyReactivePropertySlim<string?> Ontology { get; }
        public ReadOnlyReactivePropertySlim<string?> Smiles { get; }
        public ReadOnlyReactivePropertySlim<string?> InChIKey { get; }
        public ReadOnlyReactivePropertySlim<string?> AnnotatorId { get; }
    }

    internal sealed class SimilarityScoreViewModel : ViewModelBase
    {
        public SimilarityScoreViewModel(ISimilarity similarityScore) {
            if (similarityScore is null) {
                throw new ArgumentNullException(nameof(similarityScore));
            }
            Label = similarityScore.ObserveProperty(m => m.Label).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            Score = similarityScore.ObserveProperty(m => m.Score).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
        }

        public ReadOnlyReactivePropertySlim<string?> Label { get; }
        public ReadOnlyReactivePropertySlim<string?> Score { get; }
    }
}
