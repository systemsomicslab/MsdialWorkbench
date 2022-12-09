using Msdial.Lcms.Dataprocess.Utility;
using Riken.Metabolomics.Lipidomics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;

namespace Rfx.Riken.OsakaUniv
{
    public sealed class MsDialDataNormalization
    {
        private MsDialDataNormalization() { }

        public static bool NormalizationFormatCheck(MainWindow mainWindow, Window window, bool isNone, bool isIS, bool isLowess, bool isIsLowess, bool isTIC, bool isMTic) {
            var files = mainWindow.AnalysisFiles;
            var alignResult = mainWindow.FocusedAlignmentResult;

            if (isIS || isIsLowess) {
                if (!InternalStandardCheck(alignResult)) return false;
            }

            if (isLowess || isIsLowess) {
                if (!QualityControlCheck(files) || !AnalyticalOrderCheck(files)) return false;

                var spanWindow = new LowessSpanSetWin(mainWindow);
                spanWindow.Owner = window;
                spanWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                if (spanWindow.ShowDialog() == true) {
                    return true;
                }
                else {
                    return false;
                }
            }

            return true;
        }

        public static void MainProcess(MainWindow mainWindow, bool isNone, bool isIS, bool isLowess, bool isIsLowess, bool isTIC, bool isMTic, double lowessSpan) {
            var files = mainWindow.AnalysisFiles;
            var alignResult = mainWindow.FocusedAlignmentResult;

            if (isNone || isLowess) NoneNormalize(alignResult);
            if (isIS || isIsLowess) InternalStandardNormalize(alignResult);

            if (isLowess || isIsLowess) LowessNormalize(files, alignResult, lowessSpan);

            if (isTIC) TicNormalization(alignResult);
            if (isMTic) MTicNormalization(alignResult);
        }

        public static double LowessSpanTune(MainWindow mainWindow, double lowessSpan) {
            var files = mainWindow.AnalysisFiles;
            var alignResult = mainWindow.FocusedAlignmentResult;

            var qcList = new List<double[]>();
            var minSpan = lowessSpan;
            var optSpanList = new List<double>();

            var batchDict = files.GroupBy(item => item.AnalysisFilePropertyBean.AnalysisBatch).ToDictionary(grp => grp.Key, grp => grp.ToList());
            foreach (var eachBatch in batchDict) {
                var analysisFileBeanCollectionPerBatch = eachBatch.Value;
                var alignedSpots = alignResult.AlignmentPropertyBeanCollection;
                var globalSpots = DataAccessLcUtility.GetAlignmentPropertyBeanAndAlignedDriftSpotBeanMergedList(alignedSpots);

                for (int i = 0; i < globalSpots.Count; i++) {
                    qcList = new List<double[]>();
                    for (int j = 0; j < analysisFileBeanCollectionPerBatch.Count; j++) {
                        var alignedProp = analysisFileBeanCollectionPerBatch[j].AnalysisFilePropertyBean;
                        if (alignedProp.AnalysisFileType == AnalysisFileType.QC && alignedProp.AnalysisFileIncluded) {

                            var variable = 0.0;
                            if (globalSpots[i].GetType() == typeof(AlignmentPropertyBean)) {
                                variable = ((AlignmentPropertyBean)globalSpots[i]).AlignedPeakPropertyBeanCollection[alignedProp.AnalysisFileId].Variable;
                            }
                            else {
                                variable = ((AlignedDriftSpotPropertyBean)globalSpots[i]).AlignedPeakPropertyBeanCollection[alignedProp.AnalysisFileId].Variable;
                            }
                            qcList.Add(new double[] { alignedProp.AnalysisFileAnalyticalOrder, variable });
                        }
                    }

                    qcList = qcList.OrderBy(n => n[0]).ToList();

                    var xQcArray = new double[qcList.Count];
                    var yQcArray = new double[qcList.Count];

                    for (int j = 0; j < qcList.Count; j++) { xQcArray[j] = qcList[j][0]; yQcArray[j] = qcList[j][1]; }


                    var recoSpan = SmootherMathematics.GetOptimalLowessSpanByCrossValidation(xQcArray, yQcArray, minSpan, 0.05, 3, 7);
                    optSpanList.Add(recoSpan);
                }
            }
            var optSpanArray = optSpanList.ToArray();
            var optSpan = BasicMathematics.Mean(optSpanArray);

            return Math.Round(optSpan, 2);
        }

