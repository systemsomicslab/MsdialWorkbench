using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.Msdial.Model.Chart
{
    internal sealed class BrushMapDataSelector<T> : BindableBase
    {
        public BrushMapDataSelector(params BrushMapData<T>[] brushes) {
            Brushes = new ObservableCollection<BrushMapData<T>>(brushes);
            SelectedBrush = brushes.FirstOrDefault();
        }

        public ObservableCollection<BrushMapData<T>> Brushes { get; }

        public BrushMapData<T> SelectedBrush {
            get => _selectedBrush;
            set => SetProperty(ref _selectedBrush, value);
        }
        private BrushMapData<T> _selectedBrush;

        public static BrushMapDataSelector<ChromatogramPeakFeatureModel> CreateBrushes() {
            return new BrushMapDataSelector<ChromatogramPeakFeatureModel>(BrushMapData.CreateAmplitudeScoreBursh());
        }

        public static BrushMapDataSelector<ChromatogramPeakFeatureModel> CreateLipidomicsBrushes() {
            var scoreBrushData = BrushMapData.CreateAmplitudeScoreBursh();
            var ontologyBrushData = BrushMapData.CreateOntologyBrush();
            return new BrushMapDataSelector<ChromatogramPeakFeatureModel>(scoreBrushData, ontologyBrushData)
            {
                SelectedBrush = ontologyBrushData,
            };
        }
    }
}
