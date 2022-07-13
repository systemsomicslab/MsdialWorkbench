using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.CommonMVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.ViewModel.Table
{
    internal class ProteinGroupTableViewModel : ViewModelBase
    {
        ProteinGroupTableViewModel(ProteinGroupModel model) {
            GroupId = model.GroupID;

        }

        public int GroupId { get; }
        public ReadOnlyObservableCollection<ProteinGroupViewModel> Groups { get; }
    }
}
