using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace Rfx.Riken.OsakaUniv
{
    public class ExcludeMassVM : ViewModelBase
    {
        private ExcludeMassBean excludedMassBean;

        public ExcludeMassVM()
        {
            this.excludedMassBean = new ExcludeMassBean();
        }

        public float? ExcludedMass
        {
            get { return this.excludedMassBean.ExcludedMass; }
            set { if (this.excludedMassBean.ExcludedMass == value) return; this.excludedMassBean.ExcludedMass = value; OnPropertyChanged("ExcludedMass"); }
        }

        public float? MassTolerance
        {
            get { return this.excludedMassBean.MassTolerance; }
            set { if (this.excludedMassBean.MassTolerance == value) return; this.excludedMassBean.MassTolerance = value; OnPropertyChanged("MassTolerance"); }
        }
    }
}
