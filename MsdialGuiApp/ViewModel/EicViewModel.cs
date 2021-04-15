using CompMs.App.Msdial.Model;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace CompMs.App.Msdial.ViewModel
{
    public class EicViewModel : ViewModelBase
    {
        public EicViewModel(EicModel model) {
            this.model = model;
            WeakEventManager<EicModel, PropertyChangedEventArgs>.AddHandler(model, nameof(model.PropertyChanged), ClearCache);
            WeakEventManager<EicModel, PropertyChangedEventArgs>.AddHandler(model, nameof(model.PropertyChanged), UpdateAxis);
        }

        private readonly EicModel model;

        private void ClearCache(object sender, PropertyChangedEventArgs e) {
            cacheEic = null;
            cacheEicPeak = null;
            cacheEicFocused = null;
            OnPropertyChanged(nameof(Eic));
            OnPropertyChanged(nameof(EicPeak));
            OnPropertyChanged(nameof(EicFocused));
        }

        private void UpdateAxis(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(model.HorizontalData)) {
                OnPropertyChanged(nameof(HorizontalData));
                OnPropertyChanged(nameof(HorizontalAxis));
                OnPropertyChanged(nameof(HorizontalProperty));
                OnPropertyChanged(nameof(HorizontalTitle));
            }

            if (e.PropertyName == nameof(model.VerticalData)) {
                OnPropertyChanged(nameof(VerticalData));
                OnPropertyChanged(nameof(VerticalAxis));
                OnPropertyChanged(nameof(VerticalProperty));
                OnPropertyChanged(nameof(VerticalTitle));
            }
        }

        public ICollectionView Eic {
            get => cacheEic ?? (cacheEic = CollectionViewSource.GetDefaultView(model.Eic));
        }
        private ICollectionView cacheEic;

        public double EicMaxIntensity => model.Eic.Select(peak => peak.Intensity).DefaultIfEmpty().Max();

        public ICollectionView EicPeak {
            get => cacheEicPeak ?? (cacheEicPeak = CollectionViewSource.GetDefaultView(model.EicPeak));
        }
        private ICollectionView cacheEicPeak;

        public ICollectionView EicFocused {
            get => cacheEicFocused ?? (cacheEicFocused = CollectionViewSource.GetDefaultView(cacheEicFocused));
        }
        private ICollectionView cacheEicFocused;

        public AxisData HorizontalData {
            get => model.HorizontalData;
            set {
                if (model.HorizontalData == value) {
                    return;
                }
                model.HorizontalData = value;
                OnPropertyChanged(nameof(HorizontalData));
                OnPropertyChanged(nameof(HorizontalAxis));
                OnPropertyChanged(nameof(HorizontalProperty));
                OnPropertyChanged(nameof(HorizontalTitle));
            }
        }

        public IAxisManager HorizontalAxis => HorizontalData.Axis;

        public string HorizontalProperty => HorizontalData.Property;

        public string HorizontalTitle => HorizontalData.Title;

        public AxisData VerticalData {
            get => model.VerticalData;
            set {
                if (model.VerticalData == value) {
                    return;
                }
                model.VerticalData = value;
                OnPropertyChanged(nameof(VerticalData));
                OnPropertyChanged(nameof(VerticalAxis));
                OnPropertyChanged(nameof(VerticalProperty));
                OnPropertyChanged(nameof(VerticalTitle));
            }
        }

        public IAxisManager VerticalAxis => VerticalData.Axis;

        public string VerticalProperty => VerticalData.Property;

        public string VerticalTitle => VerticalData.Title;

        public string GraphTitle {
            get => model.GraphTitle;
            set {
                if (model.GraphTitle == value) {
                    return;
                }
                model.GraphTitle = value;
                OnPropertyChanged(nameof(GraphTitle));
            }
        }
    }
}
