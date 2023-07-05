using CompMs.CommonMVVM;

namespace CompMs.App.Msdial.Model.Search
{
    internal sealed class ValueFilterModel : BindableBase
    {
        public double Lower {
            get => _lower;
            set => SetProperty(ref _lower, value);
        }
        private double _lower;

        public double Upper {
            get => _upper;
            set => SetProperty(ref _upper, value);
        }
        private double _upper;

        public bool Contains(double value) {
            return Lower <= value && value <= Upper;
        }
    }
}
