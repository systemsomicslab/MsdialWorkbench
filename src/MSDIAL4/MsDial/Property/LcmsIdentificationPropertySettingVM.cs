using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace Rfx.Riken.OsakaUniv
{
    public class LcmsIdentificationPropertySettingVM : ViewModelBase
    {
        private MainWindow mainWindow;
        private Window window;
        private ObservableCollection<AlignmentPropertyBean> alignmentPropertyBeanViewModelCollection;
        private ObservableCollection<AlignmentPropertyBean> originalAlignmentPropertyBeanCollection;

        public ObservableCollection<AlignmentPropertyBean> AlignmentPropertyBeanViewModelCollection
        {
            get { return alignmentPropertyBeanViewModelCollection; }
            set { alignmentPropertyBeanViewModelCollection = value; }
        }

        public LcmsIdentificationPropertySettingVM(MainWindow mainWindow, Window window)
        {
            this.mainWindow = mainWindow;
            this.window = window;
            this.alignmentPropertyBeanViewModelCollection = new ObservableCollection<AlignmentPropertyBean>();
            this.originalAlignmentPropertyBeanCollection = mainWindow.FocusedAlignmentResult.AlignmentPropertyBeanCollection;

            for (int i = 0; i < mainWindow.FocusedAlignmentResult.AlignmentPropertyBeanCollection.Count; i++)
            {
                this.alignmentPropertyBeanViewModelCollection.Add(new AlignmentPropertyBean()
                {
                    AlignmentID = mainWindow.FocusedAlignmentResult.AlignmentPropertyBeanCollection[i].AlignmentID,
                    CentralAccurateMass = mainWindow.FocusedAlignmentResult.AlignmentPropertyBeanCollection[i].CentralAccurateMass,
                    CentralRetentionTime = mainWindow.FocusedAlignmentResult.AlignmentPropertyBeanCollection[i].CentralRetentionTime,
                    MsmsIncluded = mainWindow.FocusedAlignmentResult.AlignmentPropertyBeanCollection[i].MsmsIncluded,
                    MetaboliteName = mainWindow.FocusedAlignmentResult.AlignmentPropertyBeanCollection[i].MetaboliteName,
                    AdductIonName = mainWindow.FocusedAlignmentResult.AlignmentPropertyBeanCollection[i].AdductIonName
                });
            }
        }

        protected override void executeCommand(object parameter)
        {
            base.executeCommand(parameter);

            closingMethod();
            window.Close();
        }

        private void closingMethod()
        {
            for (int i = 0; i < this.alignmentPropertyBeanViewModelCollection.Count; i++)
            {
                this.originalAlignmentPropertyBeanCollection[this.alignmentPropertyBeanViewModelCollection[i].AlignmentID].AdductIonName = this.alignmentPropertyBeanViewModelCollection[i].AdductIonName;
                this.originalAlignmentPropertyBeanCollection[this.alignmentPropertyBeanViewModelCollection[i].AlignmentID].MetaboliteName = this.alignmentPropertyBeanViewModelCollection[i].MetaboliteName;
            }
            this.mainWindow.WindowOpened = false;
        }
    }
}
