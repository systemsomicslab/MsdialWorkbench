using CompMs.CommonMVVM;

namespace CompMs.App.Msdial.ViewModel.DataObj
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

        public bool IsValid => Mass.HasValue && Tolerance.HasValue && Mass > 0 && Tolerance > 0;
    }
}
