using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CompMs.App.Msdial.Model.DataObj;

namespace CompMs.App.Msdial.ViewModel.DataObj
{
    internal class ProteinViewModel
    {
        public ProteinViewModel(ProteinModel model)
        {
            _model = model;
            DatabaseId = model.DatabaseId;

        }
        private readonly ProteinModel _model;
        public string DatabaseId { get; }

    }
}
