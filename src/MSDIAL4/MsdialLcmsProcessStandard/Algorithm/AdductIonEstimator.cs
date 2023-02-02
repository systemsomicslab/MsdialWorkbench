using Msdial.Lcms.Dataprocess.Utility;
using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Msdial.Lcms.Dataprocess.Algorithm
{
    public class SearchedPrecursor
    {
        private double precursorMz;
        private AdductIon adductIon;

        public double PrecursorMz
        {
            get { return precursorMz; }
            set { precursorMz = value; }
        }

        public AdductIon AdductIon
        {
            get { return adductIon; }
            set { adductIon = value; }
        }
    }

    public sealed class AdductIonEstimator
    {
        private AdductIonEstimator() { }

        private static float rtMargin = 0.03F;

        /// <summary>
        /// This method adds the adduct ion information to each peak information by means of the mass difference from the identified compound's peak.
        /// Since the identified peaks should have the adduct type information as long as the MSP query has the 'PrecursorType' field correctly,
        /// this program will search the mass difference from the identified compound and then try to add the adduct ion information.
        /// 
        /// After this program, 'SetAdductIonInformation' program will also run to assign the adduct ion information for all remaining peaks.
        /// </summary>
        /// <param name="peakAreas"></param>
        /// <param name="param"></param>
        /// <param name="ionMode"></param>
        public static void SetAdductIonInformationFromIdentifiedCompound(List<PeakAreaBean> peakAreas, AnalysisParametersBean param, IonMode ionMode)
        {
            peakAreas = peakAreas.OrderBy(n => n.RtAtPeakTop).ToList();

            var searchedAdducts = new List<AdductIon>();
            for (int i = 0; i < param.AdductIonInformationBeanList.Count; i++) {
                if (param.AdductIonInformationBeanList[i].Included)
                    searchedAdducts.Add(AdductIonParcer.GetAdductIonBean(param.AdductIonInformationBeanList[i].AdductName));
            }

            for (int i = 0; i < peakAreas.Count; i++)
            {
                var peak = peakAreas[i];
                if ((peak.LibraryID >= 0 || peak.PostIdentificationLibraryId >= 0) && 
                    (peak.AdductIonName != null && peak.AdductIonName != string.Empty)) {

                    peak.AdductParent = peak.PeakID;
                        
                    // if users do not set any adduct types except for proton adduct/loss (searchedAdducts.count == 1),
                    // adduct searcher is not necessary if the peak adduct type is proton adduct/loss.
                    if (searchedAdducts.Count == 1 && (peak.AdductIonName == "[M-H]-" || peak.AdductIonName == "[M+H]+")) continue;

                    var peakRt = peakAreas[i].RtAtPeakTop;
                    var startScanIndex = DataAccessLcUtility.GetScanStartIndexByRt(peakRt - 0.01F, peakAreas);

                    var searchedPeaks = new List<PeakAreaBean>();

                    for (int j = startScanIndex; j < peakAreas.Count; j++) {
                        if (peak.PeakID == peakAreas[j].PeakID) continue;
                        if (peakAreas[j].RtAtPeakTop < peakRt - rtMargin) continue;
                        if (peakAreas[j].IsotopeWeightNumber != 0) continue;
                        if (peakAreas[j].RtAtPeakTop > peakRt + rtMargin) break;
                        searchedPeaks.Add(peakAreas[j]);
                    }

                    searchedPeaks = searchedPeaks.OrderBy(n => n.AccurateMass).ToList();
                    adductSearcherWithIdentifiedInfo(peak, searchedPeaks, searchedAdducts, param, ionMode);
                }
            }
        }

        private static void adductSearcherWithIdentifiedInfo(PeakAreaBean peak, List<PeakAreaBean> searchedPeaks, List<AdductIon> searchedAdducts, 
            AnalysisParametersBean param, IonMode ionMode)
        {
            var centralAdduct = AdductIonParcer.GetAdductIonBean(peak.AdductIonName);
            var centralExactMass = MolecularFormulaUtility.ConvertPrecursorMzToExactMass(peak.AccurateMass, centralAdduct.AdductIonAccurateMass,
                centralAdduct.ChargeNumber, centralAdduct.AdductIonXmer, ionMode);
            var ppm = MolecularFormulaUtility.PpmCalculator(200.0, 200.0 + param.AdductAndIsotopeMassTolerance); //based on m/z 400

            var searchedPrecursors = new List<SearchedPrecursor>();
            foreach (var searchedAdduct in searchedAdducts) {
                if (centralAdduct.AdductIonName == searchedAdduct.AdductIonName) continue;
                var searchedPrecursorMz = MolecularFormulaUtility.ConvertExactMassToPrecursorMz(searchedAdduct, centralExactMass);
                searchedPrecursors.Add(new SearchedPrecursor() { PrecursorMz = searchedPrecursorMz, AdductIon = searchedAdduct });
            }

            foreach (var searchedPeak in searchedPeaks) {
                foreach (var searchedPrecursor in searchedPrecursors) {

                    var adductTol = MolecularFormulaUtility.ConvertPpmToMassAccuracy(searchedPeak.AccurateMass, ppm);

                    if (Math.Abs(searchedPeak.AccurateMass - searchedPrecursor.PrecursorMz) < adductTol) {
                        var searchedAdduct = searchedPrecursor.AdductIon;

                        if (searchedPeak.AdductIonName == null || searchedPeak.AdductIonName == string.Empty) {
                            searchedPeak.AdductParent = peak.PeakID;
                            searchedPeak.AdductIonName = searchedAdduct.AdductIonName;
                            searchedPeak.AdductIonChargeNumber = searchedAdduct.ChargeNumber;
                            searchedPeak.AdductIonXmer = searchedAdduct.AdductIonXmer;
                            searchedPeak.AdductIonAccurateMass = (float)searchedAdduct.AdductIonAccurateMass;
                        }
                        else if (searchedPeak.AdductIonName == searchedPrecursor.AdductIon.AdductIonName) {
                            searchedPeak.AdductParent = peak.PeakID;
                        }

                        break;
                    }
                }
            }
        }

        /// <summary>
        /// This program is to assign the adduct ion information.
        /// The current program will search the mass difference within the same data point.
        /// The molecular ion will be assinged to [M+H]+ or [M-H]-. (of course there is a possibility that it will be [M]+ or [M]- though...)
        /// </summary>
        public static void SetAdductIonInformation(List<PeakAreaBean> peakAreaList, AnalysisParametersBean param, IonMode ionMode)
        {
            peakAreaList = peakAreaList.OrderBy(n => n.ScanNumberAtPeakTop).ToList();

            var searchedAdducts = new List<AdductIon>();
            for (int i = 0; i < param.AdductIonInformationBeanList.Count; i++) {
                if (param.AdductIonInformationBeanList[i].Included)
                    searchedAdducts.Add(AdductIonParcer.GetAdductIonBean(param.AdductIonInformationBeanList[i].AdductName));
            }

            if (searchedAdducts.Count > 1) { //adduct searcher will be used when users selected several adducts for searching
                foreach (var peak in peakAreaList) {

                    if (peak.AdductIonName != null && peak.AdductIonName != string.Empty) continue;
                    if (peak.IsotopeWeightNumber != 0)
                        continue;

                    var peakRt = peak.RtAtPeakTop;
                    var startScanIndex = DataAccessLcUtility.GetScanStartIndexByRt(peakRt - 0.01F, peakAreaList);
                    var searchedPeaks = new List<PeakAreaBean>();

                    for (int i = startScanIndex; i < peakAreaList.Count; i++) {

                        if (peak.PeakID == peakAreaList[i].PeakID) continue;
                        if (peakAreaList[i].RtAtPeakTop < peakRt - rtMargin) continue;
                        if (peakAreaList[i].IsotopeWeightNumber != 0) continue;
                        if (peakAreaList[i].AdductIonName != null && peakAreaList[i].AdductIonName != string.Empty) continue;
                        if (peakAreaList[i].LibraryID >= 0 || peakAreaList[i].PostIdentificationLibraryId >= 0) continue;
                        if (peakAreaList[i].RtAtPeakTop > peakRt + rtMargin) break;

                        searchedPeaks.Add(peakAreaList[i]);
                    }

                    searchedPeaks = searchedPeaks.OrderBy(n => n.AccurateMass).ToList();
                    adductSearcher(peak, searchedPeaks, searchedAdducts, param, ionMode);
                }
            }

            //finalization
            peakAreaList = peakAreaList.OrderBy(n => n.PeakID).ToList();
            var defaultAdduct = searchedAdducts[0];
            var defaultAdduct2 = AdductIonParcer.ConvertDifferentChargedAdduct(defaultAdduct, 2);
            foreach (var peak in peakAreaList.Where(n => n.IsotopeWeightNumber == 0)) {
                if (peak.AdductParent >= 0 && peak.AdductIonName != string.Empty)
                    continue;

                peak.AdductParent = peak.PeakID;

                if (peak.ChargeNumber == 2) {
                    peak.AdductIonName = defaultAdduct2.AdductIonName;
                    peak.AdductIonXmer = defaultAdduct2.AdductIonXmer;
                    peak.AdductIonChargeNumber = defaultAdduct2.ChargeNumber;
                    peak.AdductIonAccurateMass = (float)defaultAdduct2.AdductIonAccurateMass;
                }
                else {
                    peak.AdductIonName = defaultAdduct.AdductIonName;
                    peak.AdductIonXmer = defaultAdduct.AdductIonXmer;
                    peak.AdductIonChargeNumber = defaultAdduct.ChargeNumber;
                    peak.AdductIonAccurateMass = (float)defaultAdduct.AdductIonAccurateMass;
                }
            }

            foreach (var peak in peakAreaList) {
                if (peak.IsotopeParentPeakID >= 0 && peak.IsotopeWeightNumber > 0) {

                    var parentPeak = peakAreaList[peak.IsotopeParentPeakID];

                    peak.AdductParent = parentPeak.AdductParent;
                    peak.AdductIonName = parentPeak.AdductIonName;
                    peak.AdductIonXmer = parentPeak.AdductIonXmer;
                    peak.AdductIonChargeNumber = parentPeak.ChargeNumber;
                    peak.AdductIonAccurateMass = (float)parentPeak.AdductIonAccurateMass;
                }
            }

            //refine the dependency
            foreach (var peak in peakAreaList) {
                if (peak.PeakID == peak.AdductParent) continue;
                var parentID = peak.AdductParent;

                if (peakAreaList[parentID].AdductParent != peakAreaList[parentID].PeakID) {
                    var parentParentID = peakAreaList[parentID].AdductParent;
                    if (peakAreaList[parentParentID].AdductParent == peakAreaList[parentParentID].PeakID)
                        peak.AdductParent = peakAreaList[parentParentID].PeakID;
                    else {
                        var parentParentParentID = peakAreaList[parentParentID].AdductParent;
                        if (peakAreaList[parentParentParentID].AdductParent == peakAreaList[parentParentParentID].PeakID)
                            peak.AdductParent = peakAreaList[parentParentParentID].PeakID;
                    }
                }
            }
        }

        private static void adductSearcher(PeakAreaBean peak, List<PeakAreaBean> searchedPeaks, List<AdductIon> searchedAdducts,
            AnalysisParametersBean param, IonMode ionMode)
        {
            var flg = false;
            var ppm = MolecularFormulaUtility.PpmCalculator(200.0, 200.0 + param.AdductAndIsotopeMassTolerance); //based on m/z 200

            foreach (var centralAdduct in searchedAdducts) {

                var rCentralAdduct = AdductIonParcer.ConvertDifferentChargedAdduct(centralAdduct, peak.ChargeNumber);

                var centralExactMass = MolecularFormulaUtility.ConvertPrecursorMzToExactMass(peak.AccurateMass, rCentralAdduct.AdductIonAccurateMass,
                    rCentralAdduct.ChargeNumber, rCentralAdduct.AdductIonXmer, ionMode);

                var searchedPrecursors = new List<SearchedPrecursor>();
                foreach (var searchedAdduct in searchedAdducts) {
                    if (rCentralAdduct.AdductIonName == searchedAdduct.AdductIonName) continue;
                    var searchedPrecursorMz = MolecularFormulaUtility.ConvertExactMassToPrecursorMz(searchedAdduct, centralExactMass);
                    searchedPrecursors.Add(new SearchedPrecursor() { PrecursorMz = searchedPrecursorMz, AdductIon = searchedAdduct });
                }

                foreach (var searchedPeak in searchedPeaks) {
                    foreach (var searchedPrecursor in searchedPrecursors) {

                        var adductTol = MolecularFormulaUtility.ConvertPpmToMassAccuracy(searchedPeak.AccurateMass, ppm);

                        if (Math.Abs(searchedPeak.AccurateMass - searchedPrecursor.PrecursorMz) < adductTol) {

                            var searchedAdduct = searchedPrecursor.AdductIon;

                            if (searchedPeak.AdductIonName == null || searchedPeak.AdductIonName == string.Empty) {
                                searchedPeak.AdductParent = peak.PeakID;
                                searchedPeak.AdductIonName = searchedAdduct.AdductIonName;
                                searchedPeak.AdductIonChargeNumber = searchedAdduct.ChargeNumber;
                                searchedPeak.AdductIonXmer = searchedAdduct.AdductIonXmer;
                                searchedPeak.AdductIonAccurateMass = (float)searchedAdduct.AdductIonAccurateMass;
                            }

                            flg = true;
                            break;
                        }
                    }
                }

                if (flg) {
                    peak.AdductParent = peak.PeakID;
                    peak.AdductIonName = rCentralAdduct.AdductIonName;
                    peak.AdductIonXmer = rCentralAdduct.AdductIonXmer;
                    peak.AdductIonChargeNumber = rCentralAdduct.ChargeNumber;
                    peak.AdductIonAccurateMass = (float)rCentralAdduct.AdductIonAccurateMass;
                    break;
                }
            }
        }
    }
}
