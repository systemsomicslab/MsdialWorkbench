using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;

namespace Rfx.Riken.OsakaUniv
{
    public class GcmsNormalizationPropertySetVM : ViewModelBase
    {
        private MainWindow mainWindow;
        private Window window;
        private ObservableCollection<AlignmentPropertyBean> alignmentPropertyBeanViewModelCollection;
        private ObservableCollection<AlignmentPropertyBean> originalAlignmentPropertyBeanCollection;

        public GcmsNormalizationPropertySetVM(MainWindow mainWindow, Window window)
        {
            this.mainWindow = mainWindow;
            this.window = window;
            this.alignmentPropertyBeanViewModelCollection = new ObservableCollection<AlignmentPropertyBean>();
            this.originalAlignmentPropertyBeanCollection = mainWindow.FocusedAlignmentResult.AlignmentPropertyBeanCollection;

            for (int i = 0; i < mainWindow.FocusedAlignmentResult.AlignmentPropertyBeanCollection.Count; i++)
            {
                this.alignmentPropertyBeanViewModelCollection.Add(new AlignmentPropertyBean()
                {
                    CentralRetentionIndex = mainWindow.FocusedAlignmentResult.AlignmentPropertyBeanCollection[i].CentralRetentionIndex,
                    CentralRetentionTime = mainWindow.FocusedAlignmentResult.AlignmentPropertyBeanCollection[i].CentralRetentionTime,
                    AlignmentID = mainWindow.FocusedAlignmentResult.AlignmentPropertyBeanCollection[i].AlignmentID,
                    MetaboliteName = mainWindow.FocusedAlignmentResult.AlignmentPropertyBeanCollection[i].MetaboliteName,
                    QuantMass = mainWindow.FocusedAlignmentResult.AlignmentPropertyBeanCollection[i].QuantMass,
                    InternalStandardAlignmentID = mainWindow.FocusedAlignmentResult.AlignmentPropertyBeanCollection[i].InternalStandardAlignmentID
                });
            }
        }

        public ObservableCollection<AlignmentPropertyBean> AlignmentPropertyBeanViewModelCollection
        {
            get { return alignmentPropertyBeanViewModelCollection; }
            set { alignmentPropertyBeanViewModelCollection = value; }
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
                this.originalAlignmentPropertyBeanCollection[i].InternalStandardAlignmentID = this.alignmentPropertyBeanViewModelCollection[i].InternalStandardAlignmentID;
            }
            this.mainWindow.WindowOpened = false;
        }
    }
}
