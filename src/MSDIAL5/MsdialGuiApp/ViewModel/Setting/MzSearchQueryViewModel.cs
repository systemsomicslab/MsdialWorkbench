using CompMs.Common.Query;
using CompMs.CommonMVVM;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    /// <summary>
    /// A row of the exclusion mass list. An edit that passes validation is written to the query at once, as the other
    /// peak detection settings are written to their model, so committing the model commits what the grid shows.
    /// </summary>
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

        [Range(0d, double.MaxValue, ErrorMessage = "Mass should be zero or positive value.")]
        public double Mass {
            get => mass;
            set {
                if (SetProperty(ref mass, value) && !ContainsError(nameof(Mass))) {
                    Model.Mass = value;
                }
            }
        }
        private double mass;

        public double RelativeIntensity {
            get => relativeIntensity;
            set {
                if (SetProperty(ref relativeIntensity, value)) {
                    Model.RelativeIntensity = value;
                }
            }
        }
        private double relativeIntensity;

        public SearchType SearchType {
            get => searchType;
            set {
                if (SetProperty(ref searchType, value)) {
                    Model.SearchType = value;
                }
            }
        }
        private SearchType searchType;

        [Range(0.0001d, double.MaxValue, ErrorMessage = "Tolerance should be positive value.")]
        public double MassTolerance {
            get => massTolerance;
            set {
                if (SetProperty(ref massTolerance, value) && !ContainsError(nameof(MassTolerance))) {
                    Model.MassTolerance = value;
                }
            }
        }
        private double massTolerance;

        // The setting view models read HasValidationErrors when a row raises PropertyChanged,
        // and SetProperty raises it before it validates the new value.
        protected override void OnErrorsChanged([CallerMemberName] string propertyname = "") {
            base.OnErrorsChanged(propertyname);
            OnPropertyChanged(nameof(HasValidationErrors));
        }
    }
}
