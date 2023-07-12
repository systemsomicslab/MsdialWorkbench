using CompMs.CommonMVVM;

namespace CompMs.App.Msdial.Model.Search
{
    internal sealed class ValueFilterModel : BindableBase
    {
        public ValueFilterModel() {
            
        }

        public ValueFilterModel(string label, double minimum, double maximum) {
            Label = label;
            Lower = Minimum = minimum;
            Upper = Maximum = maximum;
        }

        public string Label { get; }
        public double Minimum {
            get => _minimum;
            set {
                if (SetProperty(ref _minimum, value)) {
                    Lower = value;
                }
            }
        }
        private double _minimum;

        public double Maximum {
            get => _maximum;
            set {
                if (SetProperty(ref _maximum, value)) {
                    Upper = value;
                }
            }
        }
        private double _maximum;

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
