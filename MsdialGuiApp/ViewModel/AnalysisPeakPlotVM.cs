using CompMs.App.Msdial.Model;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;

namespace CompMs.App.Msdial.ViewModel
{
    public class AnalysisPeakPlotVM : ViewModelBase
    {
        public AnalysisPeakPlotVM(
            AnalysisPeakPlotModel model,
            string horizontalProperty,
            string verticalProperty,
            string graphTitle,
            string horizontalTitle,
            string verticalTitle) {

            Model = model;
            WeakEventManager<AnalysisPeakPlotModel, PropertyChangedEventArgs>.AddHandler(Model, "PropertyChanged", (s, e) => this.spots = null);

            HorizontalProperty = horizontalProperty;
            VerticalProperty = verticalProperty;
            GraphTitle = graphTitle;
            HorizontalTitle = horizontalTitle;
            VerticalTitle = verticalTitle;
        }

        public AnalysisPeakPlotModel Model {
            get => model;
            set => SetProperty(ref model, value);
        }
        private AnalysisPeakPlotModel model;

        public ICollectionView Spots {
            get => spots ?? (spots = CollectionViewSource.GetDefaultView(Model.Spots));
        }
        private ICollectionView spots;

        public string HorizontalProperty {
            get => horizontalProperty;
            set => SetProperty(ref horizontalProperty, value);
        }
        private string horizontalProperty;

        public string VerticalProperty {
            get => verticalProperty;
            set => SetProperty(ref verticalProperty, value);
        }
        private string verticalProperty;

        public IAxisManager HorizontalAxis => Model.HorizontalAxis;
        public IAxisManager VerticalAxis => Model.VerticalAxis;

        public string GraphTitle {
            get => graphTitle;
            set => SetProperty(ref graphTitle, value);
        }
        private string graphTitle = string.Empty;

        public string HorizontalTitle {
            get => horizontalTitle;
            set => SetProperty(ref horizontalTitle, value);
        }
        private string horizontalTitle;

        public string VerticalTitle {
            get => verticalTitle;
            set => SetProperty(ref verticalTitle, value);
        }
        private string verticalTitle;

        public string LabelProperty {
            get => labelProperty;
            set => SetProperty(ref labelProperty, value);
        }
        private string labelProperty;

        public ChromatogramPeakFeatureModel Target {
            get => Model.Target;
            set {
                if (Model.Target == value) {
                    return;
                }
                Model.Target = value;
                OnPropertyChanged(nameof(Target));
            }
        }
    }
}