        private static void MTicNormalization(AlignmentResultBean alignResult) {
            var fileCount = alignResult.AlignmentPropertyBeanCollection[0].AlignedPeakPropertyBeanCollection.Count;
            var isMobility = alignResult.AlignmentPropertyBeanCollection[0].AlignedDriftSpots != null && alignResult.AlignmentPropertyBeanCollection[0].AlignedDriftSpots.Count > 0;

            for (int i = 0; i < fileCount; i++) {
                double tic = 0.0;
                double ticOnDrift = 0.0;
                var alignedSpots = alignResult.AlignmentPropertyBeanCollection;
                for (int j = 0; j < alignedSpots.Count; j++) {
                    if ((alignedSpots[j].LibraryID >= 0 && !alignedSpots[j].MetaboliteName.Contains("w/o")) || alignedSpots[j].PostIdentificationLibraryID >= 0)
                        tic += alignedSpots[j].AlignedPeakPropertyBeanCollection[i].Variable;

                    if (isMobility) {
                        var driftSpots = alignedSpots[j].AlignedDriftSpots;
                        foreach (var spot in driftSpots.OrEmptyIfNull()) {
                            if (spot.LibraryID >= 0 || spot.PostIdentificationLibraryID >= 0) {
                                ticOnDrift += spot.AlignedPeakPropertyBeanCollection[i].Variable;
                            }
                        }
                    }
                }

                for (int j = 0; j < alignedSpots.Count; j++) {
                    alignedSpots[j].IonAbundanceUnit = IonAbundanceUnit.NormalizedByMTIC;
                    var properties = alignedSpots[j].AlignedPeakPropertyBeanCollection;
                    if (tic > 0)
                        properties[i].NormalizedVariable = (float)((double)properties[i].Variable / tic);
                    else
                        properties[i].NormalizedVariable = properties[i].Variable;

                    if (isMobility) {
                        var driftSpots = alignedSpots[j].AlignedDriftSpots;
                        foreach (var spot in driftSpots.OrEmptyIfNull()) {
                            spot.IonAbundanceUnit = IonAbundanceUnit.NormalizedByMTIC;
                            var driftProperties = spot.AlignedPeakPropertyBeanCollection;
                            if (ticOnDrift > 0)
                                driftProperties[i].NormalizedVariable = (float)((double)driftProperties[i].Variable / ticOnDrift);
                            else
                                driftProperties[i].NormalizedVariable = driftProperties[i].Variable;
                        }
                    }
                }
            }
        }

        private static void TicNormalization(AlignmentResultBean alignResult) {
            var fileCount = alignResult.AlignmentPropertyBeanCollection[0].AlignedPeakPropertyBeanCollection.Count;
            var isMobility = alignResult.AlignmentPropertyBeanCollection[0].AlignedDriftSpots != null && alignResult.AlignmentPropertyBeanCollection[0].AlignedDriftSpots.Count > 0;

            for (int i = 0; i < fileCount; i++) {
                double tic = 0.0;
                double ticOnDrift = 0.0;
                var alignedSpots = alignResult.AlignmentPropertyBeanCollection;

                for (int j = 0; j < alignedSpots.Count; j++) {
                    tic += alignedSpots[j].AlignedPeakPropertyBeanCollection[i].Variable;

                    if (isMobility) {
                        var driftSpots = alignedSpots[j].AlignedDriftSpots;
                        foreach (var spot in driftSpots.OrEmptyIfNull()) {
                            ticOnDrift += spot.AlignedPeakPropertyBeanCollection[i].Variable;
                        }
                    }
                }

                //for (int j = 0; j < alignResult.AlignmentPropertyBeanCollection.Count; j++) {
                //    tic += alignResult.AlignmentPropertyBeanCollection[j].AlignedPeakPropertyBeanCollection[i].Variable;
                //}

                for (int j = 0; j < alignedSpots.Count; j++) {
                    //if (tic > 0)
                    //    alignResult.AlignmentPropertyBeanCollection[j].AlignedPeakPropertyBeanCollection[i].NormalizedVariable = (float)((double)alignResult.AlignmentPropertyBeanCollection[j].AlignedPeakPropertyBeanCollection[i].Variable / tic);
                    //else
                    //    alignResult.AlignmentPropertyBeanCollection[j].AlignedPeakPropertyBeanCollection[i].NormalizedVariable = alignResult.AlignmentPropertyBeanCollection[j].AlignedPeakPropertyBeanCollection[i].Variable;

                    alignedSpots[j].IonAbundanceUnit = IonAbundanceUnit.NormalizedByTIC;
                    var properties = alignedSpots[j].AlignedPeakPropertyBeanCollection;
                    if (tic > 0)
                        properties[i].NormalizedVariable = (float)((double)properties[i].Variable / tic);
                    else
                        properties[i].NormalizedVariable = properties[i].Variable;

                    if (isMobility) {
                        var driftSpots = alignedSpots[j].AlignedDriftSpots;
                        foreach (var spot in driftSpots.OrEmptyIfNull()) {
                            spot.IonAbundanceUnit = IonAbundanceUnit.NormalizedByTIC;
                            var driftProperties = spot.AlignedPeakPropertyBeanCollection;
                            if (ticOnDrift > 0)
                                driftProperties[i].NormalizedVariable = (float)((double)driftProperties[i].Variable / ticOnDrift);
                            else
                                driftProperties[i].NormalizedVariable = driftProperties[i].Variable;
                        }
                    }
                }
            }
        }

