using Msdial.Gcms.Dataprocess.Utility;
using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Msdial.Gcms.Dataprocess.Algorithm
{
    public sealed class IsotopeEstimator
    {
        /// <summary>
        /// This method tries to decide if the detected peak is the isotopic ion or not.
        /// The peaks less than the abundance of the mono isotopic ion will be assigned to the isotopic ions within the same data point.
        /// </summary>
        /// <param name="peakAreaBeanList"></param>
        /// <param name="param"></param>
        public static void SetIsotopeInformation(List<PeakAreaBean> peakAreaBeanList, AnalysisParamOfMsdialGcms param)
        {
            peakAreaBeanList = peakAreaBeanList.OrderBy(n => n.AccurateMass).ToList();

            var spectrumMargin = 2;
            var rtMargin = 0.015F;

            foreach (var peak in peakAreaBeanList) {

                if (peak.IsotopeWeightNumber >= 0) continue;

                var focusedScan = peak.ScanNumberAtPeakTop;
                var focusedMass = peak.AccurateMass;
                var focusedRt = peak.RtAtPeakTop;

                var startScanIndex = DataAccessGcUtility.GetScanStartIndexByMz(focusedMass - 0.0001F, peakAreaBeanList);
                var isotopeCandidates = new List<PeakAreaBean>() { peak };

                for (int j = startScanIndex; j < peakAreaBeanList.Count; j++) {

                    if (peakAreaBeanList[j].PeakID == peak.PeakID) continue;

                    if (peakAreaBeanList[j].ScanNumberAtPeakTop < focusedScan - spectrumMargin ||
                        peakAreaBeanList[j].RtAtPeakTop < focusedRt - rtMargin) continue;

                    if (peakAreaBeanList[j].ScanNumberAtPeakTop > focusedScan + spectrumMargin ||
                        peakAreaBeanList[j].RtAtPeakTop > focusedRt + rtMargin) continue;

                    if (peakAreaBeanList[j].IsotopeWeightNumber >= 0) continue;
                    if (peakAreaBeanList[j].AccurateMass <= focusedMass) continue;
                    if (peakAreaBeanList[j].AccurateMass > focusedMass + 8.1) break;

                    isotopeCandidates.Add(peakAreaBeanList[j]);
                }
                isotopeCalculation(isotopeCandidates, param);
            }
            peakAreaBeanList = peakAreaBeanList.OrderBy(n => n.PeakID).ToList();
        }

        private static void isotopeCalculation(List<PeakAreaBean> peakAreaBeanList, AnalysisParamOfMsdialGcms param)
        {
            var c13_c12Diff = MassDiffDictionary.C13_C12;  //1.003355F;
            var tolerance = param.MassAccuracy; if (param.AccuracyType == AccuracyType.IsNominal) tolerance = 0.5F;
            var monoIsoPeak = peakAreaBeanList[0];
            var ppm = MolecularFormulaUtility.PpmCalculator(200.0, 200.0 + tolerance); //based on m/z 200
            var accuracy = MolecularFormulaUtility.ConvertPpmToMassAccuracy(monoIsoPeak.AccurateMass, ppm);

            tolerance = (float)accuracy;
            var isFinished = false;

            monoIsoPeak.IsotopeWeightNumber = 0;
            monoIsoPeak.IsotopeParentPeakID = monoIsoPeak.PeakID;

            var reminderIntensity = monoIsoPeak.IntensityAtPeakTop;
            var reminderIndex = 1;

            for (int i = 1; i <= 8; i++) {

                var isotopicMassSingleCharged = (double)monoIsoPeak.AccurateMass + (double)i * c13_c12Diff;

                for (int j = reminderIndex; j < peakAreaBeanList.Count; j++) {

                    var isotopePeak = peakAreaBeanList[j];

                    if (isotopicMassSingleCharged - tolerance <= isotopePeak.AccurateMass &&
                        isotopePeak.AccurateMass <= isotopicMassSingleCharged + tolerance) {

                        if (reminderIntensity > isotopePeak.IntensityAtPeakTop) {

                            isotopePeak.IsotopeParentPeakID = monoIsoPeak.PeakID;
                            isotopePeak.IsotopeWeightNumber = i;

                            reminderIntensity = isotopePeak.IntensityAtPeakTop;
                            reminderIndex = j + 1;
                            break;
                        }
                        else {
                            isFinished = true;
                            break;
                        }
                    }
                }
                if (isFinished) break;
            }
        }
    }
}
