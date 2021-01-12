using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.ViewModel.DataObj
{
    public class AlignmentSpotPropertyVM
    {
        public int AlignmentID => innerModel.AlignmentID;
        public int MasterAlignmentID => innerModel.MasterAlignmentID;
        public double TimesCenter => innerModel.TimesCenter.Value;
        public double MassCenter => innerModel.MassCenter;
        public double HeightAverage => innerModel.HeightAverage;
        public ReadOnlyCollection<AlignmentChromPeakFeature> AlignedPeakProperties => innerModel.AlignedPeakProperties.AsReadOnly();

        private readonly AlignmentSpotProperty innerModel;

        public AlignmentSpotPropertyVM(AlignmentSpotProperty innerModel) {
            this.innerModel = innerModel;
        }
    }
}
