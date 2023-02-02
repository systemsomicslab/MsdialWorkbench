using CompMs.Common.Query;
using CompMs.CommonMVVM;
using System.ComponentModel.DataAnnotations;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    public class MzSearchQueryViewModel : ViewModelBase
    {
        public MzSearchQueryViewModel(MzSearchQuery model) {
            Model = model;

            Mass = model.Mass;
            RelativeIntensity = model.RelativeIntensity;
            SearchType = model.SearchType;
            MassTolerance = model.MassTolerance;
        }

        public MzSearchQuery Model { get; }

        [RegularExpression(@"\d+\.?\d*", ErrorMessage = "Invalid format.")]
        public double Mass {
            get => mass;
            set => SetProperty(ref mass, value);
        }
        private double mass;

        public double RelativeIntensity {
            get => relativeIntensity;
            set => SetProperty(ref relativeIntensity, value);
        }
        private double relativeIntensity;

        public SearchType SearchType {
            get => searchType;
            set => SetProperty(ref searchType, value);
        }
        private SearchType searchType;

        [RegularExpression(@"0?\.\d+", ErrorMessage = "Invalid format.")]
        public double MassTolerance {
            get => massTolerance;
            set => SetProperty(ref massTolerance, value);
        }
        private double massTolerance;

        public void Commit() {
            Model.Mass = Mass;
            Model.RelativeIntensity = RelativeIntensity;
            Model.SearchType = SearchType;
            Model.MassTolerance = MassTolerance;
        }
    }
}
