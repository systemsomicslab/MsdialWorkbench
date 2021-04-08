using CompMs.App.Msdial.Model;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using System.ComponentModel;
using System.Collections.Generic;
using System.Windows.Data;
using System.Windows;

namespace CompMs.App.Msdial.ViewModel
{
    public class AlignmentPeakPlotVM : ViewModelBase {

        public AlignmentPeakPlotVM(
            AlignmentPeakPlotModel model,
            string horizontalProperty,
            string verticalProperty,
            string graphTitle,
            string horizontalTitle,
            string verticalTitle) {

            Model = model;
            // WeakEventManager<AlignmentPeakPlotModel, PropertyChangedEventArgs>.AddHandler(Model, "PropertyChanged", (s, e) => this.spots = null);

            HorizontalProperty = horizontalProperty;
            VerticalProperty = verticalProperty;
            GraphTitle = graphTitle;
            HorizontalTitle = horizontalTitle;
            VerticalTitle = verticalTitle;
        }

        public AlignmentPeakPlotVM(
            IList<AlignmentSpotPropertyModel> spots,
            IAxisManager horizontalAxis,
            IAxisManager verticalAxis,
            string horizontalProperty,
            string verticalProperty,
            string graphTitle,
            string horizontalTitle,
            string verticalTitle)
            : this(new AlignmentPeakPlotModel(spots, horizontalAxis, verticalAxis),
                  horizontalProperty,
                  verticalProperty,
                  graphTitle,
                  horizontalTitle,
                  verticalTitle) {
        }

        public AlignmentPeakPlotModel Model { get; }

        public ICollectionView Spots {
            get => spots ?? (spots = CollectionViewSource.GetDefaultView(Model.Spots));
        }
        private ICollectionView spots;

        public string HorizontalProperty { get; }

        public string VerticalProperty { get; }

        public IAxisManager HorizontalAxis {
            get => Model.HorizontalAxis;
        }

        public IAxisManager VerticalAxis {
            get => Model.VerticalAxis;
        }

        public string HorizontalTitle { get; }

        public string VerticalTitle { get; }

        public string GraphTitle { get; }

        public AlignmentSpotPropertyModel Target {
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
