using CompMs.App.Msdial.Model.DataObj;
using Reactive.Bindings;
using System.ComponentModel;

namespace CompMs.App.Msdial.ViewModel.Core
{
    internal interface IAnalysisResultViewModel : IResultViewModel
    {
        ICollectionView Ms1PeaksView { get; }

        ReadOnlyReactivePropertySlim<ChromatogramPeakFeatureModel> Target { get; }

        // Filtering
        ReactivePropertySlim<string> MetaboliteFilterKeyword { get; }
        ReadOnlyReactivePropertySlim<string[]> MetaboliteFilterKeywords { get; }
        ReactivePropertySlim<string> CommentFilterKeyword { get; }
        ReadOnlyReactivePropertySlim<string[]> CommentFilterKeywords { get; }
        ReactivePropertySlim<string> ProteinFilterKeyword { get; }
        ReadOnlyReactivePropertySlim<string[]> ProteinFilterKeywords { get; }

        double AmplitudeOrderMin { get; }
        double AmplitudeOrderMax { get; }
        ReactivePropertySlim<double> AmplitudeLowerValue { get; }
        ReactivePropertySlim<double> AmplitudeUpperValue { get; }

        ReactivePropertySlim<string> DisplayLabel { get; }
    }

    internal static class AnalysisResultViewModel
    {
        public static ICollectionView PeakSpotsView(this IAnalysisResultViewModel self) => self.Ms1PeaksView;
    }
}
