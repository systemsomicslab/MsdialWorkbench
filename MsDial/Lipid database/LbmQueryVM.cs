using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rfx.Riken.OsakaUniv
{
    public class LbmQueryVM : ViewModelBase
    {
        private LbmClass lbmClass;
        private AdductIon adductIon;
        private IonMode ionMode;
        private bool isSelected;

        public LbmQueryVM(LbmQuery query)
        {
            this.lbmClass = query.LbmClass;
            this.adductIon = query.AdductIon;
            this.IonMode = query.IonMode;
            this.isSelected = query.IsSelected;
        }

        public LbmClass LbmClass
        {
            get { return lbmClass; }
            set { if (lbmClass == value) return; lbmClass = value; OnPropertyChanged("LbmClass"); }
        }

        public AdductIon AdductIon
        {
            get { return adductIon; }
            set { if (adductIon == value) return; adductIon = value; OnPropertyChanged("AdductIon"); }
        }

        public IonMode IonMode
        {
            get { return ionMode; }
            set { if (ionMode == value) return; ionMode = value; OnPropertyChanged("IonMode"); }
        }

        public bool IsSelected
        {
            get { return isSelected; }
            set { if (isSelected == value) return; isSelected = value; OnPropertyChanged("IsSelected"); }
        }

    }
}
