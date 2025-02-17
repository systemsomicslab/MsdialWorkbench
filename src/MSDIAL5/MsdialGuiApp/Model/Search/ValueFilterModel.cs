using CompMs.CommonMVVM;

namespace CompMs.App.Msdial.Model.Search
{
    public sealed class ValueFilterModel : BindableBase
    {
        public ValueFilterModel(string label, double minimum, double maximum) {
            Label = label;
            Lower = Minimum = minimum;
            Upper = Maximum = maximum;
            _isEnabled = false;
        }

        public string Label { get; }
        public double Minimum {
            get => _minimum;
            set {
                if (SetProperty(ref _minimum, value)) {
                    Lower = value;
                    IsEnabled = Maximum != Upper;
                }
            }
        }
        private double _minimum;

        public double Maximum {
            get => _maximum;
            set {
                if (SetProperty(ref _maximum, value)) {
                    Upper = value;
                    IsEnabled = Minimum != Lower;
                }
            }
        }
        private double _maximum;

        public double Lower {
            get => _lower;
            set {
                if (SetProperty(ref _lower, value)) {
                    IsEnabled = Minimum != _lower || Maximum != Upper;
                }
            }
        }
        private double _lower;

        public double Upper {
            get => _upper;
            set {
                if (SetProperty(ref _upper, value)) {
                    IsEnabled = Minimum != Lower || Maximum != _upper;
                }
            }
        }
        private double _upper;

        public bool Contains(double value) {
            return Lower <= value && value <= Upper;
        }

        public bool IsEnabled {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }
        private bool _isEnabled;
    }
}
