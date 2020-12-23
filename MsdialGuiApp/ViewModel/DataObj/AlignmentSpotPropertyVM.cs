using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.ViewModel.DataObj
{
    class AlignmentSpotPropertyVM
    {
        public int AlignmentID => innerModel.AlignmentID;
        public int MasterAlignmentID => innerModel.MasterAlignmentID;
        public double TimesCenter => innerModel.TimesCenter.Value;
        public double MassCenter => innerModel.MassCenter;

        private readonly AlignmentSpotProperty innerModel;

        public AlignmentSpotPropertyVM(AlignmentSpotProperty innerModel) {
            this.innerModel = innerModel;
        }
    }
}
