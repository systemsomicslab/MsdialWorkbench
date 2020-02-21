using Riken.Metabolomics.MsfinderCommon.Utility;
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
        private MainWindowVM mainWindowVM;
        private MainWindow mainWindow;

        private AdductIon adductIon;
        private string adductString;
        private bool isEnabled;

        public UserDefinedAdductVM(Window window, MainWindowVM mainWindowVM, MainWindow mainWindow)
        {
            this.window = window;
            this.mainWindowVM = mainWindowVM;
            this.mainWindow = mainWindow;
            this.adductIon = new AdductIon();

            this.isEnabled = false;
        }

        protected override void executeCommand(object parameter)
        {
            base.executeCommand(parameter);

            if (this.adductIon.IonMode == IonMode.Positive)
            {
                this.mainWindowVM.AdductPositiveResources.Add(this.adductIon);
                AdductListParcer.WriteAdductPositiveResources(this.mainWindowVM.AdductPositiveResources);
            }
            else if (this.adductIon.IonMode == IonMode.Negative)
            {
                this.mainWindowVM.AdductNegativeResources.Add(this.adductIon);
                AdductListParcer.WriteAdductNegativeResources(this.mainWindowVM.AdductNegativeResources);
            }

            var rawDataVM = this.mainWindowVM.RawDataVM;
            var rawDataFileID = this.mainWindowVM.SelectedRawFileId;
            if (rawDataVM != null && rawDataFileID >= 0)
            {
                RefreshUtility.UpdataFiles(this.mainWindowVM, rawDataFileID);
                RefreshUtility.RawDataFileRefresh(this.mainWindowVM, rawDataFileID);
                RefreshUtility.FormulaDataFileRefresh(this.mainWindow, this.mainWindowVM, rawDataFileID);
                RefreshUtility.StructureDataFileRefresh(this.mainWindow, this.mainWindowVM, rawDataFileID);
                RefreshUtility.RawDataMassSpecUiRefresh(this.mainWindow, this.mainWindowVM);
            }

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
