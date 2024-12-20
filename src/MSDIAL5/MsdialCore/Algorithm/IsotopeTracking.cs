using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm
{
    static class IsotopeTracking
    {
        public static void SetIsotopeTrackingID(AlignmentResultContainer alignmentResult, ParameterBase param, 
            List<MoleculeMsReference> mspDB, Action<int> reportAction)
        {
            var alignmentSpots = new List<AlignmentSpotProperty>(alignmentResult.AlignmentSpotProperties);
            alignmentSpots = alignmentSpots.OrderBy(spot => spot.MassCenter).ToList();

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

                    var nonLabelIntensity = targetSpot.AlignedPeakProperties[param.NonLabeledReferenceID].PeakHeightTop;
                    var labeledIntensity = targetSpot.AlignedPeakProperties[param.FullyLabeledReferenceID].PeakHeightTop;

                    var isLabeledDetected = targetSpot.AlignedPeakProperties[param.FullyLabeledReferenceID].PeakID >= 0;

                    var isNonlabeledDetected = targetSpot.AlignedPeakProperties[param.NonLabeledReferenceID].PeakID >= 0;

                    if (!(!isLabeledDetected && isNonlabeledDetected) && nonLabelIntensity < labeledIntensity * 4.0)
                        continue;
                }

                if (targetSpot.PeakCharacter.IsotopeParentPeakID >= 0) continue;

                var spotMz = targetSpot.MassCenter;
                var spotRT = targetSpot.TimesCenter.Value;

                var startIndex = getStartIndexByRt(alignmentSpots, (float)spotMz, mzTol);
                var candidateSpots = new List<AlignmentSpotProperty>();
                var maxLabelCount = getMaxLabelCount(targetSpot, isotopeLabel.IsotopeElements[isotopeLabel.SelectedID]);

                for (int j = startIndex; j < alignmentSpots.Count; j++)
                {
                    var candidateSpot = alignmentSpots[j];
                    if (candidateSpot.MassCenter > spotMz + maxLabelCount * labelMassDiff + mzTol)
                        break;

                    if (isExactCandidate(targetSpot, candidateSpot, rtTol, mzTol)) {
                        candidateSpots.Add(candidateSpot);
                    }
                }

                targetSpot.PeakCharacter.IsotopeWeightNumber = 0;
                targetSpot.PeakCharacter.IsotopeParentPeakID = targetSpot.AlignmentID;

                findIsotopeLabeledSpots(targetSpot, candidateSpots, mzTol, isotopeLabel, maxLabelCount, param, mspDB);
                defineTrueBeginAndEndPointsInIsotopegroup(targetSpot, candidateSpots, mzTol, isotopeLabel, maxLabelCount, param);

                reportAction?.Invoke((int)(i / (alignmentSpots.Count - 1) * 100));
            }

            //finalization
            foreach (var spot in alignmentSpots.Where(spot => spot.PeakCharacter.IsotopeParentPeakID < 0)) {
                spot.PeakCharacter.IsotopeWeightNumber = 0;
                spot.PeakCharacter.IsotopeParentPeakID = spot.AlignmentID;
            }

            alignmentSpots = alignmentSpots.OrderBy(spot => spot.AlignmentID).ToList();
            alignmentResult.AlignmentSpotProperties = new ObservableCollection<AlignmentSpotProperty>(alignmentSpots);
        }

        private static void defineTrueBeginAndEndPointsInIsotopegroup(
            AlignmentSpotProperty targetSpot, List<AlignmentSpotProperty> candidateSpots,
            float mzTol, IsotopeTrackingDictionary isotopeLabel, int maxLabelCount, ParameterBase param) {

            // sometimes, for example in negative ion mode, when [M-H]- is a real monoisotopic ion,
            // [M-2H]- and [M-3H]- are also monitored especially when the monoisotopic ion is really high.

            // and sometimes, for example when we use 13C labeled sample, natural isotopic ions of other elements
            // such as 15N, 18O, 34S are well detected.

            // this function is made to exclude the above ions.
            // The begining ions such as [M-2H]- and [M-3H]- are represented as M-2 and M-1.
            // The ending ions are represented by 1000 order number.

            if (candidateSpots == null || candidateSpots.Count == 0) return;

            var parentID = targetSpot.AlignmentID;
            var groupedIons = new List<AlignmentSpotProperty>() { targetSpot };
            foreach (var spot in candidateSpots.Where(spot => spot.PeakCharacter.IsotopeParentPeakID == parentID))
                groupedIons.Add(spot);

            if (groupedIons.Count < 3) return;

            groupedIons = groupedIons.OrderBy(spot => spot.PeakCharacter.IsotopeWeightNumber).ToList();

            var firstSpot = groupedIons[0];
            var nonlabelID = param.NonLabeledReferenceID;
            var labeledID = param.FullyLabeledReferenceID;

            var offset = 0;
            // first spot initialization. until 2 Da higher range, the intensity is checked.
            if (groupedIons.Count > 3) {
                if (groupedIons[1].PeakCharacter.IsotopeWeightNumber == 1 || groupedIons[1].PeakCharacter.IsotopeWeightNumber == 2) {
                    if (groupedIons[1].AlignedPeakProperties[nonlabelID].PeakHeightTop > firstSpot.AlignedPeakProperties[nonlabelID].PeakHeightTop * 5) {
                        firstSpot = groupedIons[1];
                        offset = firstSpot.PeakCharacter.IsotopeWeightNumber;
                    }
                }

                if (groupedIons[2].PeakCharacter.IsotopeWeightNumber == 1 || groupedIons[2].PeakCharacter.IsotopeWeightNumber == 2) {
                    if (groupedIons[2].AlignedPeakProperties[nonlabelID].PeakHeightTop > firstSpot.AlignedPeakProperties[nonlabelID].PeakHeightTop * 5) {
                        firstSpot = groupedIons[2];
                        offset = firstSpot.PeakCharacter.IsotopeWeightNumber;
                    }
                }
            }

            var lastSpot = groupedIons[groupedIons.Count - 1];
            var lastWeightNum = lastSpot.PeakCharacter.IsotopeWeightNumber;
            var lastWeightID = groupedIons.Count - 1;

            if (param.SetFullyLabeledReferenceFile) {

                if (lastSpot.PeakCharacter.IsotopeWeightNumber - firstSpot.PeakCharacter.IsotopeWeightNumber > 3) {

                    for (int i = groupedIons.Count - 2; i >= 0; i--) {
                        var tSpot = groupedIons[i];
                        var tSpotID = i;

                        if (lastWeightNum - tSpot.PeakCharacter.IsotopeWeightNumber > 2) break;

                        if (lastSpot.AlignedPeakProperties[labeledID].PeakHeightTop * 5 <
                            tSpot.AlignedPeakProperties[labeledID].PeakHeightTop) {
                            lastSpot = tSpot;
                            lastWeightNum = tSpot.PeakCharacter.IsotopeWeightNumber;
                            lastWeightID = tSpotID;
                        }
                    }
                }

                foreach (var tSpot in groupedIons) {

                    var weightNumber = tSpot.PeakCharacter.IsotopeWeightNumber - offset;
                    if (weightNumber > lastWeightNum)
                        tSpot.PeakCharacter.IsotopeWeightNumber = 1000 + weightNumber;
                    else
                        tSpot.PeakCharacter.IsotopeWeightNumber = weightNumber;
                }
            }
            else {
                foreach (var tSpot in groupedIons) {
                    var weightNumber = tSpot.PeakCharacter.IsotopeWeightNumber - offset;
                    tSpot.PeakCharacter.IsotopeWeightNumber = weightNumber;
                }
            }
        }

        private static void alignmentSpotsInitialization(List<AlignmentSpotProperty> alignmentSpots, 
            ParameterBase param, List<MoleculeMsReference> mspDB)
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
                else if ((spot.MspID >= 0 || spot.TextDbID >= 0) &&
                    spot.RepresentativeFileID == param.NonLabeledReferenceID && !spot.Name.Contains("w/o")) {
                    spot.PeakCharacter.IsotopeParentPeakID = spot.AlignmentID;
                    spot.PeakCharacter.IsotopeWeightNumber = 0;
                    if (spot.MspID >= 0) {
                        spot.Formula = mspDB[spot.MspID].Formula;
                    }
                }
            }

            //first, isotope tracking for identified spots is performed
            foreach (var spot in alignmentSpots) {
                if ((spot.MspID >= 0 || spot.TextDbID >= 0) && 
                    spot.RepresentativeFileID == param.NonLabeledReferenceID && !spot.Name.Contains("w/o")) {

                    var spotRT = spot.TimesCenter.Value;
                    var spotMz = spot.MassCenter;
                    var startIndex = getStartIndexByMz(alignmentSpots, (float)spotMz, mzTol);
                    var candidateSpots = new List<AlignmentSpotProperty>() ;
                    var maxLabelCount = getMaxLabelCount(spot, isotopeLabel.IsotopeElements[isotopeLabel.SelectedID]);
                    var minLabelCount = getMinLabelCount(spot, isotopeLabel.IsotopeElements[isotopeLabel.SelectedID]);

                    for (int j = startIndex; j < alignmentSpots.Count; j++) {
                        var candidateSpot = alignmentSpots[j];
                        if (candidateSpot.MassCenter > spotMz + maxLabelCount * labelMassDiff + mzTol * 2.0)
                            break;
                        if (candidateSpot.MassCenter > spotMz + 10 * labelMassDiff - mzTol * 2.0 && 
                            candidateSpot.MassCenter < spotMz + minLabelCount * labelMassDiff - mzTol * 2.0)
                            continue;

                        if (isExactCandidate(spot, candidateSpot, rtTol, mzTol)) {
                            candidateSpots.Add(candidateSpot);
                        }
                    }

                    findIsotopeLabeledSpots(spot, candidateSpots, mzTol, isotopeLabel, maxLabelCount, param, mspDB);
                }
            }
        }

        private static int getMinLabelCount(AlignmentSpotProperty targetSpot, IsotopeElement isotopeElement) {
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

        private static bool isExactCandidate(AlignmentSpotProperty spot, AlignmentSpotProperty candidateSpot, 
            double rtTol, float mzTol) {

            var spotRT = spot.TimesCenter.Value;
            var spotMz = spot.MassCenter;

            if (candidateSpot.AlignmentID == spot.AlignmentID) return false;
            if (candidateSpot.MassCenter <= spot.MassCenter) return false;
            if (candidateSpot.TimesCenter.Value < spotRT - rtTol) return false;
            if (candidateSpot.PeakCharacter.IsotopeParentPeakID >= 0) return false;
            if (candidateSpot.TimesCenter.Value > spotRT + rtTol) return false;

            return true;
        }

        private static void setDefaultCompoundInformation(AlignmentSpotProperty alignmentSpot)
        {
            alignmentSpot.MSRawID2MspBasedMatchResult = new Dictionary<int, MsScanMatchResult>();
            alignmentSpot.TextDbBasedMatchResult = null;
            alignmentSpot.MatchResults.ClearResults();
            alignmentSpot.MatchResults.ClearMspResults();
            alignmentSpot.MatchResults.ClearTextDbResults();
            alignmentSpot.SetAdductType(AdductIon.Default);
            alignmentSpot.Name = string.Empty;
        }

        private static void findIsotopeLabeledSpots(AlignmentSpotProperty targetSpot, 
            List<AlignmentSpotProperty> candidateSpots, 
            float mzTol, 
            IsotopeTrackingDictionary isotopeLabel, 
            int maxLabelCount, 
            ParameterBase param,
            List<MoleculeMsReference> mspDB)
        {
            var targetMz = targetSpot.MassCenter;
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

            //    if (isotopicMassDoubleCharged - accuracy < cSpot.MassCenter &&
            //        cSpot.MassCenter < isotopicMassDoubleCharged + accuracy) {
            //        isNonlabelDoubleCharged = true;
            //        break;
            //    }

            //    if (cSpot.MassCenter >= isotopicMassDoubleCharged + accuracy) {
            //        break;
            //    }
            //}
            #endregion

            //charge number check at M + 1
            var predChargeNumber = 1;
            var c13_c12Diff = MassDiffDictionary.C13_C12;  //1.003355F;
            for (int j = 0; j < candidateSpots.Count; j++) {
                var isotopePeak = candidateSpots[j];
                if (isotopePeak.MassCenter > targetMz + c13_c12Diff + accuracy) break;

                for (int k = param.MaxChargeNumber; k >= 1; k--) {
                    var predIsotopeMass = (double)targetMz + (double)c13_c12Diff / (double)k;
                    var diff = Math.Abs(predIsotopeMass - isotopePeak.MassCenter);
                    if (diff < accuracy) {
                        predChargeNumber = k;
                        if (k <= 3) {
                            break;
                        }
                        else if (k == 4 || k == 5) {
                            var predNextIsotopeMass = (double)targetMz + (double)c13_c12Diff / (double)(k - 1);
                            var nextDiff = Math.Abs(predNextIsotopeMass - isotopePeak.MassCenter);
                            if (diff > nextDiff) predChargeNumber = k - 1;
                            break;
                        }
                        else if (k >= 6) {
                            var predNextIsotopeMass = (double)targetMz + (double)c13_c12Diff / (double)(k - 1);
                            var nextDiff = Math.Abs(predNextIsotopeMass - isotopePeak.MassCenter);
                            if (diff > nextDiff) {
                                predChargeNumber = k - 1;
                                diff = nextDiff;

                                predNextIsotopeMass = (double)targetMz + (double)c13_c12Diff / (double)(k - 2);
                                nextDiff = Math.Abs(predNextIsotopeMass - isotopePeak.MassCenter);

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

            //if (Math.Abs(targetMz - 90.0549) < 0.005 && Math.Abs(targetSpot.TimesCenter.Value - 7.9605) < 0.1) {
            //    Console.WriteLine();
            //}

            for (int i = 1; i <= maxLabelCount; i++) {

                var isotopicMass = targetMz + (float)i * labelMassDiff * chargeCoff;
                var isotopicCandidates = new List<AlignmentSpotProperty>();

                for (int j = remainderID; j < candidateSpots.Count; j++) {
                    var cSpot = candidateSpots[j];
                    if (isotopicMass - accuracy < cSpot.MassCenter &&
                    cSpot.MassCenter < isotopicMass + accuracy) {
                        isotopicCandidates.Add(cSpot);
                    }

                    if (cSpot.MassCenter >= isotopicMass + accuracy) {
                        remainderID = j;
                        break;
                    }
                }

                if (isotopicCandidates.Count > 0) {
                    var minDiff = double.MaxValue;
                    var minID = -1;
                    for (int j = 0; j < isotopicCandidates.Count; j++) {
                        var isotopicRt = isotopicCandidates[j].TimesCenter.Value;
                        var rtDiff = Math.Abs(targetSpot.TimesCenter.Value - isotopicRt);
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
                    var nonLabelDetected = isotopicCandidate.AlignedPeakProperties[param.NonLabeledReferenceID].PeakID >= 0;
                    var nonLabelIntensity = isotopicCandidate.AlignedPeakProperties[param.NonLabeledReferenceID].PeakHeightTop;


                    if (isSetFullyLabeledReferenceFile) {
                        var nonlabelID = param.NonLabeledReferenceID;
                        var labeledID = param.FullyLabeledReferenceID;

                        var isLabeledDetected = isotopicCandidate.AlignedPeakProperties[labeledID].PeakID >= 0;

                        
                        var labeledIntensity = isotopicCandidate.AlignedPeakProperties[labeledID].PeakHeightTop;

                        if (isFullLabeledFrag && ((!isLabeledDetected && nonLabelDetected) || labeledIntensity < nonLabelIntensity * 4.0)) {
                            break;
                        }

                        if ((isLabeledDetected && !nonLabelDetected) || labeledIntensity > nonLabelIntensity * 4.0) {
                            isFullLabeledFrag = true;
                        }
                    }

                    if (isIsotopePaternFinished && nonLabelDetected) continue;
                    if (i > 10 && i - lastSpotLabel > 3 && beyond10) break;

                    isotopicCandidates[minID].PeakCharacter.IsotopeWeightNumber = i;
                    isotopicCandidates[minID].PeakCharacter.IsotopeParentPeakID = targetSpot.AlignmentID;
                    lastSpotLabel = i;

                    if (i > 10) beyond10 = true;
                }
            }
        }

        private static int getMaxLabelCount(AlignmentSpotProperty targetSpot, IsotopeElement isotopeElement)
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
                var formula = GetConsiderableFormulaFromAccurateMass(targetSpot.MassCenter);
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

        private static int getStartIndexByRt(List<AlignmentSpotProperty> alignmentSpots, float spotRT, float rtTol)
        {
            float targetRT = spotRT - rtTol;
            int startIndex = 0, endIndex = alignmentSpots.Count - 1;
            int counter = 0;
            while (counter < 10)
            {
                if (alignmentSpots[startIndex].TimesCenter.Value <= targetRT && 
                    targetRT < alignmentSpots[(startIndex + endIndex) / 2].TimesCenter.Value)
                {
                    endIndex = (startIndex + endIndex) / 2;
                }
                else if (alignmentSpots[(startIndex + endIndex) / 2].TimesCenter.Value <= targetRT && 
                    targetRT < alignmentSpots[endIndex].TimesCenter.Value)
                {
                    startIndex = (startIndex + endIndex) / 2;
                }
                counter++;
            }
            return startIndex;
        }

        private static int getStartIndexByMz(List<AlignmentSpotProperty> alignmentSpots, float spotMz, float mzTol)
        {
            float targetMz = spotMz - mzTol;
            int startIndex = 0, endIndex = alignmentSpots.Count - 1;
            int counter = 0;
            while (counter < 10) {
                if (alignmentSpots[startIndex].MassCenter <= targetMz && 
                    targetMz < alignmentSpots[(startIndex + endIndex) / 2].MassCenter) {
                    endIndex = (startIndex + endIndex) / 2;
                }
                else if (alignmentSpots[(startIndex + endIndex) / 2].MassCenter <= targetMz && 
                    targetMz < alignmentSpots[endIndex].MassCenter) {
                    startIndex = (startIndex + endIndex) / 2;
                }
                counter++;
            }
            return startIndex;
        }

        public static void SetTargetFormulaInformation(AlignmentResultContainer alignmentResult, List<MoleculeMsReference> targetFormulas, ParameterBase param)
        {
            var alignmentSpots = new List<AlignmentSpotProperty>(alignmentResult.AlignmentSpotProperties);
            alignmentSpots = alignmentSpots.OrderBy(spot => spot.MassCenter).ToList();

            targetFormulas = targetFormulas.OrderBy(reference => reference.PrecursorMz).ToList();

            foreach (var query in targetFormulas)
            {
                var startIndex = getStartIndexByMass(alignmentSpots, (float)query.PrecursorMz, param.TextDbSearchParam.Ms1Tolerance);
                var maxID = -1; var maxScore = double.MinValue;
                MsScanMatchResult maxResult = null;
                for (int i = startIndex; i < alignmentSpots.Count; i++)
                {
                    var spotMass = alignmentSpots[i].MassCenter;
                    if (spotMass < query.PrecursorMz - param.TextDbSearchParam.Ms1Tolerance) continue;
                    if (spotMass > query.PrecursorMz + param.TextDbSearchParam.Ms1Tolerance) break;

                    var result = getTargetFormulaSimilarity(query, alignmentSpots[i], param);
                    if (maxScore < result.TotalScore)
                    {
                        maxScore = result.TotalScore;
                        maxID = i;
                        maxResult = result;
                    }
                }

                if (maxID >= 0 && maxScore > param.TextDbSearchParam.TotalScoreCutoff)
                {
                    setToAlignmentProperty(alignmentSpots[maxID], query, maxResult);
                }
            }

            alignmentSpots = alignmentSpots.OrderBy(spot => spot.AlignmentID).ToList();
            alignmentResult.AlignmentSpotProperties = new ObservableCollection<AlignmentSpotProperty>(alignmentSpots);
        }

        private static void setToAlignmentProperty(AlignmentSpotProperty spot, MoleculeMsReference query, MsScanMatchResult result)
        {
            spot.TextDbBasedMatchResult = result;
            result.Source = SourceType.TextDB;
            spot.MatchResults.AddTextDbResult(result);
            spot.Formula = query.Formula;
            spot.SetAdductType(query.AdductType);
        }

        private static MsScanMatchResult getTargetFormulaSimilarity(MoleculeMsReference query, AlignmentSpotProperty alignedSpot, ParameterBase param)
        {
            bool isMassMatch = false, isRtMatch = false;
            var similarity = 0.0;
            var massSimilarity = MsScanMatching.GetGaussianSimilarity(alignedSpot.MassCenter, query.PrecursorMz, param.CentroidMs1Tolerance, out isMassMatch);
            var rtSimilarity = -1.0;
            if (query.ChromXs.Value >= 0)
                rtSimilarity = MsScanMatching.GetGaussianSimilarity(alignedSpot.TimesCenter.Value, query.ChromXs.Value, param.TextDbSearchParam.RtTolerance, out isRtMatch);

            if (rtSimilarity < 0)
                similarity = massSimilarity;
            else
                similarity = (massSimilarity + rtSimilarity) * 0.5;

            return new MsScanMatchResult
            {
                Name = query.Name, LibraryID = query.ScanID, InChIKey = query.InChIKey,
                AcurateMassSimilarity = (float)massSimilarity, RtSimilarity = (float)rtSimilarity, TotalScore = (float)similarity,
                IsPrecursorMzMatch = isMassMatch, IsRtMatch = isRtMatch,
            };
        }


        private static int getStartIndexByMass(List<AlignmentSpotProperty> alignmentSpots, float spotMass, float massTol)
        {
            float targetMass = spotMass - massTol;
            int startIndex = 0, endIndex = alignmentSpots.Count - 1;
            int counter = 0;
            while (counter < 10)
            {
                if (alignmentSpots[startIndex].MassCenter <= targetMass && targetMass < alignmentSpots[(startIndex + endIndex) / 2].MassCenter)
                {
                    endIndex = (startIndex + endIndex) / 2;
                }
                else if (alignmentSpots[(startIndex + endIndex) / 2].MassCenter <= targetMass && targetMass < alignmentSpots[endIndex].MassCenter)
                {
                    startIndex = (startIndex + endIndex) / 2;
                }
                counter++;
            }
            return startIndex;
        }
    }
}
