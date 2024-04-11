using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.Msdial.Model.Chart
{
    internal sealed class BrushMapDataSelector<T> : BindableBase
    {
        public BrushMapDataSelector(params BrushMapData<T>[] brushes) {
            System.Diagnostics.Debug.Assert(brushes.Length > 0);
            Brushes = new ObservableCollection<BrushMapData<T>>(brushes);
            _selectedBrush = brushes.First();
        }

        public ObservableCollection<BrushMapData<T>> Brushes { get; }

        public BrushMapData<T> SelectedBrush {
            get => _selectedBrush;
            set => SetProperty(ref _selectedBrush, value);
        }
        private BrushMapData<T> _selectedBrush;
    }

    internal sealed class BrushMapDataSelectorFactory<T> {

        private readonly Func<T, double> _scoreGetter;
        private readonly Func<T, string> _ontlogyGetter;

        public BrushMapDataSelectorFactory(Func<T, double> scoreGetter, Func<T, string> ontlogyGetter) {
            _scoreGetter = scoreGetter;
            _ontlogyGetter = ontlogyGetter;
        }

        public BrushMapDataSelector<T> CreateBrushes(TargetOmics targetOmics) {
            switch (targetOmics) {
                case TargetOmics.Lipidomics:
                    return CreateLipidomicsBrushes();
                case TargetOmics.Metabolomics:
                case TargetOmics.Proteomics:
                default:
                    return CreateDefaultBrushes();
            }
        }

        public BrushMapDataSelector<T> CreateDefaultBrushes() {
            return new BrushMapDataSelector<T>(BrushMapData.CreateAmplitudeScoreBursh(_scoreGetter));
        }

        public BrushMapDataSelector<T> CreateLipidomicsBrushes() {
            var scoreBrushData = BrushMapData.CreateAmplitudeScoreBursh(_scoreGetter);
            var ontologyBrushData = BrushMapData.CreateOntologyBrush(_ontlogyGetter);
            return new BrushMapDataSelector<T>(scoreBrushData, ontologyBrushData)
            {
                SelectedBrush = ontologyBrushData,
            };
        }
    }

    internal static class BrushMapDataSelectorFactory {
        public static BrushMapDataSelector<ChromatogramPeakFeatureModel> CreatePeakFeatureBrushes(TargetOmics targetOmics) {
            var factory = new BrushMapDataSelectorFactory<ChromatogramPeakFeatureModel>(p => p.InnerModel.PeakShape.AmplitudeScoreValue, p => p?.Ontology ?? string.Empty);
            return factory.CreateBrushes(targetOmics);
        }

        public static BrushMapDataSelector<AlignmentSpotPropertyModel> CreateAlignmentSpotBrushes(TargetOmics targetOmics) {
            var factory = new BrushMapDataSelectorFactory<AlignmentSpotPropertyModel>(p => p.innerModel.RelativeAmplitudeValue, p => p?.Ontology ?? string.Empty);
            return factory.CreateBrushes(targetOmics);
        }
    }
}
