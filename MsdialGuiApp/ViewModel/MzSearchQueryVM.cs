using CompMs.CommonMVVM;

namespace CompMs.App.Msdial.ViewModel
{
    public class MzSearchQueryVM : ViewModelBase
    {
        public double? Mass {
            get => mass;
            set => SetProperty(ref mass, value);
        }
        public double? Tolerance {
            get => tolerance;
            set => SetProperty(ref tolerance, value);
        }

        private double? mass, tolerance;
    }
}
