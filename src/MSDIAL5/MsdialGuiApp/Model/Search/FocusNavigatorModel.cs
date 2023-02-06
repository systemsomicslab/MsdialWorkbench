using CompMs.CommonMVVM;
using System.Collections.ObjectModel;

namespace CompMs.App.Msdial.Model.Search
{
    public class FocusNavigatorModel : BindableBase
    {
        public FocusNavigatorModel(params ISpotFocus[] spotFocuses) {
            SpotFocuses = new ObservableCollection<ISpotFocus>(spotFocuses);
        }

        public ObservableCollection<ISpotFocus> SpotFocuses { get; }
    }
}
