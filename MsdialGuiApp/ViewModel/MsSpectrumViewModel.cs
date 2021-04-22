using CompMs.App.Msdial.Model;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;

namespace CompMs.App.Msdial.ViewModel
{
    public class MsSpectrumViewModel<T> : ViewModelBase
    {
        public MsSpectrumViewModel(
            MsSpectrumModel<T> model,
            string labelProperty,
            string orderingProperty) {

            this.model = model;
            LabelProperty = labelProperty;
            OrderingProperty = orderingProperty;

            WeakEventManager<MsSpectrumModel<T>, PropertyChangedEventArgs>.AddHandler(model, nameof(model.PropertyChanged), ClearCache);
            WeakEventManager<MsSpectrumModel<T>, PropertyChangedEventArgs>.AddHandler(model, nameof(model.PropertyChanged), UpdateAxis);
        }

        private readonly MsSpectrumModel<T> model;

        private void ClearCache(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(model.LowerSpectrum)) {
                cacheLowerSpectrum = null;
                OnPropertyChanged(nameof(LowerSpectrum));
            }
            if (e.PropertyName == nameof(model.UpperSpectrum)) {
                cacheUpperSpectrum = null;
                OnPropertyChanged(nameof(UpperSpectrum));
            }
        }

        private void UpdateAxis(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(model.HorizontalData)) {
                OnPropertyChanged(nameof(HorizontalData));
                OnPropertyChanged(nameof(HorizontalAxis));
                OnPropertyChanged(nameof(HorizontalProperty));
                OnPropertyChanged(nameof(HorizontalTitle));
            }
            if (e.PropertyName == nameof(model.UpperVerticalData)) {
                OnPropertyChanged(nameof(UpperVerticalData));
                OnPropertyChanged(nameof(UpperVerticalAxis));
                OnPropertyChanged(nameof(UpperVerticalProperty));
                OnPropertyChanged(nameof(UpperVerticalTitle));
            }
            if (e.PropertyName == nameof(model.LowerVerticalData)) {
                OnPropertyChanged(nameof(LowerVerticalData));
                OnPropertyChanged(nameof(LowerVerticalAxis));
                OnPropertyChanged(nameof(LowerVerticalProperty));
                OnPropertyChanged(nameof(LowerVerticalTitle));
            }
        }

        public ICollectionView UpperSpectrum {
            get => cacheUpperSpectrum ?? (cacheUpperSpectrum = CollectionViewSource.GetDefaultView(model.UpperSpectrum));
        }
        private ICollectionView cacheUpperSpectrum;

        public ICollectionView LowerSpectrum {
            get => cacheLowerSpectrum ?? (cacheLowerSpectrum = CollectionViewSource.GetDefaultView(model.LowerSpectrum));
        }
        private ICollectionView cacheLowerSpectrum;

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

        public AxisData UpperVerticalData {
            get => model.UpperVerticalData;
            set {
                if (model.UpperVerticalData == value) {
                    return;
                }
                model.UpperVerticalData = value;
                OnPropertyChanged(nameof(UpperVerticalData));
                OnPropertyChanged(nameof(UpperVerticalAxis));
                OnPropertyChanged(nameof(UpperVerticalProperty));
                OnPropertyChanged(nameof(UpperVerticalTitle));
            }
        }

        public IAxisManager UpperVerticalAxis => UpperVerticalData.Axis;

        public string UpperVerticalProperty => UpperVerticalData.Property;

        public string UpperVerticalTitle => UpperVerticalData.Title;

        public AxisData LowerVerticalData {
            get => model.LowerVerticalData;
            set {
                if (model.LowerVerticalData == value) {
                    return;
                }
                model.LowerVerticalData = value;
                OnPropertyChanged(nameof(LowerVerticalData));
                OnPropertyChanged(nameof(LowerVerticalAxis));
                OnPropertyChanged(nameof(LowerVerticalProperty));
                OnPropertyChanged(nameof(LowerVerticalTitle));
            }
        }

        public IAxisManager LowerVerticalAxis => LowerVerticalData.Axis;

        public string LowerVerticalProperty => LowerVerticalData.Property;

        public string LowerVerticalTitle => LowerVerticalData.Title;

        public string LabelProperty {
            get => labelProperty;
            set => SetProperty(ref labelProperty, value);
        }
        private string labelProperty = string.Empty;

        public string OrderingProperty {
            get => orderingProperty;
            set => SetProperty(ref orderingProperty, value);
        }
        private string orderingProperty = string.Empty;
    }
}
