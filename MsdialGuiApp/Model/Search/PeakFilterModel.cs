using CompMs.CommonMVVM;

namespace CompMs.App.Msdial.Model.Search
{
    public class PeakFilterModel : BindableBase
    {
        public PeakFilterModel(DisplayFilter enabledFilter) {
            EnabledFilter = enabledFilter;
        }

        public DisplayFilter EnabledFilter { get; }

        public DisplayFilter CheckedFilter {
            get => checkedFilter;
            set => SetProperty(ref checkedFilter, value & EnabledFilter);
        }
        private DisplayFilter checkedFilter;
    }
}
