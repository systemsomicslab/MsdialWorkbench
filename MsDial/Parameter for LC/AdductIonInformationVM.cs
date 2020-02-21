using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace Rfx.Riken.OsakaUniv
{
    public class AdductIonInformationVM : ViewModelBase
    {
        private AdductIonInformationBean adductIonInformationBean;

        public AdductIonInformationBean AdductIonInformationBean
        {
            get { return adductIonInformationBean; }
            set { adductIonInformationBean = value; }
        }

        public string AdductName
        {
            get { return AdductIonInformationBean.AdductName; }
            set { AdductIonInformationBean.AdductName = value; }
        }

        public int Charge
        {
            get { return AdductIonInformationBean.Charge; }
            set { AdductIonInformationBean.Charge = value; }
        }

        public double AccurateMass
        {
            get { return AdductIonInformationBean.AccurateMass; }
            set { AdductIonInformationBean.AccurateMass = value; }
        }

        public bool Included
        {
            get { return AdductIonInformationBean.Included; }
            set { AdductIonInformationBean.Included = value; OnPropertyChanged("Included"); ValidationMethod(); }
        }

        private void ValidationMethod()
        {
            if (this.adductIonInformationBean == null) return;
            if ((this.adductIonInformationBean.AdductName == "M+H" || this.adductIonInformationBean.AdductName == "M-H") && this.adductIonInformationBean.Included == false)
                this.adductIonInformationBean.Included = true;
        }
    }
}
