using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace Rfx.Riken.OsakaUniv
{
    public class UserDefinedAdductVM : ViewModelBase
    {
        private Window window;
        private MainWindow mainWindow;

        private AdductIon adductIon;
        private string adductString;
        private bool isEnabled;

        public UserDefinedAdductVM(Window window, MainWindow mainWindow)
        {
            this.window = window;
            this.mainWindow = mainWindow;
            this.adductIon = new AdductIon();

            if (mainWindow.ProjectProperty.IonMode == IonMode.Negative) this.adductIon.IonMode = IonMode.Negative;

            this.isEnabled = false;
        }

        protected override void executeCommand(object parameter)
        {
            base.executeCommand(parameter);

            var adductInfo = new AdductIonInformationBean()
            {
                AccurateMass = this.adductIon.AdductIonAccurateMass,
                Xmer = this.adductIon.AdductIonXmer,
                AdductName = this.adductIon.AdductIonName,
                Charge = this.adductIon.ChargeNumber,
                IonMode = this.adductIon.IonMode,
                Included = true
            };

            this.mainWindow.AnalysisParamForLC.AdductIonInformationBeanList.Add(adductInfo);
            this.window.DialogResult = true;
            this.window.Close();
        }

        public string AdductString
        {
            get { return adductString; }
            set { if (adductString == value) return; adductString = value; OnPropertyChanged("AdductString"); UpdateVM(); }
        }

        public bool IsEnabled
        {
            get { return isEnabled; }
            set { isEnabled = value; OnPropertyChanged("IsEnabled"); }
        }

        private void UpdateVM()
        {
            this.adductIon = AdductIonParcer.GetAdductIonBean(AdductString);
            if (this.adductIon.FormatCheck == false) IsEnabled = false;
            else IsEnabled = true;

            Mass = adductIon.AdductIonAccurateMass;
            Xmer = adductIon.AdductIonXmer;
            Charge = adductIon.ChargeNumber;
            IonMode = adductIon.IonMode;
        }

        public double Mass
        {
            get { return Math.Round(adductIon.AdductIonAccurateMass, 4); }
            set
            {
                OnPropertyChanged("Mass");
            }
        }

        public int Xmer
        {
            get { return adductIon.AdductIonXmer; }
            set
            {
                OnPropertyChanged("Xmer");
            }
        }

        public int Charge
        {
            get { return adductIon.ChargeNumber; }
            set
            {
                OnPropertyChanged("Charge");
            }
        }

        public IonMode IonMode
        {
            get { return adductIon.IonMode; }
            set
            {
                OnPropertyChanged("IonMode");
            }
        }
    }
}