        public static bool InternalStandardCheck(AlignmentResultBean alignmentResultBean) {

            var isMobility = alignmentResultBean.AnalysisParamForLC != null && alignmentResultBean.AnalysisParamForLC.IsIonMobility;
            var alignedSpots = alignmentResultBean.AlignmentPropertyBeanCollection;
            for (int i = 0; i < alignedSpots.Count; i++) {
                if (alignedSpots[i].InternalStandardAlignmentID < 0) {
                    MessageBox.Show("Please set your internal standard information in \"Option\" window.", "Attention", MessageBoxButton.OK, MessageBoxImage.Information);
                    return false;
                }

                if (isMobility) {
                    var driftSpots = alignedSpots[i].AlignedDriftSpots;
                    foreach (var spot in driftSpots.OrEmptyIfNull()) {
                        if (spot.InternalStandardAlignmentID < 0) {
                            MessageBox.Show("Please set your internal standard information in \"Option\" window.", "Attention", MessageBoxButton.OK, MessageBoxImage.Information);
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        public static void NoneNormalize(AlignmentResultBean alignmentResultBean) {
            var isMobility = alignmentResultBean.AnalysisParamForLC != null && alignmentResultBean.AnalysisParamForLC.IsIonMobility;
            var alignedSpots = alignmentResultBean.AlignmentPropertyBeanCollection;

            for (int i = 0; i < alignedSpots.Count; i++) {
                var properties = alignedSpots[i].AlignedPeakPropertyBeanCollection;
                alignedSpots[i].IonAbundanceUnit = IonAbundanceUnit.Height;
                for (int j = 0; j < properties.Count; j++) {
                    properties[j].NormalizedVariable = properties[j].Variable;
                }

                if (isMobility) {
                    var driftSpots = alignedSpots[i].AlignedDriftSpots;
                    foreach (var spot in driftSpots.OrEmptyIfNull()) {
                        spot.IonAbundanceUnit = IonAbundanceUnit.Height;
                        var driftProperties = spot.AlignedPeakPropertyBeanCollection;
                        foreach (var prop in driftProperties) {
                            prop.NormalizedVariable = prop.Variable;
                        }
                    }
                }
            }
        }

        public static void InternalStandardNormalize(AlignmentResultBean alignmentResultBean) {
            var isMobility = alignmentResultBean.AnalysisParamForLC != null && alignmentResultBean.AnalysisParamForLC.IsIonMobility;
            var alignedSpots = alignmentResultBean.AlignmentPropertyBeanCollection;
            var globalSpots = DataAccessLcUtility.GetAlignmentPropertyBeanAndAlignedDriftSpotBeanMergedList(alignedSpots);
            
            for (int i = 0; i < globalSpots.Count; i++) {
                var spot = globalSpots[i];
                var properties = DataAccessLcUtility.GetAlignedPeakPropertyBeanCollection(spot);
                var isID = DataAccessLcUtility.GetInternalStanderdId(spot);
                var isSpot = globalSpots[isID];
                var isProperties = DataAccessLcUtility.GetAlignedPeakPropertyBeanCollection(isSpot);

                if (spot.GetType() == typeof(AlignmentPropertyBean)) {
                    ((AlignmentPropertyBean)spot).IonAbundanceUnit = IonAbundanceUnit.NormalizedByInternalStandardPeakHeight;
                }
                else {
                    ((AlignedDriftSpotPropertyBean)spot).IonAbundanceUnit = IonAbundanceUnit.NormalizedByInternalStandardPeakHeight;
                }

                for (int j = 0; j < properties.Count; j++) {
                    var alignedProp = properties[j];
                    var isAlignedProp = isProperties[j];
                    if (isAlignedProp.Variable <= 0)
                        alignedProp.NormalizedVariable = 0.0F;
                    else
                        alignedProp.NormalizedVariable = (float)((double)alignedProp.Variable / (double)isAlignedProp.Variable);
                }
            }
        }

        public static bool QualityControlCheck(ObservableCollection<AnalysisFileBean> analysisFileBeanCollection) {
            var batchDict = analysisFileBeanCollection.GroupBy(item => item.AnalysisFilePropertyBean.AnalysisBatch).ToDictionary(grp => grp.Key, grp => grp.ToList());
            foreach (var eachBatch in batchDict) {
                int counter = 0;
                for (int i = 0; i < analysisFileBeanCollection.Count; i++)
                    if (analysisFileBeanCollection[i].AnalysisFilePropertyBean.AnalysisFileType == AnalysisFileType.QC && analysisFileBeanCollection[i].AnalysisFilePropertyBean.AnalysisFileIncluded) counter++;
                if (counter < 2) { MessageBox.Show("LOESS normalization requires at least 2 QC samples in each batch. Please set them in the option setting window.", "Error", MessageBoxButton.OK, MessageBoxImage.Error); return false; }
            }
            return true;
        }

        public static bool AnalyticalOrderCheck(ObservableCollection<AnalysisFileBean> analysisFileBeanCollection) {
            var batchDict = analysisFileBeanCollection.GroupBy(item => item.AnalysisFilePropertyBean.AnalysisBatch).ToDictionary(grp => grp.Key, grp => grp.ToList());
            foreach (var eachBatch in batchDict) {
                List<int> analyticalOrderList = new List<int>();
                for (int i = 0; i < eachBatch.Value.Count; i++) { analyticalOrderList.Add(eachBatch.Value[i].AnalysisFilePropertyBean.AnalysisFileAnalyticalOrder); }
                analyticalOrderList = analyticalOrderList.OrderBy(n => n).ToList();
                for (int i = 0; i < analyticalOrderList.Count - 1; i++)
                    if (analyticalOrderList[i] == analyticalOrderList[i + 1]) {
                        MessageBox.Show("The independent value of analytical order should be set for LOESS normalization. Please set them in the option setting window.\n Error: batch: "
                            + eachBatch.Key + ", order: " + analyticalOrderList[i], "Error", MessageBoxButton.OK, MessageBoxImage.Error); return false;
                    }

                int minOrder = analyticalOrderList[0];
                int maxOrder = analyticalOrderList[analyticalOrderList.Count - 1];
                for (int i = 0; i < eachBatch.Value.Count; i++) {
                    if (eachBatch.Value[i].AnalysisFilePropertyBean.AnalysisFileAnalyticalOrder < minOrder && eachBatch.Value[i].AnalysisFilePropertyBean.AnalysisFileType != AnalysisFileType.QC) {
                        MessageBox.Show("The start of analytical order should be QC sample for LOESS normalization. Please set them in the option setting window.\n Error: batch: "
                            + eachBatch.Key, "Error", MessageBoxButton.OK, MessageBoxImage.Error); return false;
                    }
                    else if (eachBatch.Value[i].AnalysisFilePropertyBean.AnalysisFileAnalyticalOrder > maxOrder && eachBatch.Value[i].AnalysisFilePropertyBean.AnalysisFileType != AnalysisFileType.QC) {
                        MessageBox.Show("The end of analytical order should be QC sample for LOESS normalization. Please set them in the option setting window.\n Error: batch: "
                            + eachBatch.Key, "Error", MessageBoxButton.OK, MessageBoxImage.Error); return false;
                    }
                }
            }
            return true;
        }

        

        public static void LowessNormalize(
            ObservableCollection<AnalysisFileBean> files,
            AlignmentResultBean alignmentResultBean, 
            double lowessSpan) {

            var batchDict = files.GroupBy(item => item.AnalysisFilePropertyBean.AnalysisBatch).ToDictionary(grp => grp.Key, grp => grp.ToList());

            var medQCs = new List<double>();
            var spots = alignmentResultBean.AlignmentPropertyBeanCollection;
            var globalSpots = DataAccessLcUtility.GetAlignmentPropertyBeanAndAlignedDriftSpotBeanMergedList(spots);

            foreach (var spot in globalSpots) {
                var qcs = new List<double>();
                var targetProps = DataAccessLcUtility.GetAlignedPeakPropertyBeanCollection(spot);
                foreach (var prop in targetProps) {
                    var fileProp = files[prop.FileID].AnalysisFilePropertyBean;
                    if (fileProp.AnalysisFileType == AnalysisFileType.QC && fileProp.AnalysisFileIncluded) {
                        qcs.Add(prop.Variable);
                    }
                }
                medQCs.Add(BasicMathematics.Median(qcs.ToArray()));
            }

            //var qcList = new List<double[]>();
            foreach (var eachBatch in batchDict) {
                var analysisFileBeanCollectionPerBatch = eachBatch.Value;
                var index = 0;
                foreach (var spot in globalSpots) {

                    if (spot.GetType() == typeof(AlignmentPropertyBean)) {
                        ((AlignmentPropertyBean)spot).IonAbundanceUnit = IonAbundanceUnit.NormalizedByQcPeakHeight;
                    }
                    else {
                        ((AlignedDriftSpotPropertyBean)spot).IonAbundanceUnit = IonAbundanceUnit.NormalizedByQcPeakHeight;
                    }

                    var qcList = new List<double[]>();
                    var variableProps = DataAccessLcUtility.GetAlignedPeakPropertyBeanCollection(spot);
                    foreach (var tFile in analysisFileBeanCollectionPerBatch) {
                        var tFileProp = tFile.AnalysisFilePropertyBean;
                        if (tFileProp.AnalysisFileType == AnalysisFileType.QC && tFileProp.AnalysisFileIncluded)
                            qcList.Add(new double[] { tFileProp.AnalysisFileAnalyticalOrder, variableProps[tFileProp.AnalysisFileId].NormalizedVariable });
                    }

                    qcList = qcList.OrderBy(n => n[0]).ToList();
                    if (qcList.Count == 0) {
                        foreach (var tFile in analysisFileBeanCollectionPerBatch) {
                            var tFileProp = tFile.AnalysisFilePropertyBean;
                            variableProps[tFileProp.AnalysisFileId].NormalizedVariable = variableProps[tFileProp.AnalysisFileId].NormalizedVariable;
                        }
                        index++;
                        continue;
                    }

                    double[] xQcArray = new double[qcList.Count];
                    double[] yQcArray = new double[qcList.Count];

                    for (int j = 0; j < qcList.Count; j++) { xQcArray[j] = qcList[j][0]; yQcArray[j] = qcList[j][1]; }

                    double[] yLoessPreArray = SmootherMathematics.Lowess(xQcArray, yQcArray, lowessSpan, 3);
                    double[] ySplineDeviArray = SmootherMathematics.Spline(xQcArray, yLoessPreArray, double.MaxValue, double.MaxValue);
                    double baseQcValue = yQcArray[0];
                    double fittedValue = 0;

                    foreach (var tFile in analysisFileBeanCollectionPerBatch) {
                        var tFileProp = tFile.AnalysisFilePropertyBean;
                        if (!tFileProp.AnalysisFileIncluded) continue;
                        fittedValue = SmootherMathematics.Splint(xQcArray, yLoessPreArray, ySplineDeviArray, tFileProp.AnalysisFileAnalyticalOrder);
                        fittedValue = medQCs[index] > 0 ? fittedValue / medQCs[index] : 1;
                        if (fittedValue <= 0) fittedValue = 1;
                        if (fittedValue > 0) {
                            variableProps[tFileProp.AnalysisFileId].NormalizedVariable = variableProps[tFileProp.AnalysisFileId].NormalizedVariable / (float)fittedValue;
                        }
                        else
                            variableProps[tFileProp.AnalysisFileId].NormalizedVariable = 0.0F;
                    }

                    index++;
                }
            }
        }

        public static void SplashNormalization(ObservableCollection<AnalysisFileBean> files, AlignmentResultBean alignmentResult, List<MspFormatCompoundInformationBean> mspDB,
            List<PostIdentificatioinReferenceBean> textDB, AnalysisParametersBean param, IonAbundanceUnit unit) {

            var spots = alignmentResult.AlignmentPropertyBeanCollection;
            var globalSpots = DataAccessLcUtility.GetAlignmentPropertyBeanAndAlignedDriftSpotBeanMergedList(spots);
            var splashLipids = param.StandardCompounds;
            var lipidClasses = LipidomicsConverter.GetLipidClasses();

            // initialize
            initializeNormalizationProcess(globalSpots);
            
            //normalization to 1 for IS spot
            foreach (var compound in splashLipids) { // first try to normalize IS peaks
                var baseSpot = globalSpots[compound.PeakID];
                if (baseSpot.GetType() == typeof(AlignmentPropertyBean)) {
                    var rSpot = (AlignmentPropertyBean)baseSpot;
                    rSpot.IonAbundanceUnit = unit;
                    rSpot.InternalStandardAlignmentID = compound.PeakID;
                    var baseProps = rSpot.AlignedPeakPropertyBeanCollection;
                    foreach (var prop in baseProps) prop.NormalizedVariable = (float)compound.Concentration;
                }
                else {
                    var dSpot = (AlignedDriftSpotPropertyBean)baseSpot;
                    dSpot.IonAbundanceUnit = unit;
                    dSpot.InternalStandardAlignmentID = compound.PeakID;
                    var baseProps = dSpot.AlignedPeakPropertyBeanCollection;
                    foreach (var prop in baseProps) prop.NormalizedVariable = (float)compound.Concentration;
                }
            }

            foreach (var compound in splashLipids.Where(n => n.TargetClass != "Any others")) { // first try to normalize except for "any others" property
                foreach (var spot in globalSpots) {
                    if (spot.GetType() != globalSpots[compound.PeakID].GetType()) continue;
                    var lipidclass = string.Empty;
                    var isOnDrift = false;
                    ObservableCollection<AlignedPeakPropertyBean> targetProps = null;
                    if (spot.GetType() == typeof(AlignmentPropertyBean)) {
                        var rSpot = (AlignmentPropertyBean)spot;
                        lipidclass = GetAnnotatedLipidClass(rSpot, mspDB, textDB, lipidClasses);
                        targetProps = rSpot.AlignedPeakPropertyBeanCollection;
                    }
                    else {
                        var dSpot = (AlignedDriftSpotPropertyBean)spot;
                        lipidclass = GetAnnotatedLipidClass(dSpot, mspDB, textDB, lipidClasses);
                        targetProps = dSpot.AlignedPeakPropertyBeanCollection;
                        isOnDrift = true;
                    }
                    if (targetProps[0].NormalizedVariable >= 0) continue;

                    //var lipidclass = GetAnnotatedLipidClass(spot, mspDB, lipidClasses);
                    if (compound.TargetClass == lipidclass) {
                        var baseSpot = globalSpots[compound.PeakID];
                        ObservableCollection<AlignedPeakPropertyBean> baseProps = null;
                        if (!isOnDrift) {
                            var rBaseSpot = (AlignmentPropertyBean)baseSpot;
                            baseProps = rBaseSpot.AlignedPeakPropertyBeanCollection;
                            ((AlignmentPropertyBean)spot).InternalStandardAlignmentID = compound.PeakID;
                            ((AlignmentPropertyBean)spot).IonAbundanceUnit = unit;
                        }
                        else {
                            var dBaseSpot = (AlignedDriftSpotPropertyBean)baseSpot;
                            baseProps = dBaseSpot.AlignedPeakPropertyBeanCollection;
                            ((AlignedDriftSpotPropertyBean)spot).InternalStandardAlignmentID = compound.PeakID;
                            ((AlignedDriftSpotPropertyBean)spot).IonAbundanceUnit = unit;
                        }

                        for (int i = 0; i < baseProps.Count; i++) {
                            //if (targetProps[i].NormalizedVariable >= 0) break;

                            var baseIntensity = baseProps[i].Variable > 0 ? baseProps[i].Variable : 1.0;
                            var targetIntensity = targetProps[i].Variable;
                            targetProps[i].NormalizedVariable = (float)(compound.Concentration *  targetIntensity / baseIntensity);
                        }

                        // spot.Comment += "; unit: " + unit + " by ID " + baseSpot.AlignmentID;
                    }
                    else {
                        continue;
                    }
                }
            }

            foreach (var compound in splashLipids.Where(n => n.TargetClass == "Any others")) { // second, normalized by any other tagged compounds
                foreach (var spot in globalSpots) {
                    if (spot.GetType() != globalSpots[compound.PeakID].GetType()) continue;
                    var lipidclass = string.Empty;
                    var isOnDrift = false;
                    ObservableCollection<AlignedPeakPropertyBean> targetProps = null;
                    if (spot.GetType() == typeof(AlignmentPropertyBean)) {
                        var rSpot = (AlignmentPropertyBean)spot;
                        lipidclass = GetAnnotatedLipidClass(rSpot, mspDB, textDB, lipidClasses);
                        targetProps = rSpot.AlignedPeakPropertyBeanCollection;
                    }
                    else {
                        var dSpot = (AlignedDriftSpotPropertyBean)spot;
                        lipidclass = GetAnnotatedLipidClass(dSpot, mspDB, textDB, lipidClasses);
                        targetProps = dSpot.AlignedPeakPropertyBeanCollection;
                        isOnDrift = true;
                    }
                    if (targetProps[0].NormalizedVariable >= 0) continue;

                    //var lipidclass = GetAnnotatedLipidClass(spot, mspDB, lipidClasses);
                    var baseSpot = globalSpots[compound.PeakID];
                    ObservableCollection<AlignedPeakPropertyBean> baseProps = null;
                    if (!isOnDrift) {
                        var rBaseSpot = (AlignmentPropertyBean)baseSpot;
                        baseProps = rBaseSpot.AlignedPeakPropertyBeanCollection;
                        ((AlignmentPropertyBean)spot).InternalStandardAlignmentID = compound.PeakID;
                        ((AlignmentPropertyBean)spot).IonAbundanceUnit = unit;
                    }
                    else {
                        var dBaseSpot = (AlignedDriftSpotPropertyBean)baseSpot;
                        baseProps = dBaseSpot.AlignedPeakPropertyBeanCollection;
                        ((AlignedDriftSpotPropertyBean)spot).InternalStandardAlignmentID = compound.PeakID;
                        ((AlignedDriftSpotPropertyBean)spot).IonAbundanceUnit = unit;
                    }

                    for (int i = 0; i < baseProps.Count; i++) {
                        //if (targetProps[i].NormalizedVariable >= 0) break;

                        var baseIntensity = baseProps[i].Variable > 0 ? baseProps[i].Variable : 1.0;
                        var targetIntensity = targetProps[i].Variable;
                        targetProps[i].NormalizedVariable = (float)(compound.Concentration * targetIntensity / baseIntensity);
                    }
                    //if (compound.TargetClass == lipidclass) {
                        

                    //    // spot.Comment += "; unit: " + unit + " by ID " + baseSpot.AlignmentID;
                    //}
                    //else {
                    //    continue;
                    //}
                }
            }

            // finalization
            foreach (var spot in globalSpots) {

                ObservableCollection<AlignedPeakPropertyBean> targetProps = null;
                if (spot.GetType() == typeof(AlignmentPropertyBean)) {
                    var rSpot = (AlignmentPropertyBean)spot;
                    targetProps = rSpot.AlignedPeakPropertyBeanCollection;
                    if (targetProps[0].NormalizedVariable >= 0) continue;
                    rSpot.IonAbundanceUnit = IonAbundanceUnit.Height;
                }
                else {
                    var dSpot = (AlignedDriftSpotPropertyBean)spot;
                    targetProps = dSpot.AlignedPeakPropertyBeanCollection;
                    if (targetProps[0].NormalizedVariable >= 0) continue;
                    dSpot.IonAbundanceUnit = IonAbundanceUnit.Height;
                }

                for (int i = 0; i < targetProps.Count; i++) {
                    targetProps[i].NormalizedVariable = targetProps[i].Variable;
                }
                //spot.Comment += "; not-normalized in SPLASH method";
            }
        }

        private static void initializeNormalizationProcess(List<object> globalSpots) {
            foreach (var spot in globalSpots) {
                if (spot.GetType() == typeof(AlignmentPropertyBean)) {
                    var rSpot = (AlignmentPropertyBean)spot;
                    rSpot.InternalStandardAlignmentID = -1;
                    initializeCollections(rSpot.AlignedPeakPropertyBeanCollection);
                }
                else {
                    var dSpot = (AlignedDriftSpotPropertyBean)spot;
                    dSpot.InternalStandardAlignmentID = -1;
                    initializeCollections(dSpot.AlignedPeakPropertyBeanCollection);
                }
            }
        }

        private static void initializeCollections(ObservableCollection<AlignedPeakPropertyBean> collection) {
            foreach (var prop in collection) {
                prop.NormalizedVariable = -1.0F;
            }
        }

        public static void SplashNormalization(ObservableCollection<AnalysisFileBean> files, AlignmentResultBean alignmentResult, List<MspFormatCompoundInformationBean> mspDB,
            AnalysisParametersBean param, string unit) { // unit: pmol, pg, ng

            var spots = alignmentResult.AlignmentPropertyBeanCollection;
            var splashLipids = param.StandardCompounds;
            var lipidClasses = LipidomicsConverter.GetLipidClasses();

            // initialize
            foreach (var spot in spots) {
                spot.InternalStandardAlignmentID = -1;
                foreach (var prop in spot.AlignedPeakPropertyBeanCollection) {
                    prop.NormalizedVariable = -1.0F;
                }
            }

            //normalization to 1 for IS spot
            foreach (var compound in splashLipids) { // first try to normalize IS peaks
                var baseSpot = spots[compound.PeakID];
                var baseProps = baseSpot.AlignedPeakPropertyBeanCollection;
                for (int i = 0; i < baseProps.Count; i++) {
                    var basePMol = compound.Concentration * compound.DilutionRate * files[i].AnalysisFilePropertyBean.InjectionVolume; // pmol
                    var targetPMol = basePMol;
                    if (unit == "fmol") targetPMol = targetPMol * 1000;
                    else if (unit == "ng") targetPMol = targetPMol * compound.MolecularWeight * 0.001;
                    else if (unit == "pg") targetPMol = targetPMol * compound.MolecularWeight;
                    baseProps[i].NormalizedVariable = (float)targetPMol;
                    if (i == 0)
                        baseSpot.IonAbundanceUnit = (IonAbundanceUnit)Enum.Parse(typeof(IonAbundanceUnit), unit);
                }
            }

            foreach (var compound in splashLipids.Where(n => n.TargetClass != "Any others")) { // first try to normalize except for "any others" property
                foreach (var spot in spots) {
                    var lipidclass = GetAnnotatedLipidClass(spot, mspDB, lipidClasses);
                    if (compound.TargetClass == lipidclass) {
                        var baseSpot = spots[compound.PeakID];
                        var baseProps = baseSpot.AlignedPeakPropertyBeanCollection;
                        var targetProps = spot.AlignedPeakPropertyBeanCollection;
                        spot.InternalStandardAlignmentID = compound.PeakID;
                        for (int i = 0; i < baseProps.Count; i++) {
                            if (targetProps[i].NormalizedVariable >= 0) break;

                            var baseIntensity = baseProps[i].Variable > 0 ? baseProps[i].Variable : 1.0;
                            var basePMol = compound.Concentration * compound.DilutionRate * files[i].AnalysisFilePropertyBean.InjectionVolume; // pmol
                            var targetPMol = targetProps[i].Variable / baseIntensity * basePMol;
                            if (unit == "fmol") targetPMol = targetPMol * 1000;
                            else if (unit == "ng") targetPMol = targetPMol * compound.MolecularWeight * 0.001;
                            else if (unit == "pg") targetPMol = targetPMol * compound.MolecularWeight;
                            targetProps[i].NormalizedVariable = (float)targetPMol;
                            if (i == 0)
                                spot.IonAbundanceUnit = (IonAbundanceUnit)Enum.Parse(typeof(IonAbundanceUnit), unit);

                        }

                        // spot.Comment += "; unit: " + unit + " by ID " + baseSpot.AlignmentID;
                    }
                    else {
                        continue;
                    }
                }
            }

            foreach (var compound in splashLipids.Where(n => n.TargetClass == "Any others")) { // second, normalized by any other tagged compounds
                foreach (var spot in spots) {
                    var baseSpot = spots[compound.PeakID];
                    var baseProps = baseSpot.AlignedPeakPropertyBeanCollection;
                    var targetProps = spot.AlignedPeakPropertyBeanCollection;
                    for (int i = 0; i < baseProps.Count; i++) {
                        if (targetProps[i].NormalizedVariable >= 0) break;

                        var baseIntensity = baseProps[i].Variable > 0 ? baseProps[i].Variable : 1.0;
                        var basePMol = compound.Concentration * compound.DilutionRate * files[i].AnalysisFilePropertyBean.InjectionVolume; // pmol
                        var targetPMol = targetProps[i].Variable / baseIntensity * basePMol;
                        if (unit == "fmol") targetPMol = targetPMol * 1000;
                        else if (unit == "ng") targetPMol = targetPMol * compound.MolecularWeight * 0.001;
                        else if (unit == "pg") targetPMol = targetPMol * compound.MolecularWeight;

                        targetProps[i].NormalizedVariable = (float)targetPMol;
                        if (i == 0)
                            spot.IonAbundanceUnit = (IonAbundanceUnit)Enum.Parse(typeof(IonAbundanceUnit), unit);
                    }
                    //spot.Comment += "; unit: " + unit + " by ID " + baseSpot.AlignmentID;
                }
            }

            // finalization
            foreach (var spot in spots) {
                var targetProps = spot.AlignedPeakPropertyBeanCollection;
                for (int i = 0; i < targetProps.Count; i++) {
                    if (targetProps[i].NormalizedVariable >= 0) break;
                    targetProps[i].NormalizedVariable = targetProps[i].Variable;
                    if (i == 0)
                        spot.IonAbundanceUnit = IonAbundanceUnit.Height;
                }
                //spot.Comment += "; not-normalized in SPLASH method";
            }
        }

        public static string GetAnnotatedLipidClass(AlignmentPropertyBean spot, List<MspFormatCompoundInformationBean> mspDB, List<string> lipidClasses) {
            var lipid = spot.MetaboliteName;
            if (lipid == null || lipid == string.Empty || lipid.Contains("w/o")) return "Unknown";

            var libraryID = spot.LibraryID;
            var lipidClass = string.Empty;
            if (mspDB != null && mspDB.Count > libraryID && libraryID >= 0) {
                lipidClass = mspDB[libraryID].CompoundClass;
                var lbmClass = LipidomicsConverter.ConvertMsdialClassDefinitionToLbmClassEnumVS2(lipidClass);
                lipidClass = LipidomicsConverter.ConvertLbmClassEnumToMsdialClassDefinitionVS2(lbmClass);
            }

            var lipidheader = lipid.Split(' ')[0];
            if (lipidClasses.Contains(lipidheader) || (lipidClasses.Contains(lipidClass) && !lipid.Contains("w/o"))) {
                if (!lipidClasses.Contains(lipidClass)) {
                    lipidClass = lipidheader;
                }
            }

            if (lipidClass == string.Empty) return "Unknown";
            else return lipidClass;
        }

        public static string GetAnnotatedLipidClass(AlignmentPropertyBean spot, List<MspFormatCompoundInformationBean> mspDB, 
            List<PostIdentificatioinReferenceBean> textDB, List<string> lipidClasses) {
            var lipid = spot.MetaboliteName;
            if (lipid == null || lipid == string.Empty || lipid.Contains("w/o")) return "Unknown";

            var libraryID = spot.LibraryID;
            var lipidClass = string.Empty;
            if (mspDB != null && mspDB.Count > libraryID && libraryID >= 0) {
                lipidClass = mspDB[libraryID].CompoundClass;
                var lbmClass = LipidomicsConverter.ConvertMsdialClassDefinitionToLbmClassEnumVS2(lipidClass);
                lipidClass = LipidomicsConverter.ConvertLbmClassEnumToMsdialClassDefinitionVS2(lbmClass);
            }

            var postIdentID = spot.PostIdentificationLibraryID;
            if (textDB != null && textDB.Count > postIdentID && postIdentID >= 0 && textDB[postIdentID].Ontology != null && textDB[postIdentID].Ontology != string.Empty) {
                var txtOntology = textDB[postIdentID].Ontology;
                if (lipidClasses.Contains(txtOntology)) lipidClass = txtOntology;
            }

            if (lipidClass == string.Empty) return "Unknown";
            else return lipidClass;
        }

        public static string GetAnnotatedLipidClass(AlignedDriftSpotPropertyBean spot, List<MspFormatCompoundInformationBean> mspDB,
            List<PostIdentificatioinReferenceBean> textDB, List<string> lipidClasses) {
            var lipid = spot.MetaboliteName;
            if (lipid == null || lipid == string.Empty || lipid.Contains("w/o")) return "Unknown";

            var libraryID = spot.LibraryID;
            var lipidClass = string.Empty;
            if (mspDB != null && mspDB.Count > libraryID && libraryID >= 0) {
                lipidClass = mspDB[libraryID].CompoundClass;
                var lbmClass = LipidomicsConverter.ConvertMsdialClassDefinitionToLbmClassEnumVS2(lipidClass);
                lipidClass = LipidomicsConverter.ConvertLbmClassEnumToMsdialClassDefinitionVS2(lbmClass);
            }

            var postIdentID = spot.PostIdentificationLibraryID;
            if (textDB != null && textDB.Count > postIdentID && postIdentID >= 0 && textDB[postIdentID].Ontology != null && textDB[postIdentID].Ontology != string.Empty) {
                var txtOntology = textDB[postIdentID].Ontology;
                if (lipidClasses.Contains(txtOntology)) lipidClass = txtOntology;
            }

            if (lipidClass == string.Empty) return "Unknown";
            else return lipidClass;
        }
    }
}
