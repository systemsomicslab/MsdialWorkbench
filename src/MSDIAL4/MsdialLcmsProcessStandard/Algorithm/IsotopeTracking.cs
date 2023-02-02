using Msdial.Lcms.Dataprocess.Scoring;
using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Msdial.Lcms.Dataprocess.Algorithm
{
    public sealed class IsotopeTracking
    {
        private IsotopeTracking() { }

        public static void SetIsotopeTrackingID(AlignmentResultBean alignmentResult, AnalysisParametersBean param, 
            ProjectPropertyBean projectProperty, List<MspFormatCompoundInformationBean> mspDB, Action<int> reportAction)
        {
            var alignmentSpots = new List<AlignmentPropertyBean>(alignmentResult.AlignmentPropertyBeanCollection);
            alignmentSpots = alignmentSpots.OrderBy(n => n.CentralAccurateMass).ToList();

            var rtTol = param.RetentionTimeAlignmentTolerance;
            var mzTol = param.Ms1AlignmentTolerance;
            var isotopeLabel = param.IsotopeTrackingDictionary;
            var labelMassDiff = isotopeLabel.IsotopeElements[isotopeLabel.SelectedID].MassDifference;
            alignmentSpotsInitialization(alignmentSpots, param, mspDB);

            //the remaining spots (i.e. for unidentified spots) are applied to the isotope tracking method
            for (int i = 0; i < alignmentSpots.Count; i++)
            {
                var targetSpot = alignmentSpots[i];

                //this is a criterion to determine what the non-labeled derived peak is.
                //when users set both non-labeled and fully-labeled reference files,
                //the intensity of non-labeled file should be detected while that of fully-labeled file not detected.
                if (param.SetFullyLabeledReferenceFile && param.FullyLabeledReferenceID >= 0) {

                    var nonLabelIntensity = targetSpot.AlignedPeakPropertyBeanCollection[param.NonLabeledReferenceID].Variable;
                    var labeledIntensity = targetSpot.AlignedPeakPropertyBeanCollection[param.FullyLabeledReferenceID].Variable;

                    var isLabeledDetected = 
                        targetSpot.AlignedPeakPropertyBeanCollection[param.FullyLabeledReferenceID].PeakID < 0 
                        ? false : true;

                    var isNonlabeledDetected =
                      targetSpot.AlignedPeakPropertyBeanCollection[param.NonLabeledReferenceID].PeakID < 0
                      ? false : true;

                    if (!(!isLabeledDetected && isNonlabeledDetected) && nonLabelIntensity < labeledIntensity * 4.0)
                        continue;
                }

                if (targetSpot.IsotopeTrackingParentID >= 0) continue;

                var spotMz = targetSpot.CentralAccurateMass;
                var spotRT = targetSpot.CentralRetentionTime;

                var startIndex = getStartIndexByRt(alignmentSpots, spotMz, mzTol);
                var candidateSpots = new List<AlignmentPropertyBean>();
                var maxLabelCount = getMaxLabelCount(targetSpot, isotopeLabel.IsotopeElements[isotopeLabel.SelectedID]);

                for (int j = startIndex; j < alignmentSpots.Count; j++)
                {
                    var candidateSpot = alignmentSpots[j];
                    if (candidateSpot.CentralAccurateMass > spotMz + maxLabelCount * labelMassDiff + mzTol)
                        break;

                    if (isExactCandidate(targetSpot, candidateSpot, rtTol, mzTol)) {
                        candidateSpots.Add(candidateSpot);
                    }
                }

                targetSpot.IsotopeTrackingWeightNumber = 0;
                targetSpot.IsotopeTrackingParentID = targetSpot.AlignmentID;

                findIsotopeLabeledSpots(targetSpot, candidateSpots, mzTol, isotopeLabel, maxLabelCount, param, mspDB);
                defineTrueBeginAndEndPointsInIsotopegroup(targetSpot, candidateSpots, mzTol, isotopeLabel, maxLabelCount, param);

                reportAction?.Invoke((int)(i / (alignmentSpots.Count - 1) * 100));
            }

            //finalization
            foreach (var spot in alignmentSpots.Where(n => n.IsotopeTrackingParentID < 0)) {
                spot.IsotopeTrackingWeightNumber = 0;
                spot.IsotopeTrackingParentID = spot.AlignmentID;
            }

            //PeakAlignment.PostAdductCurator(alignmentSpots, param, projectProperty, 0.025F);
            alignmentSpots = alignmentSpots.OrderBy(n => n.AlignmentID).ToList();
            alignmentResult.AlignmentPropertyBeanCollection = new ObservableCollection<AlignmentPropertyBean>(alignmentSpots);
        }

        private static void defineTrueBeginAndEndPointsInIsotopegroup(AlignmentPropertyBean targetSpot, List<AlignmentPropertyBean> candidateSpots, float mzTol, IsotopeTrackingDictionary isotopeLabel, int maxLabelCount, AnalysisParametersBean param) {

            // sometimes, for example in negative ion mode, when [M-H]- is a real monoisotopic ion,
            // [M-2H]- and [M-3H]- are also monitored especially when the monoisotopic ion is really high.

            // and sometimes, for example when we use 13C labeled sample, natural isotopic ions of other elements
            // such as 15N, 18O, 34S are well detected.

            // this function is made to exclude the above ions.
            // The begining ions such as [M-2H]- and [M-3H]- are represented as M-2 and M-1.
            // The ending ions are represented by 1000 order number.

            if (candidateSpots == null || candidateSpots.Count == 0) return;

            var parentID = targetSpot.AlignmentID;
            var groupedIons = new List<AlignmentPropertyBean>() { targetSpot };
            foreach (var spot in candidateSpots.Where(n => n.IsotopeTrackingParentID == parentID))
                groupedIons.Add(spot);

            if (groupedIons.Count < 3) return;

            groupedIons = groupedIons.OrderBy(n => n.IsotopeTrackingWeightNumber).ToList();

            var firstSpot = groupedIons[0];
            var nonlabelID = param.NonLabeledReferenceID;
            var labeledID = param.FullyLabeledReferenceID;

            var offset = 0;
            // first spot initialization. until 2 Da higher range, the intensity is checked.
            if (groupedIons.Count > 3) {
                if (groupedIons[1].IsotopeTrackingWeightNumber == 1 || groupedIons[1].IsotopeTrackingWeightNumber == 2) {
                    if (groupedIons[1].AlignedPeakPropertyBeanCollection[nonlabelID].Variable > firstSpot.AlignedPeakPropertyBeanCollection[nonlabelID].Variable * 5) {
                        firstSpot = groupedIons[1];
                        offset = firstSpot.IsotopeTrackingWeightNumber;
                    }
                }

                if (groupedIons[2].IsotopeTrackingWeightNumber == 1 || groupedIons[2].IsotopeTrackingWeightNumber == 2) {
                    if (groupedIons[2].AlignedPeakPropertyBeanCollection[nonlabelID].Variable > firstSpot.AlignedPeakPropertyBeanCollection[nonlabelID].Variable * 5) {
                        firstSpot = groupedIons[2];
                        offset = firstSpot.IsotopeTrackingWeightNumber;
                    }
                }
            }

            var lastSpot = groupedIons[groupedIons.Count - 1];
            var lastWeightNum = lastSpot.IsotopeTrackingWeightNumber;
            var lastWeightID = groupedIons.Count - 1;

            if (param.SetFullyLabeledReferenceFile) {

                if (lastSpot.IsotopeTrackingWeightNumber - firstSpot.IsotopeTrackingWeightNumber > 3) {

                    for (int i = groupedIons.Count - 2; i >= 0; i--) {
                        var tSpot = groupedIons[i];
                        var tSpotID = i;

                        if (lastWeightNum - tSpot.IsotopeTrackingWeightNumber > 2) break;

                        if (lastSpot.AlignedPeakPropertyBeanCollection[labeledID].Variable * 5 <
                            tSpot.AlignedPeakPropertyBeanCollection[labeledID].Variable) {
                            lastSpot = tSpot;
                            lastWeightNum = tSpot.IsotopeTrackingWeightNumber;
                            lastWeightID = tSpotID;
                        }
                    }
                }

                foreach (var tSpot in groupedIons) {

                    var weightNumber = tSpot.IsotopeTrackingWeightNumber - offset;
                    if (weightNumber > lastWeightNum)
                        tSpot.IsotopeTrackingWeightNumber = 1000 + weightNumber;
                    else
                        tSpot.IsotopeTrackingWeightNumber = weightNumber;
                }
            }
            else {
                foreach (var tSpot in groupedIons) {
                    var weightNumber = tSpot.IsotopeTrackingWeightNumber - offset;
                    tSpot.IsotopeTrackingWeightNumber = weightNumber;
                }
            }
        }

        private static void alignmentSpotsInitialization(List<AlignmentPropertyBean> alignmentSpots, 
            AnalysisParametersBean param,
            List<MspFormatCompoundInformationBean> mspDB)
        {
            var rtTol = param.RetentionTimeAlignmentTolerance * 2.0;
            var mzTol = param.Ms1AlignmentTolerance;
            var isotopeLabel = param.IsotopeTrackingDictionary;
            var labelMassDiff = isotopeLabel.IsotopeElements[isotopeLabel.SelectedID].MassDifference;

            //clear meta data from 'not reference' file.
            foreach (var spot in alignmentSpots) {
                if (spot.RepresentativeFileID != param.NonLabeledReferenceID) {
                    setDefaultCompoundInformation(spot);
                }
                else if ((spot.LibraryID >= 0 || spot.PostIdentificationLibraryID >= 0) &&
                    spot.RepresentativeFileID == param.NonLabeledReferenceID && !spot.MetaboliteName.Contains("w/o")) {
                    spot.IsotopeTrackingParentID = spot.AlignmentID;
                    spot.IsotopeTrackingWeightNumber = 0;
                    if (spot.LibraryID >= 0) {
                        var mspFormula = mspDB[spot.LibraryID].Formula;
                        if (mspFormula != null)
                            spot.Formula = FormulaStringParcer.OrganicElementsReader(mspFormula);
                    }
                }
            }

            //first, isotope tracking for identified spots is performed
            foreach (var spot in alignmentSpots) {
                if ((spot.LibraryID >= 0 || spot.PostIdentificationLibraryID >= 0) && 
                    spot.RepresentativeFileID == param.NonLabeledReferenceID && !spot.MetaboliteName.Contains("w/o")) {

                    var spotRT = spot.CentralRetentionTime;
                    var spotMz = spot.CentralAccurateMass;
                    var startIndex = getStartIndexByMz(alignmentSpots, spotMz, mzTol);
                    var candidateSpots = new List<AlignmentPropertyBean>() ;
                    var maxLabelCount = getMaxLabelCount(spot, isotopeLabel.IsotopeElements[isotopeLabel.SelectedID]);
                    var minLabelCount = getMinLabelCount(spot, isotopeLabel.IsotopeElements[isotopeLabel.SelectedID]);

                    for (int j = startIndex; j < alignmentSpots.Count; j++) {
                        var candidateSpot = alignmentSpots[j];
                        if (candidateSpot.CentralAccurateMass > spotMz + maxLabelCount * labelMassDiff + mzTol * 2.0)
                            break;
                        if (candidateSpot.CentralAccurateMass > spotMz + 10 * labelMassDiff - mzTol * 2.0 && 
                            candidateSpot.CentralAccurateMass < spotMz + minLabelCount * labelMassDiff - mzTol * 2.0)
                            continue;

                        if (isExactCandidate(spot, candidateSpot, rtTol, mzTol)) {
                            candidateSpots.Add(candidateSpot);
                        }
                    }

                    findIsotopeLabeledSpots(spot, candidateSpots, mzTol, isotopeLabel, maxLabelCount, param, mspDB);
                }
            }
        }

        private static int getMinLabelCount(AlignmentPropertyBean targetSpot, IsotopeElement isotopeElement) {
            if (targetSpot.Formula != null && targetSpot.Formula.Cnum > 0) {
                switch (isotopeElement.ElementName) {
                    case "13C": return targetSpot.Formula.Cnum - 5; //for practical reason 
                    //case "15N": return targetSpot.Formula.Nnum - 5;
                    case "15N": return 6;
                    //case "34S": return targetSpot.Formula.Snum - 5;
                    case "34S": return 4;
                    case "18O": return targetSpot.Formula.Onum - 5;
                    case "13C+15N": return targetSpot.Formula.Cnum + targetSpot.Formula.Nnum - 5;
                    case "13C+34S": return targetSpot.Formula.Cnum + targetSpot.Formula.Snum - 5;
                    case "15N+34S": return targetSpot.Formula.Nnum + targetSpot.Formula.Snum - 5;
                    case "13C+15N+34S": return targetSpot.Formula.Cnum + targetSpot.Formula.Nnum + targetSpot.Formula.Snum - 5;
                    case "Deuterium": return targetSpot.Formula.Hnum - 5;
                    default: return 0;
                }
            }
            else {
                return 0;
            }
        }

        private static bool isExactCandidate(AlignmentPropertyBean spot, AlignmentPropertyBean candidateSpot, 
            double rtTol, float mzTol) {

            var spotRT = spot.CentralRetentionTime;
            var spotMz = spot.CentralAccurateMass;

            if (candidateSpot.AlignmentID == spot.AlignmentID) return false;
            if (candidateSpot.CentralAccurateMass <= spot.CentralAccurateMass) return false;
            if (candidateSpot.CentralRetentionTime < spotRT - rtTol) return false;
            if (candidateSpot.IsotopeTrackingParentID >= 0) return false;
            if (candidateSpot.CentralRetentionTime > spotRT + rtTol) return false;

            return true;
        }

        private static void setDefaultCompoundInformation(AlignmentPropertyBean alignmentPropertyBean)
        {
            alignmentPropertyBean.LibraryID = -1;
            alignmentPropertyBean.PostIdentificationLibraryID = -1;
            alignmentPropertyBean.AdductIonName = string.Empty;
            alignmentPropertyBean.MetaboliteName = string.Empty;
            alignmentPropertyBean.TotalSimilairty = -1;
            alignmentPropertyBean.MassSpectraSimilarity = -1;
            alignmentPropertyBean.ReverseSimilarity = -1;
            alignmentPropertyBean.IsotopeSimilarity = -1;
            alignmentPropertyBean.RetentionTimeSimilarity = -1;
            alignmentPropertyBean.AccurateMassSimilarity = -1;
        }

        private static void findIsotopeLabeledSpots(AlignmentPropertyBean targetSpot, 
            List<AlignmentPropertyBean> candidateSpots, 
            float mzTol, 
            IsotopeTrackingDictionary isotopeLabel, 
            int maxLabelCount, 
            AnalysisParametersBean param,
            List<MspFormatCompoundInformationBean> mspDB)
        {
            var targetMz = targetSpot.CentralAccurateMass;
            var labelMassDiff = isotopeLabel.IsotopeElements[isotopeLabel.SelectedID].MassDifference;
            var isotopenumSpotidDictionary = new Dictionary<int, int>();
            var ppm = MolecularFormulaUtility.PpmCalculator(200.0, 200.0 + mzTol); //based on m/z 200
            var accuracy = MolecularFormulaUtility.ConvertPpmToMassAccuracy(targetMz, ppm);
            var remainderID = 0;
            var isIsotopePaternFinished = false;
            var trackingWeights = new List<int>() { 0 };

            #region old
            //charge number check at M + 1
            //var isNonlabelDoubleCharged = false;
            //var isotopicMassDoubleCharged = targetMz + (float)labelMassDiff * 0.50;
            //for (int j = 0; j < candidateSpots.Count; j++) {
            //    var cSpot = candidateSpots[j];

            //    if (isotopicMassDoubleCharged - accuracy < cSpot.CentralAccurateMass &&
            //        cSpot.CentralAccurateMass < isotopicMassDoubleCharged + accuracy) {
            //        isNonlabelDoubleCharged = true;
            //        break;
            //    }

            //    if (cSpot.CentralAccurateMass >= isotopicMassDoubleCharged + accuracy) {
            //        break;
            //    }
            //}
            #endregion

            //charge number check at M + 1
            var predChargeNumber = 1;
            var c13_c12Diff = MassDiffDictionary.C13_C12;  //1.003355F;
            for (int j = 0; j < candidateSpots.Count; j++) {
                var isotopePeak = candidateSpots[j];
                if (isotopePeak.CentralAccurateMass > targetMz + c13_c12Diff + accuracy) break;

                for (int k = param.MaxChargeNumber; k >= 1; k--) {
                    var predIsotopeMass = (double)targetMz + (double)c13_c12Diff / (double)k;
                    var diff = Math.Abs(predIsotopeMass - isotopePeak.CentralAccurateMass);
                    if (diff < accuracy) {
                        predChargeNumber = k;
                        if (k <= 3) {
                            break;
                        }
                        else if (k == 4 || k == 5) {
                            var predNextIsotopeMass = (double)targetMz + (double)c13_c12Diff / (double)(k - 1);
                            var nextDiff = Math.Abs(predNextIsotopeMass - isotopePeak.CentralAccurateMass);
                            if (diff > nextDiff) predChargeNumber = k - 1;
                            break;
                        }
                        else if (k >= 6) {
                            var predNextIsotopeMass = (double)targetMz + (double)c13_c12Diff / (double)(k - 1);
                            var nextDiff = Math.Abs(predNextIsotopeMass - isotopePeak.CentralAccurateMass);
                            if (diff > nextDiff) {
                                predChargeNumber = k - 1;
                                diff = nextDiff;

                                predNextIsotopeMass = (double)targetMz + (double)c13_c12Diff / (double)(k - 2);
                                nextDiff = Math.Abs(predNextIsotopeMass - isotopePeak.CentralAccurateMass);

                                if (diff > nextDiff) {
                                    predChargeNumber = k - 2;
                                    diff = nextDiff;
                                }
                            }
                            break;
                        }
                    }
                }
                if (predChargeNumber != 1) break;
            }

            //var chargeCoff = isNonlabelDoubleCharged == true ? 0.50 : 1.0;
            var chargeCoff = 1.0 / (double)predChargeNumber;
            maxLabelCount *= predChargeNumber;

            var lastSpotLabel = 0;
            var beyond10 = false;
            var isSetFullyLabeledReferenceFile = param.SetFullyLabeledReferenceFile;
            var isFullLabeledFrag = false;

            //if (Math.Abs(targetMz - 90.0549) < 0.005 && Math.Abs(targetSpot.CentralRetentionTime - 7.9605) < 0.1) {
            //    Console.WriteLine();
            //}

            for (int i = 1; i <= maxLabelCount; i++) {

                var isotopicMass = targetMz + (float)i * labelMassDiff * chargeCoff;
                var isotopicCandidates = new List<AlignmentPropertyBean>();

                for (int j = remainderID; j < candidateSpots.Count; j++) {
                    var cSpot = candidateSpots[j];
                    if (isotopicMass - accuracy < cSpot.CentralAccurateMass &&
                    cSpot.CentralAccurateMass < isotopicMass + accuracy) {
                        isotopicCandidates.Add(cSpot);
                    }

                    if (cSpot.CentralAccurateMass >= isotopicMass + accuracy) {
                        remainderID = j;
                        break;
                    }
                }

                if (isotopicCandidates.Count > 0) {
                    var minDiff = double.MaxValue;
                    var minID = -1;
                    for (int j = 0; j < isotopicCandidates.Count; j++) {
                        var isotopicRt = isotopicCandidates[j].CentralRetentionTime;
                        var rtDiff = Math.Abs(targetSpot.CentralRetentionTime - isotopicRt);
                        if (rtDiff < minDiff) {
                            minID = j;
                            minDiff = rtDiff;
                        }
                    }

                    trackingWeights.Add(i);
                    if (i - trackingWeights[trackingWeights.Count - 2] > 3) {
                        isIsotopePaternFinished = true;
                    }

                    var isotopicCandidate = isotopicCandidates[minID];
                    var nonLabelDetected 
                        = isotopicCandidate.AlignedPeakPropertyBeanCollection[param.NonLabeledReferenceID].PeakID < 0
                          ? false : true;
                    var nonLabelIntensity = isotopicCandidate.AlignedPeakPropertyBeanCollection[param.NonLabeledReferenceID].Variable;


                    if (isSetFullyLabeledReferenceFile) {
                        var nonlabelID = param.NonLabeledReferenceID;
                        var labeledID = param.FullyLabeledReferenceID;

                        var isLabeledDetected =
                              isotopicCandidate.AlignedPeakPropertyBeanCollection[labeledID].PeakID < 0
                              ? false : true;

                        
                        var labeledIntensity = isotopicCandidate.AlignedPeakPropertyBeanCollection[labeledID].Variable;

                        if (isFullLabeledFrag && ((!isLabeledDetected && nonLabelDetected) || labeledIntensity < nonLabelIntensity * 4.0)) {
                            break;
                        }

                        if ((isLabeledDetected && !nonLabelDetected) || labeledIntensity > nonLabelIntensity * 4.0) {
                            isFullLabeledFrag = true;
                        }
                    }

                    if (isIsotopePaternFinished && nonLabelDetected) continue;
                    if (i > 10 && i - lastSpotLabel > 3 && beyond10) break;

                    isotopicCandidates[minID].IsotopeTrackingWeightNumber = i;
                    isotopicCandidates[minID].IsotopeTrackingParentID = targetSpot.AlignmentID;
                    lastSpotLabel = i;

                    if (i > 10) beyond10 = true;
                }
            }
        }

        private static int getMaxLabelCount(AlignmentPropertyBean targetSpot, IsotopeElement isotopeElement)
        {
            if (targetSpot.Formula != null && targetSpot.Formula.Cnum > 0)
            {
                switch (isotopeElement.ElementName)
                {
                    case "13C": return targetSpot.Formula.Cnum + 5; //for practical reason 
                    //case "15N": return targetSpot.Formula.Nnum + 5;
                    case "15N": return 6;
                    //case "34S": return targetSpot.Formula.Snum + 5;
                    case "34S": return 4;
                    case "18O": return targetSpot.Formula.Onum + 5;
                    case "Deuterium": return targetSpot.Formula.Hnum + 5;
                    case "13C+15N": return targetSpot.Formula.Cnum + targetSpot.Formula.Nnum + 5;
                    case "13C+34S": return targetSpot.Formula.Cnum + targetSpot.Formula.Snum + 5;
                    case "15N+34S": return targetSpot.Formula.Nnum + targetSpot.Formula.Snum + 5;
                    case "13C+15N+34S": return targetSpot.Formula.Cnum + targetSpot.Formula.Nnum + targetSpot.Formula.Snum + 5;
                    default: return 15;
                }
            }
            else
            {
                var formula = GetConsiderableFormulaFromAccurateMass(targetSpot.CentralAccurateMass);
                switch (isotopeElement.ElementName)
                {
                    case "13C": return formula.Cnum;
                    //case "15N": return formula.Nnum;
                    case "15N": return 6;
                    //case "34S": return formula.Snum;
                    case "34S": return 4;
                    case "18O": return formula.Onum;
                    case "Deuterium": return formula.Hnum;
                    case "13C+15N": return targetSpot.Formula.Cnum + targetSpot.Formula.Nnum;
                    case "13C+34S": return targetSpot.Formula.Cnum + targetSpot.Formula.Snum;
                    case "15N+34S": return targetSpot.Formula.Nnum + targetSpot.Formula.Snum;
                    case "13C+15N+34S": return targetSpot.Formula.Cnum + targetSpot.Formula.Nnum + targetSpot.Formula.Snum;
                    default: return 15;
                }
            }
        }

        public static Formula GetConsiderableFormulaFromAccurateMass(double mass)
        {
            int cNumber = (int)(mass / 12);
            int hNumber = (int)(cNumber * 3.3);
            int nNumber = (int)(cNumber * 1.2);
            int oNumber = (int)(cNumber * 2.2);
            int pNumber = (int)(cNumber * 0.4);
            int sNumber = (int)(cNumber * 1.0);

            return new Formula(cNumber, hNumber, nNumber, oNumber, pNumber, sNumber, 0, 0, 0, 0, 0);
        }

        private static int getStartIndexByRt(List<AlignmentPropertyBean> alignmentSpots, float spotRT, float rtTol)
        {
            float targetRT = spotRT - rtTol;
            int startIndex = 0, endIndex = alignmentSpots.Count - 1;
            int counter = 0;
            while (counter < 10)
            {
                if (alignmentSpots[startIndex].CentralRetentionTime <= targetRT && 
                    targetRT < alignmentSpots[(startIndex + endIndex) / 2].CentralRetentionTime)
                {
                    endIndex = (startIndex + endIndex) / 2;
                }
                else if (alignmentSpots[(startIndex + endIndex) / 2].CentralRetentionTime <= targetRT && 
                    targetRT < alignmentSpots[endIndex].CentralRetentionTime)
                {
                    startIndex = (startIndex + endIndex) / 2;
                }
                counter++;
            }
            return startIndex;
        }

        private static int getStartIndexByMz(List<AlignmentPropertyBean> alignmentSpots, float spotMz, float mzTol)
        {
            float targetMz = spotMz - mzTol;
            int startIndex = 0, endIndex = alignmentSpots.Count - 1;
            int counter = 0;
            while (counter < 10) {
                if (alignmentSpots[startIndex].CentralAccurateMass <= targetMz && 
                    targetMz < alignmentSpots[(startIndex + endIndex) / 2].CentralAccurateMass) {
                    endIndex = (startIndex + endIndex) / 2;
                }
                else if (alignmentSpots[(startIndex + endIndex) / 2].CentralAccurateMass <= targetMz && 
                    targetMz < alignmentSpots[endIndex].CentralAccurateMass) {
                    startIndex = (startIndex + endIndex) / 2;
                }
                counter++;
            }
            return startIndex;
        }

        public static void SetTargetFormulaInformation(AlignmentResultBean alignmentResult, List<PostIdentificatioinReferenceBean> targetFormulas, AnalysisParametersBean param)
        {
            var alignmentSpots = new List<AlignmentPropertyBean>(alignmentResult.AlignmentPropertyBeanCollection);
            alignmentSpots = alignmentSpots.OrderBy(n => n.CentralAccurateMass).ToList();

            targetFormulas = targetFormulas.OrderBy(n => n.AccurateMass).ToList();

            foreach (var query in targetFormulas)
            {
                var startIndex = getStartIndexByMass(alignmentSpots, query.AccurateMass, param.AccurateMassToleranceOfPostIdentification);
                var maxID = -1; var maxScore = double.MinValue;
                for (int i = startIndex; i < alignmentSpots.Count; i++)
                {
                    var spotMass = alignmentSpots[i].CentralAccurateMass;
                    if (spotMass < query.AccurateMass - param.Ms1LibrarySearchTolerance) continue;
                    if (spotMass > query.AccurateMass + param.Ms1LibrarySearchTolerance) break;

                    var similarity = getTargetFormulaSimilarity(query, alignmentSpots[i], param);
                    if (maxScore < similarity)
                    {
                        maxScore = similarity;
                        maxID = i;
                    }
                }

                if (maxID >= 0 && maxScore > param.PostIdentificationScoreCutOff)
                {
                    setToAlignmentProperty(alignmentSpots[maxID], query);
                }
            }

            alignmentSpots = alignmentSpots.OrderBy(n => n.AlignmentID).ToList();
            alignmentResult.AlignmentPropertyBeanCollection = new ObservableCollection<AlignmentPropertyBean>(alignmentSpots);
        }

        private static void setToAlignmentProperty(AlignmentPropertyBean spot, PostIdentificatioinReferenceBean query)
        {
            spot.TargetFormulaLibraryID = query.ReferenceID;
            spot.Formula = query.Formula;
            spot.Adduct = query.AdductIon;
        }

        private static double getTargetFormulaSimilarity(PostIdentificatioinReferenceBean query, AlignmentPropertyBean alignedSpot, AnalysisParametersBean param)
        {
            var similarity = 0.0;
            var massSimilarity = LcmsScoring.GetGaussianSimilarity(alignedSpot.CentralAccurateMass, query.AccurateMass, param.AccurateMassToleranceOfPostIdentification);
            var rtSimilarity = -1.0;
            if (query.RetentionTime >= 0) rtSimilarity = LcmsScoring.GetGaussianSimilarity(alignedSpot.CentralRetentionTime, query.RetentionTime, param.RetentionTimeToleranceOfPostIdentification);

            if (rtSimilarity < 0)
                similarity = massSimilarity;
            else
                similarity = (massSimilarity + rtSimilarity) * 0.5;
            return similarity;
        }


        private static int getStartIndexByMass(List<AlignmentPropertyBean> alignmentSpots, float spotMass, float massTol)
        {
            float targetMass = spotMass - massTol;
            int startIndex = 0, endIndex = alignmentSpots.Count - 1;
            int counter = 0;
            while (counter < 10)
            {
                if (alignmentSpots[startIndex].CentralAccurateMass <= targetMass && targetMass < alignmentSpots[(startIndex + endIndex) / 2].CentralAccurateMass)
                {
                    endIndex = (startIndex + endIndex) / 2;
                }
                else if (alignmentSpots[(startIndex + endIndex) / 2].CentralAccurateMass <= targetMass && targetMass < alignmentSpots[endIndex].CentralAccurateMass)
                {
                    startIndex = (startIndex + endIndex) / 2;
                }
                counter++;
            }
            return startIndex;
        }
    }
}
