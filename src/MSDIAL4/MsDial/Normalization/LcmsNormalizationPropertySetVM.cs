using Msdial.Lcms.Dataprocess.Utility;
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
    public class LcmsNormalizationProperty : ViewModelBase {
        public float MZ { get; set; }
        public float RT { get; set; }
        public float Mobility { get; set; }
        public int ID { get; set; }
        public string MetaboliteName { get; set; }
        public string Adduct { get; set; }

        private int internalStandardID;
        public int InternalStandardID {
            get {
                return internalStandardID;
            }

            set {
                internalStandardID = value; OnPropertyChanged("InternalStandardID");
            }
        }

    }


    public class LcmsNormalizationPropertySetVM : ViewModelBase
    {
        private MainWindow mainWindow;
        private Window window;
        //private ObservableCollection<AlignmentPropertyBean> alignmentPropertyBeanViewModelCollection;
        //private ObservableCollection<AlignmentPropertyBean> originalAlignmentPropertyBeanCollection;
        private ObservableCollection<LcmsNormalizationProperty> alignmentPropertyBeanViewModelCollection;

        public LcmsNormalizationPropertySetVM(MainWindow mainWindow, Window window)
        {
            this.mainWindow = mainWindow;
            this.window = window;
            this.alignmentPropertyBeanViewModelCollection = new ObservableCollection<LcmsNormalizationProperty>();
            var alignedSpots = mainWindow.FocusedAlignmentResult.AlignmentPropertyBeanCollection;
            var param = this.mainWindow.AnalysisParamForLC;

            foreach (var spot in alignedSpots) {
                if (param.IsIonMobility) {
                    foreach (var dSpot in spot.AlignedDriftSpots) {
                        var prop = new LcmsNormalizationProperty() {
                            MZ = dSpot.CentralAccurateMass,
                            RT = spot.CentralRetentionTime,
                            Mobility = dSpot.CentralDriftTime,
                            ID = dSpot.MasterID,
                            Adduct = dSpot.AdductIonName,
                            InternalStandardID = dSpot.InternalStandardAlignmentID,
                            MetaboliteName = dSpot.MetaboliteName
                        };
                        this.alignmentPropertyBeanViewModelCollection.Add(prop);
                    }
                }
                else {
                    var prop = new LcmsNormalizationProperty() {
                        MZ = spot.CentralAccurateMass,
                        RT = spot.CentralRetentionTime,
                        ID = spot.AlignmentID,
                        Adduct = spot.AdductIonName,
                        InternalStandardID = spot.InternalStandardAlignmentID,
                        MetaboliteName = spot.MetaboliteName
                    };
                    this.alignmentPropertyBeanViewModelCollection.Add(prop);
                }
            }


            //for (int i = 0; i < mainWindow.FocusedAlignmentResult.AlignmentPropertyBeanCollection.Count; i++)
            //{
            //    this.alignmentPropertyBeanViewModelCollection.Add(new AlignmentPropertyBean()
            //    {
            //        CentralAccurateMass = mainWindow.FocusedAlignmentResult.AlignmentPropertyBeanCollection[i].CentralAccurateMass,
            //        CentralRetentionTime = mainWindow.FocusedAlignmentResult.AlignmentPropertyBeanCollection[i].CentralRetentionTime,
            //        AlignmentID = mainWindow.FocusedAlignmentResult.AlignmentPropertyBeanCollection[i].AlignmentID,
            //        MetaboliteName = mainWindow.FocusedAlignmentResult.AlignmentPropertyBeanCollection[i].MetaboliteName,
            //        AdductIonName = mainWindow.FocusedAlignmentResult.AlignmentPropertyBeanCollection[i].AdductIonName,
            //        InternalStandardAlignmentID = mainWindow.FocusedAlignmentResult.AlignmentPropertyBeanCollection[i].InternalStandardAlignmentID
            //    });
            //}
        }

        public ObservableCollection<LcmsNormalizationProperty> AlignmentPropertyBeanViewModelCollection
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
            var propCollection = this.alignmentPropertyBeanViewModelCollection;
            var alignedSpots = mainWindow.FocusedAlignmentResult.AlignmentPropertyBeanCollection;
            var globalSpots = DataAccessLcUtility.GetAlignmentPropertyBeanAndAlignedDriftSpotBeanMergedList(alignedSpots);

            var param = this.mainWindow.AnalysisParamForLC;
            if (param.IsIonMobility) {
                foreach (var prop in propCollection) {
                    if (prop.InternalStandardID >= 0 && prop.InternalStandardID < globalSpots.Count) {
                        var spot = globalSpots[prop.InternalStandardID];
                        if (spot.GetType() == typeof(AlignmentPropertyBean)) {
                            MessageBox.Show("Please add the peak ID of ion mobility axis instead of that of retention time axis", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                }

                foreach (var prop in propCollection) {
                    if (prop.InternalStandardID >= 0 && prop.InternalStandardID < globalSpots.Count) {
                        var dSpot = (AlignedDriftSpotPropertyBean)globalSpots[prop.ID];
                        dSpot.InternalStandardAlignmentID = prop.InternalStandardID;

                        var isDspot = (AlignedDriftSpotPropertyBean)globalSpots[prop.InternalStandardID];
                        alignedSpots[dSpot.AlignmentSpotID].InternalStandardAlignmentID = alignedSpots[isDspot.AlignmentSpotID].MasterID;
                    }
                }

                //// check
                //foreach (var spot in globalSpots) {
                //    if (spot.GetType() == typeof(AlignmentPropertyBean)) {
                //        var rSpot = (AlignmentPropertyBean)spot;
                //        if (rSpot.InternalStandardAlignmentID < 0) {
                //            Console.WriteLine(rSpot.MasterID);
                //        }
                //    }
                //    else {
                //        var dSpot = (AlignedDriftSpotPropertyBean)spot;
                //        if (dSpot.InternalStandardAlignmentID < 0) {
                //            Console.WriteLine(dSpot.MasterID);
                //        }
                //    }
                //}

            }
            else {
                foreach (var prop in propCollection) {
                    if (prop.InternalStandardID >= 0 && prop.InternalStandardID < globalSpots.Count) {
                        var spot = alignedSpots[prop.ID];
                        spot.InternalStandardAlignmentID = prop.InternalStandardID;
                    }
                }
            }


            //for (int i = 0; i < this.alignmentPropertyBeanViewModelCollection.Count; i++)
            //{
            //    this.originalAlignmentPropertyBeanCollection[i].InternalStandardAlignmentID = this.alignmentPropertyBeanViewModelCollection[i].InternalStandardAlignmentID;
            //}
            this.mainWindow.WindowOpened = false;
        }
    }
}
