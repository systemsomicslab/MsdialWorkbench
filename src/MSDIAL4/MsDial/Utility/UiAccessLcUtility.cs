using Msdial.Lcms.Dataprocess.Algorithm;
using Msdial.Lcms.Dataprocess.Utility;
using CompMs.RawDataHandler.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using CompMs.Common.DataObj;

namespace Rfx.Riken.OsakaUniv
{
    public sealed class UiAccessLcUtility
    {
        private UiAccessLcUtility() { }

        public static List<DriftSpotBean> GetDriftSpotBean(IEnumerable<PeakAreaBean> peakSpots) {
            var driftSpots = peakSpots.SelectMany(peak => peak.DriftSpots.OrEmptyIfNull());
            if (driftSpots == null)
                return new List<DriftSpotBean>();
            return driftSpots.OrderBy(n => n.IntensityAtPeakTop).ToList();
        }

        public static List<AlignedDriftSpotPropertyBean> GetDriftSpotBean(IEnumerable<AlignmentPropertyBean> alignedSpots) {
            var driftSpots = alignedSpots.SelectMany(spot => spot.AlignedDriftSpots.OrEmptyIfNull());
            if (driftSpots == null)
                return new List<AlignedDriftSpotPropertyBean>();
            return driftSpots.OrderBy(n => n.MaxValiable).ToList();
        }

        public static PairwisePlotBean GetRtMzPairwisePlotPeakViewBean(AnalysisFileBean analysisFileBean, bool isColoredByCompoundClass = false, List<MspFormatCompoundInformationBean> mspDB = null)
        {
            var xAxisRtDatapointCollection = new ObservableCollection<double>();
            var yAxisMzDatapointCollection = new ObservableCollection<double>();
            var plotBrushCollection = new ObservableCollection<SolidColorBrush>();
            var peakAreaBeanCollection = new ObservableCollection<PeakAreaBean>();

            string xAxisTitle = "Retention time [min]";
            string yAxisTitle = "m/z";
            string graphTitle = analysisFileBean.AnalysisFilePropertyBean.AnalysisFileName;

            var peakSpots = analysisFileBean.PeakAreaBeanCollection;

            for (int i = 0; i < peakSpots.Count; i++)
            {
                xAxisRtDatapointCollection.Add(peakSpots[i].RtAtPeakTop);
                yAxisMzDatapointCollection.Add(peakSpots[i].AccurateMass);

                if (isColoredByCompoundClass == false || mspDB == null || mspDB.Count == 0) {
                    plotBrushCollection.Add(new SolidColorBrush(Color.FromArgb(180
                        , (byte)(255 * peakSpots[i].AmplitudeScoreValue)
                        , (byte)(255 * (1 - Math.Abs(peakSpots[i].AmplitudeScoreValue - 0.5)))
                        , (byte)(255 - 255 * peakSpots[i].AmplitudeScoreValue))));
                }
                else {
                    var brushes = getSolidColarBrushForCompoundClass(peakSpots[i].LibraryID, mspDB);
                    plotBrushCollection.Add(brushes);
                }
                    

                peakAreaBeanCollection.Add(peakSpots[i]);
            }

            return new PairwisePlotBean(graphTitle, xAxisTitle, yAxisTitle, xAxisRtDatapointCollection, yAxisMzDatapointCollection, peakAreaBeanCollection, plotBrushCollection, PairwisePlotDisplayLabel.None);
        }

        public static PairwisePlotBean GetDriftTimeMzPairwisePlotPeakViewBean(AnalysisFileBean file, int peakSpotID, IonMobilityType mobilityType, 
            AnalysisParametersBean param,
            out int repSpotID, bool isColoredByCompoundClass = false, List<MspFormatCompoundInformationBean> mspDB = null) {

            repSpotID = 0;
            var summary = file.DataSummaryBean;
            var xAxisRtDatapointCollection = new ObservableCollection<double>();
            var yAxisMzDatapointCollection = new ObservableCollection<double>();
            var plotBrushCollection = new ObservableCollection<SolidColorBrush>();
            var driftSpotCollection = new ObservableCollection<DriftSpotBean>();

            string xAxisTitle = getDriftAxisTitle(mobilityType);
            string yAxisTitle = "m/z";
            string graphTitle = file.AnalysisFilePropertyBean.AnalysisFileName;

            var peakSpots = file.PeakAreaBeanCollection;
            if (peakSpots == null || peakSpots.Count == 0) return null;
            
            var targetSpot = peakSpots[peakSpotID];
            var driftSpots = getDriftSpotsAtTargetSpotRange(peakSpots, targetSpot, param.AccumulatedRtRagne, out repSpotID);

            for (int i = 0; i < driftSpots.Count; i++) {
                xAxisRtDatapointCollection.Add(driftSpots[i].DriftTimeAtPeakTop);
                yAxisMzDatapointCollection.Add(driftSpots[i].AccurateMass);

                if (isColoredByCompoundClass == false || mspDB == null || mspDB.Count == 0) {
                    plotBrushCollection.Add(new SolidColorBrush(Color.FromArgb(180
                        , (byte)(255 * driftSpots[i].AmplitudeScoreValue)
                        , (byte)(255 * (1 - Math.Abs(driftSpots[i].AmplitudeScoreValue - 0.5)))
                        , (byte)(255 - 255 * driftSpots[i].AmplitudeScoreValue))));
                }
                else {
                    var brushes = getSolidColarBrushForCompoundClass(driftSpots[i].LibraryID, mspDB);
                    plotBrushCollection.Add(brushes);
                }
                driftSpotCollection.Add(driftSpots[i]);
            }

            if (driftSpotCollection.Count == 0) return null;

            return new PairwisePlotBean(graphTitle, xAxisTitle, yAxisTitle, xAxisRtDatapointCollection, yAxisMzDatapointCollection,
                driftSpotCollection, plotBrushCollection, PairwisePlotDisplayLabel.None, 
                summary.MinDriftTime, summary.MaxDriftTime, summary.MinMass, summary.MaxMass, targetSpot.AccurateMass);
        }

        private static string getDriftAxisTitle(IonMobilityType type) {
            string xAxisTitle = "Mobility [1/K0]";
            if (type == IonMobilityType.Dtims) {
                xAxisTitle = "Mobility [millisecond]";
            }
            else if (type == IonMobilityType.Twims) {
                xAxisTitle = "Mobility [millisecond]";
            }
            else if (type == IonMobilityType.CCS) {
                xAxisTitle = "CCS";
            }
            return xAxisTitle;
        }

        private static List<DriftSpotBean> getDriftSpotsAtTargetSpotRange(ObservableCollection<PeakAreaBean> peakSpots, PeakAreaBean targetSpot,
            float rtWidth, out int representativeSpotID) {
            representativeSpotID = -1;

            var driftSpots = new List<DriftSpotBean>();
            var targetMz = targetSpot.AccurateMass;
            var targetMzWidth = 10;
            var targetRt = targetSpot.RtAtPeakTop;
            //var targetRtWidth = targetSpot.RtAtRightPeakEdge - targetSpot.RtAtLeftPeakEdge;
            //if (targetRtWidth > 0.5) targetRtWidth = 0.5F;

            var maxIntensity = float.MinValue;
            var minIntensity = float.MaxValue;
            foreach (var peakSpot in peakSpots) {
                var mz = peakSpot.AccurateMass;
                var rt = peakSpot.RtAtPeakTop;
                //if (Math.Abs(rt - targetRt) < targetRtWidth * 0.5 && Math.Abs(mz - targetMz) < targetMzWidth) {
                if (Math.Abs(rt - targetRt) < rtWidth * 0.5) {
                    foreach (var driftSpot in peakSpot.DriftSpots.OrEmptyIfNull()) {
                        driftSpots.Add(driftSpot);
                        if (driftSpot.IntensityAtPeakTop > maxIntensity) maxIntensity = driftSpot.IntensityAtPeakTop;
                        if (driftSpot.IntensityAtPeakTop < minIntensity) minIntensity = driftSpot.IntensityAtPeakTop;
                    }
                }
            }
            if (driftSpots == null || driftSpots.Count == 0) return new List<DriftSpotBean>();

            driftSpots = driftSpots.OrderBy(n => n.IntensityAtPeakTop).ToList();
            //var counter = 0;

            var maxIdentID = -1;
            var maxIdentScore = 0.0;
            var maxIntensityID = -1;
            var maxInt = 0.0;
            var maxIntensityWithMsMsID = -1;
            var maxIntensityWithMsMs = 0.0;

            for (int i = 0; i < driftSpots.Count; i++) {
                var spot = driftSpots[i];
                spot.AmplitudeScoreValue = spot.IntensityAtPeakTop / maxIntensity;
                spot.AmplitudeOrderValue = i + 1;
                spot.DisplayedSpotID = i;

                if (spot.PeakAreaBeanID == targetSpot.PeakID) {
                    if (representativeSpotID == -1) // just initialize
                        representativeSpotID = spot.DisplayedSpotID;
                    if (spot.Ms2LevelDatapointNumber >= 0) {
                        if (spot.LibraryID >= 0) {
                            if (spot.MetaboliteName.Contains("w/o")) {
                                if (maxIntensityWithMsMsID == -1) {
                                    maxIntensityWithMsMs = spot.IntensityAtPeakTop;
                                    maxIntensityWithMsMsID = i;
                                }
                                else if (spot.IntensityAtPeakTop > maxIntensityWithMsMs) {
                                    maxIntensityWithMsMs = spot.IntensityAtPeakTop;
                                    maxIntensityWithMsMsID = i;
                                }
                            }
                            else {
                                if (maxIdentID == -1) {
                                    maxIdentScore = spot.TotalScore;
                                    maxIdentID = i;
                                }
                                else if (maxIdentScore < spot.TotalScore) {
                                    maxIdentScore = spot.TotalScore;
                                    maxIdentID = i;
                                }
                            }
                        }
                        else {
                            if (maxIntensityWithMsMsID == -1) {
                                maxIntensityWithMsMs = spot.IntensityAtPeakTop;
                                maxIntensityWithMsMsID = i;
                            }
                            else if (spot.IntensityAtPeakTop > maxIntensityWithMsMs) {
                                maxIntensityWithMsMs = spot.IntensityAtPeakTop;
                                maxIntensityWithMsMsID = i;
                            }
                        }
                    }
                    else {
                        if (maxIntensityID == -1) {
                            maxInt = spot.IntensityAtPeakTop;
                            maxIntensityID = i;
                        }
                        else if (spot.IntensityAtPeakTop > maxInt) {
                            maxInt = spot.IntensityAtPeakTop;
                            maxIntensityID = i;
                        }
                    }
                }
            }

            if (maxIdentID >= 0) {
                representativeSpotID = maxIdentID;
            }
            else if (maxIntensityWithMsMsID >= 0) {
                representativeSpotID = maxIntensityWithMsMsID;
            }
            else {
                representativeSpotID = maxIntensityID;
            }

            if (representativeSpotID < 0) representativeSpotID = 0;

            //foreach (var spot in driftSpots.OrEmptyIfNull()) {
            //    spot.AmplitudeScoreValue = spot.IntensityAtPeakTop / maxIntensity;
            //    spot.AmplitudeOrderValue = counter + 1;
            //    spot.DisplayedSpotID = counter;
            //    counter++;

            //    if (spot.PeakAreaBeanID == targetSpot.PeakID) {

            //        if (representativeSpotID == 0) // just initialize
            //            representativeSpotID = spot.DisplayedSpotID;



            //        //if (representativeSpotID == 0) // just initialize
            //        //    representativeSpotID = spot.DisplayedSpotID;
            //        //else {
            //        //    if (driftSpots[representativeSpotID].Ms2LevelDatapointNumber < 0 && spot.Ms2LevelDatapointNumber >= 0) {
            //        //        representativeSpotID = spot.DisplayedSpotID;
            //        //    }
            //        //}
            //    }
            //}
            return driftSpots;
        }

        private static SolidColorBrush getSolidColarBrushForCompoundClass(int mspID, List<MspFormatCompoundInformationBean> mspDB)
        {
            //cite http://noz.day-break.net/webcolor/colorname.html for the colors

            if (mspID < 0 || mspDB.Count - 1 < mspID) return new SolidColorBrush(Color.FromArgb(180, 128, 128, 128)); // if unknown, gray
            var compoundClass = mspDB[mspID].CompoundClass;
            if (compoundClass == null || compoundClass == string.Empty) return new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)); // if null, black

            switch (compoundClass) {
                case "SM": return new SolidColorBrush(Color.FromArgb(180, 255, 165, 0)); // orange
                case "ASM": return new SolidColorBrush(Color.FromArgb(180, 245, 155, 10)); // orange modified
                case "Sphingosine": return new SolidColorBrush(Color.FromArgb(180, 235, 145, 20)); // orange modified
                case "Sphinganine": return new SolidColorBrush(Color.FromArgb(180, 235, 145, 20)); // orange modified
                case "Phytosphingosine": return new SolidColorBrush(Color.FromArgb(180, 235, 145, 20)); // orange modified
                case "Sph": return new SolidColorBrush(Color.FromArgb(180, 235, 145, 20)); // orange modified
                case "DHSph": return new SolidColorBrush(Color.FromArgb(180, 235, 145, 20)); // orange modified
                case "PhytoSph": return new SolidColorBrush(Color.FromArgb(180, 235, 145, 20)); // orange modified
                case "CE": return new SolidColorBrush(Color.FromArgb(180, 210, 105, 30)); // chocolate
                case "Cholesterol": return new SolidColorBrush(Color.FromArgb(180, 210, 105, 30)); // chocolate
                case "CholesterolSulfate": return new SolidColorBrush(Color.FromArgb(180, 210, 105, 30)); // chocolate
                case "Vitamin": return new SolidColorBrush(Color.FromArgb(180, 200, 95, 20)); // chocolate modified
                case "BileAcid": return new SolidColorBrush(Color.FromArgb(180, 190, 85, 10)); // chocolate modified
                case "VAE": return new SolidColorBrush(Color.FromArgb(180, 180, 75, 0)); // chocolate modified
                case "DCAE": return new SolidColorBrush(Color.FromArgb(180, 230, 125, 50)); // chocolate modified
                case "GDCAE": return new SolidColorBrush(Color.FromArgb(180, 230, 125, 55)); // chocolate modified
                case "GLCAE": return new SolidColorBrush(Color.FromArgb(180, 230, 125, 60)); // chocolate modified
                case "TDCAE": return new SolidColorBrush(Color.FromArgb(180, 230, 125, 65)); // chocolate modified
                case "TLCAE": return new SolidColorBrush(Color.FromArgb(180, 230, 125, 70)); // chocolate modified
                case "LCAE": return new SolidColorBrush(Color.FromArgb(180, 230, 125, 75)); // chocolate modified
                case "KLCAE": return new SolidColorBrush(Color.FromArgb(180, 230, 125, 80)); // chocolate modified
                case "KDCAE": return new SolidColorBrush(Color.FromArgb(180, 230, 125, 85)); // chocolate modified
                case "BRSE": return new SolidColorBrush(Color.FromArgb(180, 210, 145, 30)); // chocolate modified
                case "CASE": return new SolidColorBrush(Color.FromArgb(180, 210, 145, 35)); // chocolate modified
                case "SISE": return new SolidColorBrush(Color.FromArgb(180, 210, 145, 40)); // chocolate modified
                case "STSE": return new SolidColorBrush(Color.FromArgb(180, 210, 145, 45)); // chocolate modified
                case "EGSE": return new SolidColorBrush(Color.FromArgb(180, 210, 145, 50)); // chocolate modified
                case "DEGSE": return new SolidColorBrush(Color.FromArgb(180, 210, 145, 55)); // chocolate modified
                case "DSMSE": return new SolidColorBrush(Color.FromArgb(180, 210, 145, 60)); // chocolate modified
                case "AHexBRS": return new SolidColorBrush(Color.FromArgb(180, 200, 165, 20)); // chocolate modified
                case "AHexCAS": return new SolidColorBrush(Color.FromArgb(180, 200, 165, 20)); // chocolate modified
                case "AHexCS": return new SolidColorBrush(Color.FromArgb(180, 200, 165, 20)); // chocolate modified
                case "AHexSIS": return new SolidColorBrush(Color.FromArgb(180, 200, 165, 20)); // chocolate modified
                case "AHexSTS": return new SolidColorBrush(Color.FromArgb(180, 200, 165, 20)); // chocolate modified
                case "SPE": return new SolidColorBrush(Color.FromArgb(180, 129, 49, 9)); // saddlebrown modified
                case "SHex": return new SolidColorBrush(Color.FromArgb(180, 129, 49, 9)); // saddlebrown modified
                case "SPEHex": return new SolidColorBrush(Color.FromArgb(180, 129, 49, 9)); // saddlebrown modified
                case "SPGHex": return new SolidColorBrush(Color.FromArgb(180, 129, 49, 9)); // saddlebrown modified
                case "CSLPHex": return new SolidColorBrush(Color.FromArgb(180, 129, 49, 9)); // saddlebrown modified
                case "BRSLPHex": return new SolidColorBrush(Color.FromArgb(180, 129, 49, 9)); // saddlebrown modified
                case "CASLPHex": return new SolidColorBrush(Color.FromArgb(180, 129, 49, 9)); // saddlebrown modified
                case "SISLPHex": return new SolidColorBrush(Color.FromArgb(180, 129, 49, 9)); // saddlebrown modified
                case "STSLPHex": return new SolidColorBrush(Color.FromArgb(180, 129, 49, 9)); // saddlebrown modified
                case "CSPHex": return new SolidColorBrush(Color.FromArgb(180, 129, 49, 9)); // saddlebrown modified
                case "BRSPHex": return new SolidColorBrush(Color.FromArgb(180, 129, 49, 9)); // saddlebrown modified
                case "CASPHex": return new SolidColorBrush(Color.FromArgb(180, 129, 49, 9)); // saddlebrown modified
                case "SISPHex": return new SolidColorBrush(Color.FromArgb(180, 129, 49, 9)); // saddlebrown modified
                case "STSPHex": return new SolidColorBrush(Color.FromArgb(180, 129, 49, 9)); // saddlebrown modified
                case "FA": return new SolidColorBrush(Color.FromArgb(180, 139, 69, 19)); // saddlebrown
                case "OxFA": return new SolidColorBrush(Color.FromArgb(180, 89, 39, 49)); // saddlebrown modified
                case "FAHFA": return new SolidColorBrush(Color.FromArgb(180, 148, 0, 211)); // darkviolet
                case "LDGTS": return new SolidColorBrush(Color.FromArgb(180, 255, 0, 0)); // red
                case "LPC": return new SolidColorBrush(Color.FromArgb(180, 50, 205, 50)); // limegreen
                case "LPE": return new SolidColorBrush(Color.FromArgb(180, 128, 0, 128)); // purple
                case "LPS": return new SolidColorBrush(Color.FromArgb(180, 0, 255, 255)); // aqua
                case "LPG": return new SolidColorBrush(Color.FromArgb(180, 184, 134, 11)); // darkgoldenrod
                case "LPI": return new SolidColorBrush(Color.FromArgb(180, 255, 0, 255)); // fuchsia
                case "LPA": return new SolidColorBrush(Color.FromArgb(180, 218, 102, 204)); // orchid modified
                case "PA": return new SolidColorBrush(Color.FromArgb(180, 218, 112, 214)); // orchid
                case "PC": return new SolidColorBrush(Color.FromArgb(180, 50, 205, 50)); // limegreen
                case "PE": return new SolidColorBrush(Color.FromArgb(180, 128, 0, 128)); // purple
                case "MMPE": return new SolidColorBrush(Color.FromArgb(180, 128, 0, 148)); // purple modified
                case "DMPE": return new SolidColorBrush(Color.FromArgb(180, 128, 0, 168)); // purple modified
                case "PG": return new SolidColorBrush(Color.FromArgb(180, 184, 134, 11)); // darkgoldenrod
                case "PI": return new SolidColorBrush(Color.FromArgb(180, 255, 0, 255)); // fuchsia
                case "PS": return new SolidColorBrush(Color.FromArgb(180, 0, 255, 255)); // aqua
                case "PT": return new SolidColorBrush(Color.FromArgb(180, 35, 255, 255)); // aqua modified
                case "OxPA": return new SolidColorBrush(Color.FromArgb(180, 228, 122, 224)); // orchid
                case "OxPC": return new SolidColorBrush(Color.FromArgb(180, 60, 215, 60)); // limegreen
                case "OxPE": return new SolidColorBrush(Color.FromArgb(180, 138, 10, 138)); // purple
                case "OxPG": return new SolidColorBrush(Color.FromArgb(180, 194, 144, 21)); // darkgoldenrod
                case "OxPI": return new SolidColorBrush(Color.FromArgb(180, 245, 10, 245)); // fuchsia
                case "OxPS": return new SolidColorBrush(Color.FromArgb(180, 10, 245, 245)); // aqua
                case "BMP": return new SolidColorBrush(Color.FromArgb(180, 255, 69, 0)); // orangered
                case "LNAPE": return new SolidColorBrush(Color.FromArgb(180, 138, 10, 138)); // orangered
                case "LNAPS": return new SolidColorBrush(Color.FromArgb(180, 10, 245, 245)); // orangered
                case "CL": return new SolidColorBrush(Color.FromArgb(180, 128, 0, 0)); // maroon
                case "LCL": return new SolidColorBrush(Color.FromArgb(180, 255, 0, 0)); // red
                case "MLCL": return new SolidColorBrush(Color.FromArgb(180, 255, 0, 0)); // red
                case "DLCL": return new SolidColorBrush(Color.FromArgb(180, 255, 0, 0)); // red
                case "HBMP": return new SolidColorBrush(Color.FromArgb(180, 255, 140, 0)); // darkorange
                case "PMeOH": return new SolidColorBrush(Color.FromArgb(180, 35, 20, 183)); //cobalt blue
                case "PEtOH": return new SolidColorBrush(Color.FromArgb(180, 35, 20, 183)); //cobalt blue
                case "PBtOH": return new SolidColorBrush(Color.FromArgb(180, 35, 20, 183)); //cobalt blue
                case "EtherPC": return new SolidColorBrush(Color.FromArgb(180, 255, 99, 71)); // tomato
                case "EtherPE": return new SolidColorBrush(Color.FromArgb(180, 75, 0, 130)); // indigo
                case "EtherPG": return new SolidColorBrush(Color.FromArgb(180, 174, 124, 1)); // darkgoldenrod modified
                case "EtherPS": return new SolidColorBrush(Color.FromArgb(180, 10, 245, 245)); // aqua modified
                case "EtherPI": return new SolidColorBrush(Color.FromArgb(180, 245, 10, 245)); // fuchsia modified
                case "EtherLPC": return new SolidColorBrush(Color.FromArgb(180, 255, 99, 71)); // tomato
                case "EtherLPE": return new SolidColorBrush(Color.FromArgb(180, 75, 0, 130)); // indigo
                case "EtherLPG": return new SolidColorBrush(Color.FromArgb(180, 174, 124, 1)); // darkgoldenrod midified
                case "EtherLPS": return new SolidColorBrush(Color.FromArgb(180, 20, 235, 235)); // aqua modified
                case "EtherLPI": return new SolidColorBrush(Color.FromArgb(180, 235, 20, 235)); // fuchsia modified
                case "EtherOxPC": return new SolidColorBrush(Color.FromArgb(180, 60, 215, 60)); // limegreen
                case "EtherOxPE": return new SolidColorBrush(Color.FromArgb(180, 138, 10, 138)); // purple
                case "MAG": return new SolidColorBrush(Color.FromArgb(180, 152, 251, 152)); // palegreen
                case "DAG": return new SolidColorBrush(Color.FromArgb(180, 0, 255, 0)); // lime
                case "TAG": return new SolidColorBrush(Color.FromArgb(180, 0, 128, 0)); // green
                case "EtherDAG": return new SolidColorBrush(Color.FromArgb(180, 0, 204, 0)); // lime
                case "EtherTAG": return new SolidColorBrush(Color.FromArgb(180, 0, 51, 0)); // green
                case "MG": return new SolidColorBrush(Color.FromArgb(180, 152, 251, 152)); // palegreen
                case "DG": return new SolidColorBrush(Color.FromArgb(180, 0, 255, 0)); // lime
                case "TG": return new SolidColorBrush(Color.FromArgb(180, 0, 128, 0)); // green
                case "OxTG": return new SolidColorBrush(Color.FromArgb(180, 0, 128, 20)); // green modified
                case "TG_EST": return new SolidColorBrush(Color.FromArgb(180, 0, 128, 40)); // green modified
                case "EtherDG": return new SolidColorBrush(Color.FromArgb(180, 0, 204, 0)); // lime
                case "EtherTG": return new SolidColorBrush(Color.FromArgb(180, 0, 51, 0)); // green
                case "MGDG": return new SolidColorBrush(Color.FromArgb(180, 0, 0, 255)); // blue
                case "DGDG": return new SolidColorBrush(Color.FromArgb(180, 139, 0, 0)); // darkred
                case "SQDG": return new SolidColorBrush(Color.FromArgb(180, 0, 255, 127)); // springgreen
                case "GlcADG": return new SolidColorBrush(Color.FromArgb(180, 85, 107, 47)); // darkolivegreen
                case "AcylGlcADG": return new SolidColorBrush(Color.FromArgb(180, 46, 139, 87)); // seagreen
                case "DGGA": return new SolidColorBrush(Color.FromArgb(180, 85, 107, 47)); // darkolivegreen
                case "ADGGA": return new SolidColorBrush(Color.FromArgb(180, 46, 139, 87)); // seagreen
                case "DGTS": return new SolidColorBrush(Color.FromArgb(180, 255, 0, 0)); // red
                case "LDGCC": return new SolidColorBrush(Color.FromArgb(180, 245, 10, 10)); // red modified
                case "DGCC": return new SolidColorBrush(Color.FromArgb(180, 245, 10, 10)); // red modified
                case "EtherMGDG": return new SolidColorBrush(Color.FromArgb(180, 51, 0, 255)); // blue
                case "ACar": return new SolidColorBrush(Color.FromArgb(180, 255, 215, 0)); // gold
                case "CAR": return new SolidColorBrush(Color.FromArgb(180, 255, 215, 0)); // gold
                case "Cer_ADS": return new SolidColorBrush(Color.FromArgb(180, 173, 216, 230)); // lightblue
                case "Cer_AS": return new SolidColorBrush(Color.FromArgb(180, 0, 191, 255)); // deepskyblue
                case "Cer_BDS": return new SolidColorBrush(Color.FromArgb(180, 32, 178, 170)); // lightseagreen
                case "Cer_BS": return new SolidColorBrush(Color.FromArgb(180, 128, 128, 0)); // olive
                case "Cer_EODS": return new SolidColorBrush(Color.FromArgb(180, 30, 144, 255)); // dodgerblue
                case "Cer_EOS": return new SolidColorBrush(Color.FromArgb(180, 0, 0, 139)); // 	darkblue
                case "Cer_NDS": return new SolidColorBrush(Color.FromArgb(180, 219, 112, 147)); // palevioletred
                case "Cer_NS": return new SolidColorBrush(Color.FromArgb(180, 220, 20, 60)); // crimson
                case "Cer_NP": return new SolidColorBrush(Color.FromArgb(180, 255, 182, 193)); // lightpink
                case "Cer_AP": return new SolidColorBrush(Color.FromArgb(180, 178, 34, 34)); // firebrick
                case "Cer_OS": return new SolidColorBrush(Color.FromArgb(180, 228, 122, 224)); // orchid
                case "Cer_HS": return new SolidColorBrush(Color.FromArgb(180, 218, 112, 214)); // orchid modified
                case "Cer_HDS": return new SolidColorBrush(Color.FromArgb(180, 208, 102, 204)); // orchid modified
                case "Cer_EBDS": return new SolidColorBrush(Color.FromArgb(180, 10, 10, 129)); // 	darkblue modified
                case "HexCer_HS": return new SolidColorBrush(Color.FromArgb(180, 238, 132, 234)); // orchid modified
                case "HexCer_HDS": return new SolidColorBrush(Color.FromArgb(180, 248, 142, 244)); // orchid modified
                case "HexCer_AP": return new SolidColorBrush(Color.FromArgb(180, 128, 0, 0)); // maroon
                case "HexCer_NS": return new SolidColorBrush(Color.FromArgb(180, 148, 0, 47)); //wine red
                case "HexHexCer": return new SolidColorBrush(Color.FromArgb(180, 102, 0, 51)); //wine red
                case "HexHexHexCer": return new SolidColorBrush(Color.FromArgb(180, 102, 0, 0)); //wine red
                case "Hex2Cer": return new SolidColorBrush(Color.FromArgb(180, 102, 0, 51)); //wine red
                case "Hex3Cer": return new SolidColorBrush(Color.FromArgb(180, 102, 0, 0)); //wine red
                case "HexCer_NDS": return new SolidColorBrush(Color.FromArgb(180, 158, 0, 47)); //wine red
                case "HexCer_EOS": return new SolidColorBrush(Color.FromArgb(180, 0, 0, 102)); // 	darkblue
                case "AHexCer": return new SolidColorBrush(Color.FromArgb(180, 10, 10, 92)); // 	darkblue modified
                case "SHexCer": return new SolidColorBrush(Color.FromArgb(180, 255, 29, 0)); //scarlet modified
                case "SL": return new SolidColorBrush(Color.FromArgb(180, 245, 39, 10)); //scarlet modified
                case "CoQ": return new SolidColorBrush(Color.FromArgb(180, 235, 49, 20)); //scarlet
                case "PI_Cer": return new SolidColorBrush(Color.FromArgb(180, 225, 30, 225)); // fuchsia modified
                case "PE_Cer": return new SolidColorBrush(Color.FromArgb(180, 118, 10, 118)); // purple modified
                case "MIPC": return new SolidColorBrush(Color.FromArgb(180, 225, 30, 205)); // fuchsia modified modified
                case "NAE": return new SolidColorBrush(Color.FromArgb(180, 245, 59, 10)); // orangered modified
                case "NAPhe": return new SolidColorBrush(Color.FromArgb(180, 235, 49, 20)); // orangered modified
                case "NATau": return new SolidColorBrush(Color.FromArgb(180, 225, 39, 30)); // orangered modified
                case "NAAO": return new SolidColorBrush(Color.FromArgb(180, 215, 29, 40)); // orangered
                case "NAGly": return new SolidColorBrush(Color.FromArgb(180, 235, 49, 20)); // orangered modified
                case "NAGlySer": return new SolidColorBrush(Color.FromArgb(180, 225, 39, 30)); // orangered modified
                case "NAOrn": return new SolidColorBrush(Color.FromArgb(180, 215, 29, 40)); // orangered
                case "GM3": return new SolidColorBrush(Color.FromArgb(180, 168, 0, 47)); //wine red
                case "GD1a": return new SolidColorBrush(Color.FromArgb(180, 158, 0, 47)); //wine red modified
                case "GD1b": return new SolidColorBrush(Color.FromArgb(180, 148, 0, 47)); //wine red modified
                case "GD2": return new SolidColorBrush(Color.FromArgb(180, 138, 0, 47)); //wine red modified
                case "GD3": return new SolidColorBrush(Color.FromArgb(180, 128, 0, 47)); //wine red modified
                case "GM1": return new SolidColorBrush(Color.FromArgb(180, 118, 0, 47)); //wine red modified
                case "GQ1b": return new SolidColorBrush(Color.FromArgb(180, 108, 0, 47)); //wine red modified
                case "GT1b": return new SolidColorBrush(Color.FromArgb(180, 168, 10, 47)); //wine red modified
                case "NGcGM3": return new SolidColorBrush(Color.FromArgb(180, 168, 20, 47)); //wine red modified
                case "DGMG": return new SolidColorBrush(Color.FromArgb(180, 129, 0, 0)); // darkred modified
                case "MGMG": return new SolidColorBrush(Color.FromArgb(180, 10, 0, 255)); // blue modified
                case "GPNAE": return new SolidColorBrush(Color.FromArgb(180, 119, 69, 19)); // saddlebrown modified
                case "ST": return new SolidColorBrush(Color.FromArgb(180, 210, 105, 30)); // chocolate
                case "Others": return new SolidColorBrush(Color.FromArgb(180, 220, 20, 60)); //wine red
                case "Unknown": return new SolidColorBrush(Color.FromArgb(180, 181, 181, 181)); //wine red 2
                default: return new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)); // if nothing, black
            }
        }

        public static PairwisePlotBean GetRtMzPairwisePlotAlignmentViewBean(AlignmentFileBean alignmentFileBean, 
            AlignmentResultBean alignmentResultBean, bool isColoredByCompoundClass = false, List<MspFormatCompoundInformationBean> mspDB = null)
        {
            ObservableCollection<double> xAxisRtDatapointCollection = new ObservableCollection<double>();
            ObservableCollection<double> yAxisMzDatapointCollection = new ObservableCollection<double>();
            ObservableCollection<SolidColorBrush> plotBrushCollection = new ObservableCollection<SolidColorBrush>();
            ObservableCollection<AlignmentPropertyBean> alignmentPropertyBeanCollection = new ObservableCollection<AlignmentPropertyBean>();

            string xAxisTitle = "Retention time [min]";
            string yAxisTitle = "m/z";
            string graphTitle = alignmentFileBean.FileName;

            var alignedSpots = alignmentResultBean.AlignmentPropertyBeanCollection;
            var param = alignmentResultBean.AnalysisParamForLC; if (param == null) param = new AnalysisParametersBean();

            for (int i = 0; i < alignedSpots.Count; i++)
            {
                xAxisRtDatapointCollection.Add(alignedSpots[i].CentralRetentionTime);
                yAxisMzDatapointCollection.Add(alignedSpots[i].CentralAccurateMass);

                if (param.TrackingIsotopeLabels == true) {
                    var brushes = getSolidColarBrushForCompoundClass(alignedSpots[i].IsotopeTrackingWeightNumber);
                    plotBrushCollection.Add(brushes);
                }
                else if (isColoredByCompoundClass == false || mspDB == null || mspDB.Count == 0) {
                   
                    plotBrushCollection.Add(new SolidColorBrush(Color.FromArgb(180
                    , (byte)(255 * alignedSpots[i].RelativeAmplitudeValue)
                    , (byte)(255 * (1 - Math.Abs(alignedSpots[i].RelativeAmplitudeValue - 0.5)))
                    , (byte)(255 - 255 * alignedSpots[i].RelativeAmplitudeValue))));

                }
                else {

                    //// temp
                    //var prop = alignedSpots[i];
                    //var isAnnotated = (prop.LibraryID >= 0 || prop.PostIdentificationLibraryID >= 0) 
                    //    && !prop.MetaboliteName.StartsWith("w/o MS2")
                    //    && !prop.MetaboliteName.StartsWith("RIKEN") ? true : false;
                    //var isZeroContain = false;
                    //foreach (var peakProp in prop.AlignedPeakPropertyBeanCollection) {
                    //    if (peakProp.RetentionTime < 0.25) {
                    //        isZeroContain = true;
                    //        break;
                    //    }
                    //}

                    var brushes = getSolidColarBrushForCompoundClass(alignedSpots[i].LibraryID, mspDB);
                    //if (isZeroContain && isAnnotated) {
                    //    brushes = Brushes.Red;
                    //}
                    //else {
                    //    brushes = Brushes.Black;
                    //}
                    plotBrushCollection.Add(brushes);
                }

                alignmentPropertyBeanCollection.Add(alignedSpots[i]);
            }

            return new PairwisePlotBean(graphTitle, xAxisTitle, yAxisTitle, 
                xAxisRtDatapointCollection, yAxisMzDatapointCollection, 
                alignmentPropertyBeanCollection, plotBrushCollection, PairwisePlotDisplayLabel.None);
        }

        public static PairwisePlotBean GetDriftTimeMzPairwisePlotAlignmentViewBean(AlignmentFileBean alignmentFileBean,
            AlignmentResultBean alignmentResultBean, int alignedSpotID, IonMobilityType mobilityType,
            out int repSpotID, bool isColoredByCompoundClass = false, List<MspFormatCompoundInformationBean> mspDB = null) {

            repSpotID = 0;

            var xAxisRtDatapointCollection = new ObservableCollection<double>();
            var yAxisMzDatapointCollection = new ObservableCollection<double>();
            var plotBrushCollection = new ObservableCollection<SolidColorBrush>();
            var alignmentPropertyBeanCollection = new ObservableCollection<AlignedDriftSpotPropertyBean>();

            string xAxisTitle = getDriftAxisTitle(mobilityType);
            string yAxisTitle = "m/z";
            string graphTitle = alignmentFileBean.FileName;

            var alignedSpots = alignmentResultBean.AlignmentPropertyBeanCollection;
            var targetSpot = alignedSpots[alignedSpotID];
            var param = alignmentResultBean.AnalysisParamForLC; if (param == null) param = new AnalysisParametersBean();
            var driftSpots = getDriftSpotsAtTargetSpotRange(alignedSpots, targetSpot, param.AccumulatedRtRagne, out repSpotID);
            if (driftSpots == null || driftSpots.Count() == 0) return null; 

            for (int i = 0; i < driftSpots.Count; i++) {
                xAxisRtDatapointCollection.Add(driftSpots[i].CentralDriftTime);
                yAxisMzDatapointCollection.Add(driftSpots[i].CentralAccurateMass);

                if (isColoredByCompoundClass == false || mspDB == null || mspDB.Count == 0) {

                    plotBrushCollection.Add(new SolidColorBrush(Color.FromArgb(180
                    , (byte)(255 * driftSpots[i].RelativeAmplitudeValue)
                    , (byte)(255 * (1 - Math.Abs(driftSpots[i].RelativeAmplitudeValue - 0.5)))
                    , (byte)(255 - 255 * driftSpots[i].RelativeAmplitudeValue))));

                }
                else {

                    var brushes = getSolidColarBrushForCompoundClass(driftSpots[i].LibraryID, mspDB);
                    plotBrushCollection.Add(brushes);
                }

                alignmentPropertyBeanCollection.Add(driftSpots[i]);
            }

            return new PairwisePlotBean(graphTitle, xAxisTitle, yAxisTitle,
                xAxisRtDatapointCollection, yAxisMzDatapointCollection,
                alignmentPropertyBeanCollection, plotBrushCollection, PairwisePlotDisplayLabel.None, targetSpot.CentralAccurateMass);
        }

        private static List<AlignedDriftSpotPropertyBean> getDriftSpotsAtTargetSpotRange(ObservableCollection<AlignmentPropertyBean> alignedSpots, 
            AlignmentPropertyBean targetSpot, float rtWidth, out int representativeSpotID) {
            representativeSpotID = -1;

            var driftSpots = new List<AlignedDriftSpotPropertyBean>();
            var targetMz = targetSpot.CentralAccurateMass;
            var targetMzWidth = 10;
            var targetRt = targetSpot.CentralRetentionTime;
            //var targetRtWidth = targetSpot.AveragePeakWidth;
            //if (targetRtWidth > 0.5) targetRtWidth = 0.5F;
            //rtWidth = 40.0F; //temp
            var maxIntensity = float.MinValue;
            var minIntensity = float.MaxValue;
            foreach (var alignedSpot in alignedSpots) {
                var mz = alignedSpot.CentralAccurateMass;
                var rt = alignedSpot.CentralRetentionTime;
                if (Math.Abs(rt - targetRt) < rtWidth * 0.5) {
                    foreach (var driftSpot in alignedSpot.AlignedDriftSpots.OrEmptyIfNull()) {
                        driftSpots.Add(driftSpot);
                        if (driftSpot.MaxValiable > maxIntensity) maxIntensity = driftSpot.MaxValiable;
                        if (driftSpot.MinValiable < minIntensity) minIntensity = driftSpot.MinValiable;
                    }
                }
            }
            if (driftSpots == null || driftSpots.Count == 0) return new List<AlignedDriftSpotPropertyBean>();

            driftSpots = driftSpots.OrderBy(n => n.MaxValiable).ToList();
            //var counter = 0;

            var maxIdentID = -1;
            var maxIdentScore = 0.0;
            var maxIntensityID = -1;
            var maxInt = 0.0;
            var maxIntensityWithMsMsID = -1;
            var maxIntensityWithMsMs = 0.0;

            for (int i = 0; i < driftSpots.Count; i++) {
                var spot = driftSpots[i];
                spot.RelativeAmplitudeValue = spot.MaxValiable / maxIntensity;
                spot.DisplayedSpotID = i;

                if (spot.AlignmentSpotID == targetSpot.AlignmentID) {
                    if (representativeSpotID == -1) // just initialize
                        representativeSpotID = spot.DisplayedSpotID;
                    if (spot.MsmsIncluded) {
                        if (spot.LibraryID >= 0) {
                            if (spot.MetaboliteName.Contains("w/o")) {
                                if (maxIntensityWithMsMsID == -1) {
                                    maxIntensityWithMsMs = spot.MaxValiable;
                                    maxIntensityWithMsMsID = i;
                                }
                                else if (spot.MaxValiable > maxIntensityWithMsMs) {
                                    maxIntensityWithMsMs = spot.MaxValiable;
                                    maxIntensityWithMsMsID = i;
                                }
                            }
                            else {
                                if (maxIdentID == -1) {
                                    maxIdentScore = spot.TotalSimilairty;
                                    maxIdentID = i;
                                }
                                else if (maxIdentScore < spot.TotalSimilairty) {
                                    maxIdentScore = spot.TotalSimilairty;
                                    maxIdentID = i;
                                }
                            }
                        }
                        else {
                            if (maxIntensityWithMsMsID == -1) {
                                maxIntensityWithMsMs = spot.MaxValiable;
                                maxIntensityWithMsMsID = i;
                            }
                            else if (spot.MaxValiable > maxIntensityWithMsMs) {
                                maxIntensityWithMsMs = spot.MaxValiable;
                                maxIntensityWithMsMsID = i;
                            }
                        }
                    }
                    else {
                        if (maxIntensityID == -1) {
                            maxInt = spot.MaxValiable;
                            maxIntensityID = i;
                        }
                        else if (spot.MaxValiable > maxInt) {
                            maxInt = spot.MaxValiable;
                            maxIntensityID = i;
                        }
                    }
                }
            }

            if (maxIdentID >= 0) {
                representativeSpotID = maxIdentID;
            }
            else if (maxIntensityWithMsMsID >= 0) {
                representativeSpotID = maxIntensityWithMsMsID;
            }
            else {
                representativeSpotID = maxIntensityID;
            }

            if (representativeSpotID < 0) representativeSpotID = 0;
            return driftSpots;
        }

        private static SolidColorBrush getSolidColarBrushForCompoundClass(int isotopeNumber)
        {
            if (isotopeNumber == 0) return new SolidColorBrush(Color.FromArgb(180, (byte)0, (byte)0, (byte)255));
            else if (isotopeNumber >= 1 && isotopeNumber <= 5) return new SolidColorBrush(Color.FromArgb(180, (byte)(isotopeNumber * 50.0), (byte)0, (byte)(255 - isotopeNumber * 50.0)));
            else return new SolidColorBrush(Color.FromArgb(180, (byte)255, (byte)0, (byte)0));
        }

        public static ChromatogramXicViewModel GetChromatogramXicViewModel(ObservableCollection<RawSpectrum> accumulatedSpectra, ObservableCollection<RawSpectrum> spectrumCollection,
            PeakAreaBean peakAreaBean, AnalysisParametersBean param, AnalysisFilePropertyBean file,
            ProjectPropertyBean project)
        {
            var targetMz = peakAreaBean.AccurateMass;
            var targetRt = peakAreaBean.RtAtPeakTop;
            List<double[]> peaklist = null;

            if (param.IsIonMobility) {
                peaklist = DataAccessLcUtility.GetSmoothedPeaklist(DataAccessLcUtility.GetMs1Peaklist(accumulatedSpectra, project, targetMz, param.CentroidMs1Tolerance,
                    param.RetentionTimeBegin, param.RetentionTimeEnd), param.SmoothingMethod, param.SmoothingLevel);
            }
            else {
                peaklist = DataAccessLcUtility.GetSmoothedPeaklist(DataAccessLcUtility.GetMs1Peaklist(spectrumCollection, project, targetMz, param.CentroidMs1Tolerance,
                    param.RetentionTimeBegin, param.RetentionTimeEnd), param.SmoothingMethod, param.SmoothingLevel);
            }
           
           // var peaklist = DataAccessLcUtility.GetMs1Peaklist(spectrumCollection, projectPropertyBean, targetMz, analysisParametersBean.CentroidMs1Tolerance, analysisParametersBean.RetentionTimeBegin, analysisParametersBean.RetentionTimeEnd);
            var chromatogramBean = new ChromatogramBean(true, Brushes.Blue, 1, file.AnalysisFileName, targetMz, new ObservableCollection<double[]>(peaklist), new ObservableCollection<PeakAreaBean>() { peakAreaBean });

            string graphTitle = "EIC chromatogram of " + Math.Round(targetMz, 4).ToString() + " tolerance [Da]: " + param.CentroidMs1Tolerance.ToString() + "  Max intensity: " + Math.Round(chromatogramBean.MaxIntensity, 0);
            // original
            // return new ChromatogramXicViewModel(chromatogramBean, ChromatogramEditMode.Display, ChromatogramDisplayLabel.None, ChromatogramQuantitativeMode.Height, ChromatogramIntensityMode.Relative, 0, graphTitle, targetMz, analysisParametersBean.CentroidMs1Tolerance, targetRt, true);

            // fill peak area
            return new ChromatogramXicViewModel(chromatogramBean, ChromatogramEditMode.Display, ChromatogramDisplayLabel.None, ChromatogramQuantitativeMode.Height, ChromatogramIntensityMode.Relative,
                0, graphTitle, targetMz, param.CentroidMs1Tolerance, targetRt, peakAreaBean.RtAtLeftPeakEdge, peakAreaBean.RtAtRightPeakEdge, true);
        }

        public static ChromatogramXicViewModel GetChromatogramXicViewModel(ObservableCollection<RawSpectrum> spectrumCollection, 
            DriftSpotBean driftSpot, ObservableCollection<PeakAreaBean> peakSpots, AnalysisParametersBean param,
            AnalysisFilePropertyBean fileProp, ProjectPropertyBean projectProperty) {

            var parentPeak = peakSpots[driftSpot.PeakAreaBeanID];
            var scanID = parentPeak.Ms1LevelDatapointNumber;
            var rt = parentPeak.RtAtPeakTop;
            var rtWidth = parentPeak.RtAtRightPeakEdge - parentPeak.RtAtLeftPeakEdge;
            if (rtWidth > 0.6) rtWidth = 0.6F;
            if (rtWidth < 0.2) rtWidth = 0.2F;
            var mz = parentPeak.AccurateMass;
            var mztol = param.CentroidMs1Tolerance;
            var xTitle = getDriftAxisTitle(param.IonMobilityType);

            var peaklist = DataAccessLcUtility.GetDriftChromatogramByScanRtMz(spectrumCollection, scanID, rt, param.AccumulatedRtRagne, mz, mztol);
            //var peaklist = DataAccessLcUtility.GetDriftChromatogramByScanRtMz(spectrumCollection, scanID, rt, rtWidth, mz, mztol);
            var smoothedPeaklist = DataAccessLcUtility.GetSmoothedPeaklist(peaklist, param.SmoothingMethod, param.SmoothingLevel);
            var chromatogramBean = new ChromatogramBean(true, Brushes.Blue, 1, fileProp.AnalysisFileName, mz, new ObservableCollection<double[]>(smoothedPeaklist), new ObservableCollection<DriftSpotBean>() { driftSpot });

            string graphTitle = "Drift chromatogram of RT " + Math.Round(rt, 2).ToString() + " within " + Math.Round(rtWidth, 3).ToString() + " tolerance [min] and m/z"
                + Math.Round(mz, 4).ToString() + " tolerance [Da]: " + param.CentroidMs1Tolerance.ToString();

            return new ChromatogramXicViewModel(chromatogramBean, ChromatogramEditMode.Display, ChromatogramDisplayLabel.None, 
                ChromatogramQuantitativeMode.Height, ChromatogramIntensityMode.Relative, 0, graphTitle, 
                mz, param.CentroidMs1Tolerance, driftSpot.DriftTimeAtPeakTop,
                driftSpot.DriftTimeAtLeftPeakEdge, driftSpot.DriftTimeAtRightPeakEdge, true, xTitle);
        }

        public static MassSpectrogramViewModel GetMs1MassSpectrogramViewModel(ObservableCollection<RawSpectrum> accumulatedMs1Spectra, // this is used for ion mobility data access. In conventional LC-MS data processing, this object should be null.
            ObservableCollection<RawSpectrum> spectrumCollection, PeakAreaBean peakAreaBean, 
            AnalysisParametersBean param, ProjectPropertyBean project)
        {
            float targetRt = peakAreaBean.RtAtPeakTop;
            float targetMz = peakAreaBean.AccurateMass;
            int msScanPoint = peakAreaBean.Ms1LevelDatapointNumber;

            if (msScanPoint < 0) return null;
            MassSpectrogramBean massSpec = null;
            if (param.IsIonMobility) {
                msScanPoint = peakAreaBean.Ms1LevelDatapointNumberAtAcculateMs1;
                massSpec = getAccumulatedMs1Spectrum(accumulatedMs1Spectra, msScanPoint, param.CentroidMs1Tolerance,
                            param.PeakDetectionBasedCentroid,
                            project.DataType);
            }
            else {
                massSpec = getMassSpectrogramBean(spectrumCollection, msScanPoint, param.CentroidMs1Tolerance,
                            param.PeakDetectionBasedCentroid, 
                            //param.IsIonMobility,
                            project.DataType);
            }
           
            if (massSpec == null) return null;
            string graphTitle = "MS1 spectra " + "Max intensity: " + 
                Math.Round(massSpec.MaxIntensity, 0).ToString() + "\n" + massSpec.SplashKey;

            return new MassSpectrogramViewModel(massSpec, MassSpectrogramIntensityMode.Relative, msScanPoint, targetRt, targetMz, graphTitle);
        }

        public static MassSpectrogramViewModel GetMs2MassspectrogramViewModel(ObservableCollection<RawSpectrum> spectrumCollection, 
            PeakAreaBean peakAreaBean, AnalysisParametersBean analysisParametersBean, MS2DecResult deconvolutionResultBean, 
            ReversibleMassSpectraView reversibleMassSpectraView, ProjectPropertyBean projectPropertyBean, 
            List<MspFormatCompoundInformationBean> mspFormatCompoundInformationBeanList)
        {
            float targetRt = peakAreaBean.RtAtPeakTop;
            int msScanPoint = peakAreaBean.Ms2LevelDatapointNumber;

            if (msScanPoint == -1) return null;

            MassSpectrogramBean referenceSpectraBean;
            MassSpectrogramBean massSpectrogramBean;

            if (reversibleMassSpectraView == ReversibleMassSpectraView.raw)
                massSpectrogramBean = getMassSpectrogramBean(spectrumCollection, msScanPoint, analysisParametersBean.CentroidMs2Tolerance,
                    analysisParametersBean.PeakDetectionBasedCentroid, 
                    //analysisParametersBean.IsIonMobility,
                    projectPropertyBean.DataType);
            else
                massSpectrogramBean = getMassSpectrogramBean(deconvolutionResultBean, Brushes.Black);

            string graphTitle = "MS2 spectra " + "Precursor: " + Math.Round(peakAreaBean.AccurateMass, 5).ToString();

            if (mspFormatCompoundInformationBeanList != null && mspFormatCompoundInformationBeanList.Count != 0)
                referenceSpectraBean = getReferenceSpectra(mspFormatCompoundInformationBeanList, peakAreaBean.LibraryID, Brushes.Red);
            else
                referenceSpectraBean = new MassSpectrogramBean(Brushes.Red, 1.0, null);

            return new MassSpectrogramViewModel(massSpectrogramBean, referenceSpectraBean, MassSpectrogramIntensityMode.Relative, msScanPoint, targetRt, graphTitle);
        }

        public static MassSpectrogramViewModel GetMs2MassspectrogramViewModel(ObservableCollection<RawSpectrum> spectrumCollection,
            DriftSpotBean driftSpot, AnalysisParametersBean analysisParametersBean, MS2DecResult deconvolutionResultBean,
            ReversibleMassSpectraView reversibleMassSpectraView, ProjectPropertyBean projectPropertyBean,
            List<MspFormatCompoundInformationBean> mspFormatCompoundInformationBeanList)
        {
            float targetRt = driftSpot.DriftTimeAtPeakTop;
            int msScanPoint = driftSpot.Ms2LevelDatapointNumber;

            if (msScanPoint == -1) return null;

            MassSpectrogramBean referenceSpectraBean;
            MassSpectrogramBean massSpectrogramBean;

            if (reversibleMassSpectraView == ReversibleMassSpectraView.raw)
                massSpectrogramBean = getMassSpectrogramBean(spectrumCollection, msScanPoint, analysisParametersBean.CentroidMs2Tolerance,
                    analysisParametersBean.PeakDetectionBasedCentroid,
                    //analysisParametersBean.IsIonMobility,
                    projectPropertyBean.DataType);
            else
                massSpectrogramBean = getMassSpectrogramBean(deconvolutionResultBean, Brushes.Black);

            string graphTitle = "MS2 spectra " + "Precursor: " + Math.Round(driftSpot.AccurateMass, 5).ToString();

            if (mspFormatCompoundInformationBeanList != null && mspFormatCompoundInformationBeanList.Count != 0)
                referenceSpectraBean = getReferenceSpectra(mspFormatCompoundInformationBeanList, driftSpot.LibraryID, Brushes.Red);
            else
                referenceSpectraBean = new MassSpectrogramBean(Brushes.Red, 1.0, null);

            return new MassSpectrogramViewModel(massSpectrogramBean, referenceSpectraBean, MassSpectrogramIntensityMode.Relative, msScanPoint, targetRt, graphTitle);
        }


        public static MassSpectrogramViewModel GetMs2MassspectrogramViewModel(ObservableCollection<RawSpectrum> spectrumCollection,
            DriftSpotBean driftSpot, PeakAreaBean peakAreaBean,AnalysisParametersBean param, ProjectPropertyBean project, MS2DecResult deconvolutionResultBean,
            ReversibleMassSpectraView reversibleMassSpectraView,
            List<MspFormatCompoundInformationBean> mspFormatCompoundInformationBeanList)
        {
            float targetRt = driftSpot.DriftTimeAtPeakTop;
            int msScanPoint = driftSpot.Ms2LevelDatapointNumber;

            if (msScanPoint == -1) return null;

            MassSpectrogramBean referenceSpectraBean;
            MassSpectrogramBean massSpectrogramBean;

            if(project.MethodType == MethodType.ddMSMS)
            {
                return GetMs2MassspectrogramViewModel(spectrumCollection, driftSpot, param,
                    deconvolutionResultBean, ReversibleMassSpectraView.raw, project, mspFormatCompoundInformationBeanList);
            }

            if (reversibleMassSpectraView == ReversibleMassSpectraView.raw)
                return GetMs2RawMassspectrogramWithRefViewModel(spectrumCollection, driftSpot, peakAreaBean, mspFormatCompoundInformationBeanList, param, project);

            massSpectrogramBean = getMassSpectrogramBean(deconvolutionResultBean, Brushes.Black);
            string graphTitle = "MS2 spectra " + "Precursor: " + Math.Round(driftSpot.AccurateMass, 5).ToString();

            if (mspFormatCompoundInformationBeanList != null && mspFormatCompoundInformationBeanList.Count != 0)
                referenceSpectraBean = getReferenceSpectra(mspFormatCompoundInformationBeanList, driftSpot.LibraryID, Brushes.Red);
            else
                referenceSpectraBean = new MassSpectrogramBean(Brushes.Red, 1.0, null);

            return new MassSpectrogramViewModel(massSpectrogramBean, referenceSpectraBean, MassSpectrogramIntensityMode.Relative, msScanPoint, targetRt, graphTitle);
        }

        public static MassSpectrogramViewModel GetMs2MassspectrogramViewModel(AlignmentPropertyBean alignmentPropertyBean, 
            MS2DecResult deconvolutionResultBean, List<MspFormatCompoundInformationBean> mspFormatCompoundInformationBeanList)
        {
            float targetRt = alignmentPropertyBean.CentralRetentionTime;
            int msScanPoint = alignmentPropertyBean.AlignedPeakPropertyBeanCollection[alignmentPropertyBean.RepresentativeFileID].Ms2ScanNumber;

            if (msScanPoint == -1) return null;

            MassSpectrogramBean referenceSpectraBean;
            MassSpectrogramBean massSpectrogramBean = getMassSpectrogramBean(deconvolutionResultBean, Brushes.Blue);
            massSpectrogramBean.DisplayBrush = Brushes.Blue;

            string graphTitle = "MS2 spectra " + "Precursor: " + Math.Round(deconvolutionResultBean.Ms1AccurateMass, 5).ToString();
            if (mspFormatCompoundInformationBeanList != null && mspFormatCompoundInformationBeanList.Count != 0)
                referenceSpectraBean = getReferenceSpectra(mspFormatCompoundInformationBeanList, alignmentPropertyBean.LibraryID, Brushes.Red);
            else
                referenceSpectraBean = new MassSpectrogramBean(Brushes.Red, 1.0, null);

            return new MassSpectrogramViewModel(massSpectrogramBean, referenceSpectraBean, MassSpectrogramIntensityMode.Relative, msScanPoint, targetRt, graphTitle);
        }

        public static MassSpectrogramViewModel GetMs2MassspectrogramViewModel(AlignedDriftSpotPropertyBean alignmentPropertyBean,
            MS2DecResult deconvolutionResultBean, List<MspFormatCompoundInformationBean> mspFormatCompoundInformationBeanList) {
            float targetRt = alignmentPropertyBean.CentralDriftTime;
            int msScanPoint = alignmentPropertyBean.AlignedPeakPropertyBeanCollection[alignmentPropertyBean.RepresentativeFileID].Ms2ScanNumber;

            if (msScanPoint == -1) return null;

            MassSpectrogramBean referenceSpectraBean;
            MassSpectrogramBean massSpectrogramBean = getMassSpectrogramBean(deconvolutionResultBean, Brushes.Blue);
            massSpectrogramBean.DisplayBrush = Brushes.Blue;

            string graphTitle = "MS2 spectra " + "Precursor: " + Math.Round(deconvolutionResultBean.Ms1AccurateMass, 5).ToString();
            if (mspFormatCompoundInformationBeanList != null && mspFormatCompoundInformationBeanList.Count != 0)
                referenceSpectraBean = getReferenceSpectra(mspFormatCompoundInformationBeanList, alignmentPropertyBean.LibraryID, Brushes.Red);
            else
                referenceSpectraBean = new MassSpectrogramBean(Brushes.Red, 1.0, null);

            return new MassSpectrogramViewModel(massSpectrogramBean, referenceSpectraBean, MassSpectrogramIntensityMode.Relative, msScanPoint, targetRt, graphTitle);
        }

        public static MassSpectrogramViewModel GetMs2MassspectrogramViewModel(PeakAreaBean peakSpot, 
            MS2DecResult ms2DecResult, List<MspFormatCompoundInformationBean> mspDB)
        {
            float targetRt = peakSpot.RtAtPeakTop;
            int msScanPoint = peakSpot.Ms2LevelDatapointNumber;
            if (msScanPoint == -1) return null;

            MassSpectrogramBean referenceSpectraBean = null;
            MassSpectrogramBean massSpectrogramBean = getMassSpectrogramBean(ms2DecResult, Brushes.Blue);

            string graphTitle = "MS2 spectra " + "Precursor: " + Math.Round(peakSpot.AccurateMass, 5).ToString();
                                

            if (mspDB != null && mspDB.Count != 0)
                referenceSpectraBean = getReferenceSpectra(mspDB, peakSpot.LibraryID, Brushes.Red);
            else
                referenceSpectraBean = new MassSpectrogramBean(Brushes.Red, 1.0, null);

            return new MassSpectrogramViewModel(massSpectrogramBean, referenceSpectraBean, MassSpectrogramIntensityMode.Relative, msScanPoint, targetRt, graphTitle);
        }

        public static MassSpectrogramViewModel GetMs2MassspectrogramViewModel(DriftSpotBean driftSpot,
            MS2DecResult ms2DecResult, List<MspFormatCompoundInformationBean> mspDB, bool isDDA = false)
        {
            float targetRt = driftSpot.DriftTimeAtPeakTop;
            int msScanPoint = driftSpot.Ms2LevelDatapointNumber;            
            if (isDDA && msScanPoint == -1) return null;

            MassSpectrogramBean referenceSpectraBean = null;
            MassSpectrogramBean massSpectrogramBean = getMassSpectrogramBean(ms2DecResult, Brushes.Blue);

            string graphTitle = "MS2 spectra " + "Precursor: " + Math.Round(driftSpot.AccurateMass, 5).ToString();


            if (mspDB != null && mspDB.Count != 0)
                referenceSpectraBean = getReferenceSpectra(mspDB, driftSpot.LibraryID, Brushes.Red);
            else
                referenceSpectraBean = new MassSpectrogramBean(Brushes.Red, 1.0, null);

            return new MassSpectrogramViewModel(massSpectrogramBean, referenceSpectraBean, MassSpectrogramIntensityMode.Relative, msScanPoint, targetRt, graphTitle);
            
        }

        public static MassSpectrogramBean GetMs2RawMassspectrogram(ObservableCollection<RawSpectrum> lcmsSpectrumCollection,
            DriftSpotBean driftSpot, PeakAreaBean peakSpot,AnalysisParametersBean param, ProjectPropertyBean project)
        {
            var centroidedSpectra = DataAccessLcUtility.GetAccumulatedMs2Spectra(lcmsSpectrumCollection, driftSpot, peakSpot, param, project);
            if (centroidedSpectra.Count == 0) return null;
            ObservableCollection<MassSpectrogramDisplayLabel> massSpectraDisplayLabelCollection = new ObservableCollection<MassSpectrogramDisplayLabel>();

            foreach (var s in centroidedSpectra)
            {
                massSpectraDisplayLabelCollection.Add(new MassSpectrogramDisplayLabel() { Mass = s[0], Intensity = s[1], Label = Math.Round(s[0], 4).ToString() });
            }

            MassSpectrogramBean massSpectrogramBean = null;

            if (centroidedSpectra.Count > 0)
                massSpectrogramBean = new MassSpectrogramBean(Brushes.Blue, 1.0, new ObservableCollection<double[]>(centroidedSpectra), massSpectraDisplayLabelCollection);
            return massSpectrogramBean;
        }


        public static MassSpectrogramViewModel GetMs2RawMassspectrogramWithRefViewModel(ObservableCollection<RawSpectrum> lcmsSpectrumCollection,
            DriftSpotBean driftSpot, PeakAreaBean peakSpot,
            List<MspFormatCompoundInformationBean> mspDB, AnalysisParametersBean param, ProjectPropertyBean project)
        {
            MassSpectrogramBean referenceSpectraBean = null;
            MassSpectrogramBean massSpectrogramBean = GetMs2RawMassspectrogram(lcmsSpectrumCollection, driftSpot, peakSpot, param, project);

            if (mspDB != null && mspDB.Count != 0)
                referenceSpectraBean = getReferenceSpectra(mspDB, driftSpot.LibraryID, Brushes.Red);
            else
                referenceSpectraBean = new MassSpectrogramBean(Brushes.Red, 1.0, null);

            float targetRt = driftSpot.DriftTimeAtPeakTop;
            string graphTitle = "MS2 spectra " + "Precursor: " + Math.Round(driftSpot.AccurateMass, 5).ToString();

            return new MassSpectrogramViewModel(massSpectrogramBean, referenceSpectraBean, MassSpectrogramIntensityMode.Relative, driftSpot.Ms1LevelDatapointNumber, targetRt, graphTitle);
            
        }

        public static MassSpectrogramViewModel GetMs2RawMassspectrogramWithAccumulateViewModel(ObservableCollection<RawSpectrum> lcmsSpectrumCollection, 
            DriftSpotBean driftSpot, PeakAreaBean peakSpot, AnalysisParametersBean param, ProjectPropertyBean project)
        {
            MassSpectrogramBean massSpectrogramBean = GetMs2RawMassspectrogram(lcmsSpectrumCollection, driftSpot, peakSpot, param, project);
            if (massSpectrogramBean == null) return null;

            string graphTitle = "Raw MS/MS spectrum" + "\n" + massSpectrogramBean.SplashKey;
            float targetRt = driftSpot.DriftTimeAtPeakTop;

            return new MassSpectrogramViewModel(massSpectrogramBean, null, MassSpectrogramIntensityMode.Absolute, driftSpot.Ms1LevelDatapointNumber, targetRt, graphTitle);
        }


        public static MassSpectrogramViewModel GetMs2RawMassspectrogramViewModel(ObservableCollection<RawSpectrum> spectrumCollection,
           DriftSpotBean driftSpot, AnalysisParametersBean analysisParametersBean, ProjectPropertyBean projectPropertyBean) {
            float targetRt = driftSpot.DriftTimeAtPeakTop;
            int msScanPoint = driftSpot.Ms2LevelDatapointNumber;

            if (msScanPoint == -1) return null;

            var massSpectrogramBean = getMassSpectrogramBean(spectrumCollection, msScanPoint, analysisParametersBean.CentroidMs2Tolerance,
                analysisParametersBean.PeakDetectionBasedCentroid,
                //analysisParametersBean.IsIonMobility,
                projectPropertyBean.DataType);

            if (massSpectrogramBean == null) return null;

            string graphTitle = "Raw MS/MS spectrum" + "\n" + massSpectrogramBean.SplashKey;

            return new MassSpectrogramViewModel(massSpectrogramBean, null, MassSpectrogramIntensityMode.Absolute, msScanPoint, targetRt, graphTitle);
        }

        public static MassSpectrogramViewModel GetMs2RawMassspectrogramViewModel(ObservableCollection<RawSpectrum> spectrumCollection, 
            PeakAreaBean peakAreaBean, AnalysisParametersBean analysisParametersBean, ProjectPropertyBean projectPropertyBean)
        {
            float targetRt = peakAreaBean.RtAtPeakTop;
            int msScanPoint = peakAreaBean.Ms2LevelDatapointNumber;

            if (msScanPoint == -1) return null;

            MassSpectrogramBean massSpectrogramBean = getMassSpectrogramBean(spectrumCollection, msScanPoint, analysisParametersBean.CentroidMs2Tolerance, 
                analysisParametersBean.PeakDetectionBasedCentroid, 
                //analysisParametersBean.IsIonMobility,
                projectPropertyBean.DataType);

            if (massSpectrogramBean == null) return null;

            string graphTitle = "Raw MS/MS spectrum" + "\n" + massSpectrogramBean.SplashKey;

            return new MassSpectrogramViewModel(massSpectrogramBean, null, MassSpectrogramIntensityMode.Absolute, msScanPoint, targetRt, graphTitle);
        }

        public static MassSpectrogramViewModel GetMs2DeconvolutedMassspectrogramViewModel(PeakAreaBean peakAreaBean, 
            MS2DecResult deconvolutionResultBean)
        {
            float targetRt = peakAreaBean.RtAtPeakTop;
            int msScanPoint = peakAreaBean.Ms2LevelDatapointNumber;

            if (msScanPoint == -1) return null;

            MassSpectrogramBean massSpectrogramBean = getMassSpectrogramBean(deconvolutionResultBean, Brushes.Black);

            if (massSpectrogramBean == null) return null;

            string graphTitle = "Deconvoluted MS/MS spectrum" + "\n" + massSpectrogramBean.SplashKey; 

            return new MassSpectrogramViewModel(massSpectrogramBean, null, MassSpectrogramIntensityMode.Absolute, msScanPoint, targetRt, graphTitle);
        }

        public static MassSpectrogramViewModel GetMs2DeconvolutedMassspectrogramViewModel(DriftSpotBean driftSpot,
            MS2DecResult deconvolutionResultBean) {
            float targetRt = driftSpot.DriftTimeAtPeakTop;
            int msScanPoint = driftSpot.Ms2LevelDatapointNumber;

            if (msScanPoint == -1) return null;

            MassSpectrogramBean massSpectrogramBean = getMassSpectrogramBean(deconvolutionResultBean, Brushes.Black);

            if (massSpectrogramBean == null) return null;

            string graphTitle = "Deconvoluted MS/MS spectrum" + "\n" + massSpectrogramBean.SplashKey;

            return new MassSpectrogramViewModel(massSpectrogramBean, null, MassSpectrogramIntensityMode.Absolute, msScanPoint, targetRt, graphTitle);
        }

        public static ChromatogramTicEicViewModel GetChromatogramTicViewModel(ObservableCollection<RawSpectrum> RAW_SpectrumCollection, 
            AnalysisFileBean analysisFileBean, ProjectPropertyBean projectPropertyBean, AnalysisParametersBean analysisParametersBean)
        {
            if (RAW_SpectrumCollection == null || RAW_SpectrumCollection.Count == 0) return null;

            ObservableCollection<ChromatogramBean> chromatogramBeanCollection = new ObservableCollection<ChromatogramBean>();
            List<double[]> peaklist = DataAccessLcUtility.GetTicPeaklist(RAW_SpectrumCollection, projectPropertyBean);

            if (peaklist == null || peaklist.Count == 0) return null;

            peaklist = DataAccessLcUtility.GetSmoothedPeaklist(peaklist, analysisParametersBean.SmoothingMethod, analysisParametersBean.SmoothingLevel);
            chromatogramBeanCollection.Add(new ChromatogramBean(true, Brushes.Black, "TIC", new ObservableCollection<double[]>(peaklist), null));

            return new ChromatogramTicEicViewModel(chromatogramBeanCollection, ChromatogramEditMode.Display, ChromatogramDisplayLabel.None, ChromatogramQuantitativeMode.Height, ChromatogramIntensityMode.Absolute, "TIC", analysisFileBean.AnalysisFilePropertyBean.AnalysisFileAnalyticalOrder, analysisFileBean.AnalysisFilePropertyBean.AnalysisFileType.ToString(), analysisFileBean.AnalysisFilePropertyBean.AnalysisFileClass, analysisFileBean.AnalysisFilePropertyBean.AnalysisFileName, analysisFileBean.AnalysisFilePropertyBean.AnalysisFileId);
        }

        public static ChromatogramTicEicViewModel GetChromatogramEicViewModel(ObservableCollection<ExtractedIonChromatogramDisplaySettingBean> extractedIonChromatogramDisplaySettingBeanCollection, 
            ProjectPropertyBean projectPropertyBean, AnalysisFileBean analysisFileBean,
            AnalysisParametersBean analysisParametersBean, List<SolidColorBrush> solidColorBrushList, 
            ObservableCollection<RawSpectrum> RAW_SpectrumCollection)
        {
            if (RAW_SpectrumCollection == null || RAW_SpectrumCollection.Count == 0) return null;

            ObservableCollection<ChromatogramBean> chromatogramBeanCollection = new ObservableCollection<ChromatogramBean>();
            ChromatogramBean chromatogramBean;

            List<double[]> peaklist;
            for (int i = 0; i < extractedIonChromatogramDisplaySettingBeanCollection.Count; i++)
            {
                peaklist = DataAccessLcUtility.GetMs1Peaklist(RAW_SpectrumCollection, projectPropertyBean, (float)extractedIonChromatogramDisplaySettingBeanCollection[i].ExactMass, (float)extractedIonChromatogramDisplaySettingBeanCollection[i].MassTolerance, float.MinValue, float.MaxValue);
                if (peaklist == null || peaklist.Count == 0) continue;
                peaklist = DataAccessLcUtility.GetSmoothedPeaklist(peaklist, analysisParametersBean.SmoothingMethod, analysisParametersBean.SmoothingLevel);

                if (i <= solidColorBrushList.Count - 1)
                    chromatogramBean = new ChromatogramBean(true, solidColorBrushList[i], 1.0, extractedIonChromatogramDisplaySettingBeanCollection[i].EicName, (float)extractedIonChromatogramDisplaySettingBeanCollection[i].ExactMass, (float)extractedIonChromatogramDisplaySettingBeanCollection[i].MassTolerance, new ObservableCollection<double[]>(peaklist));
                else
                    chromatogramBean = new ChromatogramBean(true, solidColorBrushList[0], 1.0, extractedIonChromatogramDisplaySettingBeanCollection[i].EicName, (float)extractedIonChromatogramDisplaySettingBeanCollection[i].ExactMass, (float)extractedIonChromatogramDisplaySettingBeanCollection[i].MassTolerance, new ObservableCollection<double[]>(peaklist));

                chromatogramBeanCollection.Add(chromatogramBean);
            }

            return new ChromatogramTicEicViewModel(chromatogramBeanCollection, ChromatogramEditMode.Display, ChromatogramDisplayLabel.None, ChromatogramQuantitativeMode.Height, ChromatogramIntensityMode.Absolute, "EIC", analysisFileBean.AnalysisFilePropertyBean.AnalysisFileAnalyticalOrder, analysisFileBean.AnalysisFilePropertyBean.AnalysisFileType.ToString(), analysisFileBean.AnalysisFilePropertyBean.AnalysisFileClass, analysisFileBean.AnalysisFilePropertyBean.AnalysisFileName, analysisFileBean.AnalysisFilePropertyBean.AnalysisFileId);
        }

        public static ChromatogramTicEicViewModel GetMultiFilesEicsOfTargetPeak(AlignmentPropertyBean alignedSpot, ObservableCollection<AnalysisFileBean> files, 
            int focusedFileID, ObservableCollection<RawSpectrum> focusedSpectra, ProjectPropertyBean projectPropety, RdamPropertyBean rdamProperty, 
            AnalysisParametersBean param)
        {
            if (focusedSpectra == null || focusedSpectra.Count == 0) return null;

            var chromatogramBeanCollection = new ObservableCollection<ChromatogramBean>();
            var rtBegin = alignedSpot.CentralRetentionTime - 0.5F;
            var rtEnd = alignedSpot.CentralRetentionTime + 0.5F;
            //var classIdColorDictionary = MsDialStatistics.GetClassIdColorDictionary(files, solidColorBrushList);
            var classnameToBytes = projectPropety.ClassnameToColorBytes;
            var classnameToBrushes = MsDialStatistics.ConvertToSolidBrushDictionary(classnameToBytes);

            foreach (var file in files.Where(n => n.AnalysisFilePropertyBean.AnalysisFileIncluded == true)) { // draw the included samples

                var fileProperty = file.AnalysisFilePropertyBean;
                var targetMz = alignedSpot.AlignedPeakPropertyBeanCollection[fileProperty.AnalysisFileId].AccurateMass;
                var peaklist = new List<double[]>();

                if (fileProperty.AnalysisFileId == focusedFileID) {
                    peaklist = DataAccessLcUtility.GetMs1Peaklist(focusedSpectra, projectPropety, targetMz, param.CentroidMs1Tolerance, rtBegin, rtEnd);
                }
                else {
                    peaklist = DataAccessLcUtility.GetChromatogramPeaklist(file, rtBegin, rtEnd, targetMz, param.CentroidMs1Tolerance, projectPropety.IonMode, rdamProperty);
                }

                if (peaklist == null || peaklist.Count == 0) continue;
                peaklist = DataAccessLcUtility.GetSmoothedPeaklist(peaklist, param.SmoothingMethod, param.SmoothingLevel);
                var chromatogramBean = new ChromatogramBean(true, classnameToBrushes[file.AnalysisFilePropertyBean.AnalysisFileClass], 1.0, fileProperty.AnalysisFileName, targetMz, param.CentroidMs1Tolerance, new ObservableCollection<double[]>(peaklist));

                chromatogramBeanCollection.Add(chromatogramBean);
            }
            return new ChromatogramTicEicViewModel(chromatogramBeanCollection, ChromatogramEditMode.Display, ChromatogramDisplayLabel.None, ChromatogramQuantitativeMode.Height, ChromatogramIntensityMode.Absolute, "EICs of Multi samples", -1, "Selected files", "Selected files", "Selected files", -1);
        }

        public static ChromatogramTicEicViewModel GetChromatogramBpcViewModel(
            ObservableCollection<ExtractedIonChromatogramDisplaySettingBean> extractedIonChromatogramDisplaySettingBeanCollection, 
            ProjectPropertyBean projectPropertyBean, AnalysisFileBean analysisFileBean, 
            AnalysisParametersBean analysisParametersBean, List<SolidColorBrush> solidColorBrushList, 
            ObservableCollection<RawSpectrum> RAW_SpectrumCollection)
        {
            if (RAW_SpectrumCollection == null || RAW_SpectrumCollection.Count == 0) return null;

            ObservableCollection<ChromatogramBean> chromatogramBeanCollection = new ObservableCollection<ChromatogramBean>();
            ChromatogramBean chromatogramBean;

            List<double[]> peaklist;
            for (int i = 0; i < extractedIonChromatogramDisplaySettingBeanCollection.Count; i++)
            {
                peaklist = DataAccessLcUtility.GetMs1PeaklistAsBPC(RAW_SpectrumCollection, projectPropertyBean, (float)extractedIonChromatogramDisplaySettingBeanCollection[i].ExactMass, (float)extractedIonChromatogramDisplaySettingBeanCollection[i].MassTolerance, float.MinValue, float.MaxValue);
                if (peaklist == null || peaklist.Count == 0) continue;
                peaklist = DataAccessLcUtility.GetSmoothedPeaklist(peaklist, analysisParametersBean.SmoothingMethod, analysisParametersBean.SmoothingLevel);

                if (i <= solidColorBrushList.Count - 1)
                    chromatogramBean = new ChromatogramBean(true, solidColorBrushList[i], 1.0, extractedIonChromatogramDisplaySettingBeanCollection[i].EicName, (float)extractedIonChromatogramDisplaySettingBeanCollection[i].ExactMass, (float)extractedIonChromatogramDisplaySettingBeanCollection[i].MassTolerance, new ObservableCollection<double[]>(peaklist));
                else
                    chromatogramBean = new ChromatogramBean(true, solidColorBrushList[0], 1.0, extractedIonChromatogramDisplaySettingBeanCollection[i].EicName, (float)extractedIonChromatogramDisplaySettingBeanCollection[i].ExactMass, (float)extractedIonChromatogramDisplaySettingBeanCollection[i].MassTolerance, new ObservableCollection<double[]>(peaklist));

                chromatogramBeanCollection.Add(chromatogramBean);
            }

            return new ChromatogramTicEicViewModel(chromatogramBeanCollection, ChromatogramEditMode.Display, ChromatogramDisplayLabel.None, ChromatogramQuantitativeMode.Height, ChromatogramIntensityMode.Absolute, "BPC", analysisFileBean.AnalysisFilePropertyBean.AnalysisFileAnalyticalOrder, analysisFileBean.AnalysisFilePropertyBean.AnalysisFileType.ToString(), analysisFileBean.AnalysisFilePropertyBean.AnalysisFileClass, analysisFileBean.AnalysisFilePropertyBean.AnalysisFileName, analysisFileBean.AnalysisFilePropertyBean.AnalysisFileId);
        }

        public static ChromatogramTicEicViewModel GetChromatogramBpcViewModel(
           ProjectPropertyBean project, AnalysisFileBean file,
           AnalysisParametersBean param,
           ObservableCollection<RawSpectrum> spectrumlist) {
            if (spectrumlist == null || spectrumlist.Count == 0) return null;

            var chromatogramBeanCollection = new ObservableCollection<ChromatogramBean>();
            var peaklist = DataAccessLcUtility.GetMs1PeaklistAsBPC(spectrumlist, project, param.CentroidMs1Tolerance);
            if (peaklist == null || peaklist.Count == 0) return null;
            peaklist = DataAccessLcUtility.GetSmoothedPeaklist(peaklist, param.SmoothingMethod, param.SmoothingLevel);

            chromatogramBeanCollection.Add(new ChromatogramBean(true, Brushes.Red, "BPC", 
                new ObservableCollection<double[]>(peaklist), null));
            return new ChromatogramTicEicViewModel(chromatogramBeanCollection,
                ChromatogramEditMode.Display, ChromatogramDisplayLabel.None, ChromatogramQuantitativeMode.Height,
                ChromatogramIntensityMode.Absolute, "TIC", file.AnalysisFilePropertyBean.AnalysisFileAnalyticalOrder,
                file.AnalysisFilePropertyBean.AnalysisFileType.ToString(),
                file.AnalysisFilePropertyBean.AnalysisFileClass,
                file.AnalysisFilePropertyBean.AnalysisFileName, 
                file.AnalysisFilePropertyBean.AnalysisFileId);
        }

        public static ChromatogramTicEicViewModel GetChromatogramTicBpcHighestEicViewModel(
          ProjectPropertyBean project, AnalysisFileBean file,
          AnalysisParametersBean param,
          ObservableCollection<RawSpectrum> spectrumlist) {
            if (spectrumlist == null || spectrumlist.Count == 0) return null;

            var peakspots = file.PeakAreaBeanCollection;
            if (peakspots == null || peakspots.Count == 0) return null;
            var chromatogramBeanCollection = new ObservableCollection<ChromatogramBean>();

            var bpcPeaklist = DataAccessLcUtility.GetMs1PeaklistAsBPC(spectrumlist, project, param.CentroidMs1Tolerance);
            if (bpcPeaklist == null || bpcPeaklist.Count == 0) return null;
            bpcPeaklist = DataAccessLcUtility.GetSmoothedPeaklist(bpcPeaklist, param.SmoothingMethod, param.SmoothingLevel);

            chromatogramBeanCollection.Add(new ChromatogramBean(true, Brushes.Red, 1.0, "BPC",
                0.0F, param.CentroidMs1Tolerance, new ObservableCollection<double[]>(bpcPeaklist)));

            var maxSpotID = 0;
            var maxIntensity = double.MinValue;
            for (int i = 0; i < peakspots.Count; i++) {
                if (peakspots[i].IntensityAtPeakTop > maxIntensity) {
                    maxIntensity = peakspots[i].IntensityAtPeakTop;
                    maxSpotID = i;
                }
            }
            var hSpot = peakspots[maxSpotID];

            var eicPeaklist = DataAccessLcUtility.GetMs1Peaklist(spectrumlist, project, (float)hSpot.AccurateMass,
                param.CentroidMs1Tolerance, float.MinValue, float.MaxValue);
            if (eicPeaklist == null || eicPeaklist.Count == 0) return null;
            eicPeaklist = DataAccessLcUtility.GetSmoothedPeaklist(eicPeaklist, param.SmoothingMethod, param.SmoothingLevel);
            var chromatogramBean = new ChromatogramBean(true, Brushes.Blue, 1.0, 
                "EIC",
                hSpot.AccurateMass, (float)param.CentroidMs1Tolerance, new ObservableCollection<double[]>(eicPeaklist));

            chromatogramBeanCollection.Add(chromatogramBean);

            var ticpeaklist = DataAccessLcUtility.GetTicPeaklist(spectrumlist, project);
            if (ticpeaklist == null || ticpeaklist.Count == 0) return null;

            ticpeaklist = DataAccessLcUtility.GetSmoothedPeaklist(ticpeaklist, param.SmoothingMethod, param.SmoothingLevel);
            chromatogramBeanCollection.Add(new ChromatogramBean(true, Brushes.Black, 1.0, "TIC", 
                0.0F, 0.0F,
                new ObservableCollection<double[]>(ticpeaklist)));

            return new ChromatogramTicEicViewModel(chromatogramBeanCollection,
                ChromatogramEditMode.Display, ChromatogramDisplayLabel.None, ChromatogramQuantitativeMode.Height,
                ChromatogramIntensityMode.Absolute, "TIC, BPC, and most abundant ion's EIC", 
                file.AnalysisFilePropertyBean.AnalysisFileAnalyticalOrder,
                file.AnalysisFilePropertyBean.AnalysisFileType.ToString(),
                file.AnalysisFilePropertyBean.AnalysisFileClass,
                file.AnalysisFilePropertyBean.AnalysisFileName,
                file.AnalysisFilePropertyBean.AnalysisFileId);
        }


        public static ChromatogramMrmViewModel GetMs2ChromatogramViewModel(ObservableCollection<RawSpectrum> spectrumCollection, 
            ProjectPropertyBean projectPropertyBean, PeakAreaBean peakAreaBean, AnalysisParametersBean analysisParametersBean, 
            Dictionary<int, AnalystExperimentInformationBean> analystExperimentInformationBean, 
            MS2DecResult deconvolutionResultBean, MrmChromatogramView mrmChromatogramView, List<SolidColorBrush> solidColorBrushList)
        {
            if (peakAreaBean.Ms2LevelDatapointNumber == -1) return null;

            ObservableCollection<ChromatogramBean> chromatogramBeanCollection = new ObservableCollection<ChromatogramBean>();

            int experimentCycleNumber = analystExperimentInformationBean.Count;
            int ms2LevelId = 0;

            float startRt = peakAreaBean.RtAtPeakTop - (peakAreaBean.RtAtRightPeakEdge - peakAreaBean.RtAtLeftPeakEdge) * 1.5F;
            float endRt = peakAreaBean.RtAtPeakTop + (peakAreaBean.RtAtRightPeakEdge - peakAreaBean.RtAtLeftPeakEdge) * 1.5F;

            double focusedMass1 = peakAreaBean.AccurateMass;
            double focusedMass2;
            List<double[]> ms2Peaklist = new List<double[]>();

            ObservableCollection<double[]> centroidedSpectraCollection;
            List<double[]> centroidedSpectraList;

            foreach (var value in analystExperimentInformationBean) { if (value.Value.MsType == MsType.SWATH && value.Value.StartMz < focusedMass1 && focusedMass1 <= value.Value.EndMz) { ms2LevelId = value.Key; break; } }

            if (mrmChromatogramView == MrmChromatogramView.raw)
            {
                centroidedSpectraCollection = DataAccessLcUtility.GetCentroidMassSpectra(spectrumCollection, projectPropertyBean.DataTypeMS2, peakAreaBean.Ms2LevelDatapointNumber, analysisParametersBean.CentroidMs2Tolerance, analysisParametersBean.PeakDetectionBasedCentroid);
                if (centroidedSpectraCollection == null || centroidedSpectraCollection.Count == 0) return null;
                centroidedSpectraList = new List<double[]>(centroidedSpectraCollection);
                centroidedSpectraList = centroidedSpectraList.OrderByDescending(n => n[1]).ToList();

                for (int i = 0; i < centroidedSpectraCollection.Count; i++)
                {
                    focusedMass2 = centroidedSpectraList[i][0];
                    ms2Peaklist = DataAccessLcUtility.GetMs2Peaklist(spectrumCollection, peakAreaBean.Ms2LevelDatapointNumber, ms2LevelId, experimentCycleNumber, startRt, endRt, focusedMass2, analysisParametersBean);
                    ms2Peaklist = DataAccessLcUtility.GetSmoothedPeaklist(ms2Peaklist, analysisParametersBean.SmoothingMethod, analysisParametersBean.SmoothingLevel);

                    if (i < 10)
                        chromatogramBeanCollection.Add(new ChromatogramBean(true, solidColorBrushList[i], 1.0, peakAreaBean.AccurateMass, (float)focusedMass2, new ObservableCollection<double[]>(ms2Peaklist), null));
                    else if (10 <= i && i < 30)
                        chromatogramBeanCollection.Add(new ChromatogramBean(false, solidColorBrushList[i], 1.0, peakAreaBean.AccurateMass, (float)focusedMass2, new ObservableCollection<double[]>(ms2Peaklist), null));
                    else
                        chromatogramBeanCollection.Add(new ChromatogramBean(false, solidColorBrushList[0], 1.0, peakAreaBean.AccurateMass, (float)focusedMass2, new ObservableCollection<double[]>(ms2Peaklist), null));
                }
            }
            else if (mrmChromatogramView == MrmChromatogramView.component)
            {
                centroidedSpectraList = deconvolutionResultBean.MassSpectra;
                centroidedSpectraList = centroidedSpectraList.OrderByDescending(n => n[1]).ToList();

                for (int i = 0; i < centroidedSpectraList.Count; i++)
                {
                    focusedMass2 = centroidedSpectraList[i][0];
                    ms2Peaklist = DataAccessLcUtility.GetMatchedMs2Peaklist(deconvolutionResultBean.PeaklistList, focusedMass2);
                    ms2Peaklist.Insert(0, new double[] { 0, startRt, 0, 0 });
                    ms2Peaklist.Add(new double[] { 0, endRt, 0, 0 });

                    if (i < 10)
                        chromatogramBeanCollection.Add(new ChromatogramBean(true, solidColorBrushList[i], 1.5, peakAreaBean.AccurateMass, (float)focusedMass2, new ObservableCollection<double[]>(ms2Peaklist), null));
                    else if (10 <= i && i < 30)
                        chromatogramBeanCollection.Add(new ChromatogramBean(false, solidColorBrushList[i], 1.5, peakAreaBean.AccurateMass, (float)focusedMass2, new ObservableCollection<double[]>(ms2Peaklist), null));
                    else
                        chromatogramBeanCollection.Add(new ChromatogramBean(false, solidColorBrushList[0], 1.5, peakAreaBean.AccurateMass, (float)focusedMass2, new ObservableCollection<double[]>(ms2Peaklist), null));
                }
            }
            else
            {
                centroidedSpectraCollection = DataAccessLcUtility.GetCentroidMassSpectra(spectrumCollection, projectPropertyBean.DataTypeMS2, peakAreaBean.Ms2LevelDatapointNumber, analysisParametersBean.CentroidMs2Tolerance, analysisParametersBean.PeakDetectionBasedCentroid);
                if (centroidedSpectraCollection == null || centroidedSpectraCollection.Count == 0) return null;
                centroidedSpectraList = new List<double[]>(centroidedSpectraCollection);
                centroidedSpectraList = centroidedSpectraList.OrderByDescending(n => n[1]).ToList();

                for (int i = 0; i < centroidedSpectraList.Count; i++)
                {
                    focusedMass2 = centroidedSpectraList[i][0];
                    ms2Peaklist = DataAccessLcUtility.GetMs2Peaklist(spectrumCollection, peakAreaBean.Ms2LevelDatapointNumber, ms2LevelId, experimentCycleNumber, startRt, endRt, focusedMass2, analysisParametersBean);
                    ms2Peaklist = DataAccessLcUtility.GetSmoothedPeaklist(ms2Peaklist, analysisParametersBean.SmoothingMethod, analysisParametersBean.SmoothingLevel);

                    if (i < 10)
                        chromatogramBeanCollection.Add(new ChromatogramBean(true, solidColorBrushList[i], 1.0, peakAreaBean.AccurateMass, (float)focusedMass2, new ObservableCollection<double[]>(ms2Peaklist), null));
                    else if (10 <= i && i < 30)
                        chromatogramBeanCollection.Add(new ChromatogramBean(false, solidColorBrushList[i], 1.0, peakAreaBean.AccurateMass, (float)focusedMass2, new ObservableCollection<double[]>(ms2Peaklist), null));
                    else
                        chromatogramBeanCollection.Add(new ChromatogramBean(false, solidColorBrushList[0], 1.0, peakAreaBean.AccurateMass, (float)focusedMass2, new ObservableCollection<double[]>(ms2Peaklist), null));
                }

                centroidedSpectraList = deconvolutionResultBean.MassSpectra;
                centroidedSpectraList = centroidedSpectraList.OrderByDescending(n => n[1]).ToList();

                for (int i = 0; i < centroidedSpectraList.Count; i++)
                {
                    focusedMass2 = centroidedSpectraList[i][0];
                    ms2Peaklist = DataAccessLcUtility.GetMatchedMs2Peaklist(deconvolutionResultBean.PeaklistList, focusedMass2);
                    ms2Peaklist.Insert(0, new double[] { 0, startRt, 0, 0 });
                    ms2Peaklist.Add(new double[] { 0, endRt, 0, 0 });

                    if (i < 10)
                        chromatogramBeanCollection.Add(new ChromatogramBean(true, solidColorBrushList[i], 1.5, peakAreaBean.AccurateMass, (float)focusedMass2, new ObservableCollection<double[]>(ms2Peaklist), null));
                    else if (10 <= i && i < 30)
                        chromatogramBeanCollection.Add(new ChromatogramBean(false, solidColorBrushList[i], 1.5, peakAreaBean.AccurateMass, (float)focusedMass2, new ObservableCollection<double[]>(ms2Peaklist), null));
                    else
                        chromatogramBeanCollection.Add(new ChromatogramBean(false, solidColorBrushList[0], 1.5, peakAreaBean.AccurateMass, (float)focusedMass2, new ObservableCollection<double[]>(ms2Peaklist), null));
                }
            }

            return new ChromatogramMrmViewModel(chromatogramBeanCollection, ChromatogramEditMode.Display, ChromatogramDisplayLabel.None, ChromatogramQuantitativeMode.Height, ChromatogramIntensityMode.Relative, -1, -1, "MS2 chromatograms ", -1, "", "", "", "", -1, -1, peakAreaBean.RtAtPeakTop, null, -1, -1);
        }

        public static ChromatogramMrmViewModel GetMs2ChromatogramIonMobilityViewModel(ObservableCollection<RawSpectrum> spectrumCollection,
            ProjectPropertyBean projectPropertyBean, PeakAreaBean peakAreaBean, DriftSpotBean driftSpot, AnalysisParametersBean analysisParametersBean,
            MS2DecResult deconvolutionResultBean, MrmChromatogramView mrmChromatogramView, List<SolidColorBrush> solidColorBrushList)
        {
            if (peakAreaBean.Ms2LevelDatapointNumber == -1) return null;
            if (projectPropertyBean.MethodType == MethodType.ddMSMS) return null;

            ObservableCollection<ChromatogramBean> chromatogramBeanCollection = new ObservableCollection<ChromatogramBean>();

            var dtWidth = (driftSpot.DriftTimeAtRightPeakEdge - driftSpot.DriftTimeAtLeftPeakEdge) * 1.5;
            var minDt = driftSpot.DriftTimeAtPeakTop - dtWidth;
            var maxDt = driftSpot.DriftTimeAtPeakTop + dtWidth;

            double focusedMass1 = peakAreaBean.AccurateMass;
            double focusedMass2;
            List<double[]> ms2Peaklist = new List<double[]>();
            var centroidedSpectraList = new List<double[]>();
            if (mrmChromatogramView == MrmChromatogramView.raw)
            {
                var targetSpectrum = new ObservableCollection<double[]>();
                var targetSpec = spectrumCollection[driftSpot.Ms2LevelDatapointNumber];
                var centroidSpec = new ObservableCollection<double[]>();
                for (var i = 0; i < targetSpec.Spectrum.Length; i++)
                {
                    targetSpectrum.Add(new double[] { targetSpec.Spectrum[i].Mz, targetSpec.Spectrum[i].Intensity });
                }
                if (projectPropertyBean.DataTypeMS2 == DataType.Profile)
                {
                    centroidSpec = SpectralCentroiding.Centroid(targetSpectrum, analysisParametersBean.CentroidMs2Tolerance, analysisParametersBean.PeakDetectionBasedCentroid);
                }
                else
                {
                    centroidSpec = targetSpectrum;
                }
                if (centroidSpec == null || centroidSpec.Count == 0) return null;

                var ms2peaklistlist = DataAccessLcUtility.GetAccumulatedMs2PeakListList(spectrumCollection, peakAreaBean, new List<double[]>(centroidSpec.OrderByDescending(x => x[1])), minDt, maxDt, projectPropertyBean.IonMode);
                if (ms2peaklistlist == null || ms2peaklistlist.Count == 0) return null;
                var smoothedMs2peaklistlist = new List<List<double[]>>();
                foreach (var peaklist in ms2peaklistlist)
                {
                    smoothedMs2peaklistlist.Add(DataAccessLcUtility.GetSmoothedPeaklist(peaklist, analysisParametersBean.SmoothingMethod, analysisParametersBean.SmoothingLevel));
                }

                for (int i = 0; i < smoothedMs2peaklistlist.Count; i++)
                {
                    focusedMass2 = smoothedMs2peaklistlist[i][0][2];

                    if (i < 10)
                        chromatogramBeanCollection.Add(new ChromatogramBean(true, solidColorBrushList[i], 1.0, peakAreaBean.AccurateMass, (float)focusedMass2, new ObservableCollection<double[]>(smoothedMs2peaklistlist[i]), null));
                    else if (10 <= i && i < 30)
                        chromatogramBeanCollection.Add(new ChromatogramBean(false, solidColorBrushList[i], 1.0, peakAreaBean.AccurateMass, (float)focusedMass2, new ObservableCollection<double[]>(smoothedMs2peaklistlist[i]), null));
                    else
                        chromatogramBeanCollection.Add(new ChromatogramBean(false, solidColorBrushList[0], 1.0, peakAreaBean.AccurateMass, (float)focusedMass2, new ObservableCollection<double[]>(smoothedMs2peaklistlist[i]), null));
                }
            }

            else if (mrmChromatogramView == MrmChromatogramView.component)
            {
                centroidedSpectraList = deconvolutionResultBean.MassSpectra;
                if (centroidedSpectraList == null || centroidedSpectraList.Count == 0) return null;
                centroidedSpectraList = centroidedSpectraList.OrderByDescending(n => n[1]).ToList();

                for (int i = 0; i < centroidedSpectraList.Count; i++)
                {
                    focusedMass2 = centroidedSpectraList[i][0];
                    ms2Peaklist = DataAccessLcUtility.GetMatchedMs2Peaklist(deconvolutionResultBean.PeaklistList, focusedMass2);
                    ms2Peaklist.Insert(0, new double[] { 0, minDt, 0, 0 });
                    ms2Peaklist.Add(new double[] { 0, maxDt, 0, 0 });

                    if (i < 10)
                        chromatogramBeanCollection.Add(new ChromatogramBean(true, solidColorBrushList[i], 1.5, peakAreaBean.AccurateMass, (float)focusedMass2, new ObservableCollection<double[]>(ms2Peaklist), null));
                    else if (10 <= i && i < 30)
                        chromatogramBeanCollection.Add(new ChromatogramBean(false, solidColorBrushList[i], 1.5, peakAreaBean.AccurateMass, (float)focusedMass2, new ObservableCollection<double[]>(ms2Peaklist), null));
                    else
                        chromatogramBeanCollection.Add(new ChromatogramBean(false, solidColorBrushList[0], 1.5, peakAreaBean.AccurateMass, (float)focusedMass2, new ObservableCollection<double[]>(ms2Peaklist), null));
                }
            }

            else
            {
                var targetSpectrum = new ObservableCollection<double[]>();
                var targetSpec = spectrumCollection[driftSpot.Ms2LevelDatapointNumber];
                var centroidSpec = new ObservableCollection<double[]>();
                for (var i = 0; i < targetSpec.Spectrum.Length; i++)
                {
                    targetSpectrum.Add(new double[] { targetSpec.Spectrum[i].Mz, targetSpec.Spectrum[i].Intensity });
                }
                if (projectPropertyBean.DataTypeMS2 == DataType.Profile)
                {
                    centroidSpec = SpectralCentroiding.Centroid(targetSpectrum, analysisParametersBean.CentroidMs2Tolerance, analysisParametersBean.PeakDetectionBasedCentroid);
                }
                else
                {
                    centroidSpec = targetSpectrum;
                }
                if (centroidSpec == null || centroidSpec.Count == 0) return null;

                var ms2peaklistlist = DataAccessLcUtility.GetAccumulatedMs2PeakListList(spectrumCollection, peakAreaBean, new List<double[]>(centroidSpec.OrderByDescending(x => x[1])), minDt, maxDt, projectPropertyBean.IonMode);
                if (ms2peaklistlist != null && ms2peaklistlist.Count > 0)
                {
                    var smoothedMs2peaklistlist = new List<List<double[]>>();
                    foreach (var peaklist in ms2peaklistlist)
                    {
                        smoothedMs2peaklistlist.Add(DataAccessLcUtility.GetSmoothedPeaklist(peaklist, analysisParametersBean.SmoothingMethod, analysisParametersBean.SmoothingLevel));
                    }

                    for (int i = 0; i < smoothedMs2peaklistlist.Count; i++)
                    {
                        focusedMass2 = smoothedMs2peaklistlist[i][0][2];

                        if (i < 10)
                            chromatogramBeanCollection.Add(new ChromatogramBean(true, solidColorBrushList[i], 1.0, peakAreaBean.AccurateMass, (float)focusedMass2, new ObservableCollection<double[]>(smoothedMs2peaklistlist[i]), null));
                        else if (10 <= i && i < 30)
                            chromatogramBeanCollection.Add(new ChromatogramBean(false, solidColorBrushList[i], 1.0, peakAreaBean.AccurateMass, (float)focusedMass2, new ObservableCollection<double[]>(smoothedMs2peaklistlist[i]), null));
                        else
                            chromatogramBeanCollection.Add(new ChromatogramBean(false, solidColorBrushList[0], 1.0, peakAreaBean.AccurateMass, (float)focusedMass2, new ObservableCollection<double[]>(smoothedMs2peaklistlist[i]), null));
                    }
                }
                centroidedSpectraList = deconvolutionResultBean.MassSpectra;
                if (centroidedSpectraList != null && centroidedSpectraList.Count > 0)
                {
                    centroidedSpectraList = centroidedSpectraList.OrderByDescending(n => n[1]).ToList();

                    for (int i = 0; i < centroidedSpectraList.Count; i++)
                    {
                        focusedMass2 = centroidedSpectraList[i][0];
                        ms2Peaklist = DataAccessLcUtility.GetMatchedMs2Peaklist(deconvolutionResultBean.PeaklistList, focusedMass2);
                        ms2Peaklist.Insert(0, new double[] { 0, minDt, 0, 0 });
                        ms2Peaklist.Add(new double[] { 0, maxDt, 0, 0 });

                        if (i < 10)
                            chromatogramBeanCollection.Add(new ChromatogramBean(true, solidColorBrushList[i], 1.5, peakAreaBean.AccurateMass, (float)focusedMass2, new ObservableCollection<double[]>(ms2Peaklist), null));
                        else if (10 <= i && i < 30)
                            chromatogramBeanCollection.Add(new ChromatogramBean(false, solidColorBrushList[i], 1.5, peakAreaBean.AccurateMass, (float)focusedMass2, new ObservableCollection<double[]>(ms2Peaklist), null));
                        else
                            chromatogramBeanCollection.Add(new ChromatogramBean(false, solidColorBrushList[0], 1.5, peakAreaBean.AccurateMass, (float)focusedMass2, new ObservableCollection<double[]>(ms2Peaklist), null));
                    }
                }
            }
            
            return new ChromatogramMrmViewModel(chromatogramBeanCollection, ChromatogramEditMode.Display, ChromatogramDisplayLabel.None, ChromatogramQuantitativeMode.Height, ChromatogramIntensityMode.Relative, -1, -1, "MS2 chromatograms ", -1, "", "", "", "", -1, -1, peakAreaBean.RtAtPeakTop, null, -1, -1);
        }


        public static ChromatogramMrmViewModel GetMs2ChromatogramViewModel(PeakAreaBean peakAreaBean, MS2DecResult ms2DecResult, 
            List<SolidColorBrush> solidColorBrushList)
        {
            if (peakAreaBean.Ms2LevelDatapointNumber == -1) return null;
            if (ms2DecResult.PeaklistList.Count == 0) return null;


            ObservableCollection<ChromatogramBean> chromatogramBeanCollection = new ObservableCollection<ChromatogramBean>();

            float productMz;
            List<double[]> ms2Peaklist;

            for (int i = 0; i < ms2DecResult.PeaklistList.Count; i++)
            {
                productMz = (float)ms2DecResult.MassSpectra[i][0];
                ms2Peaklist = ms2DecResult.PeaklistList[i];

                if (i < 10)
                    chromatogramBeanCollection.Add(new ChromatogramBean(true, solidColorBrushList[i], 1.0, peakAreaBean.AccurateMass, (float)productMz, new ObservableCollection<double[]>(ms2Peaklist), null));
                else if (10 <= i && i < 30)
                    chromatogramBeanCollection.Add(new ChromatogramBean(false, solidColorBrushList[i], 1.0, peakAreaBean.AccurateMass, (float)productMz, new ObservableCollection<double[]>(ms2Peaklist), null));
                else
                    chromatogramBeanCollection.Add(new ChromatogramBean(false, solidColorBrushList[0], 1.0, peakAreaBean.AccurateMass, (float)productMz, new ObservableCollection<double[]>(ms2Peaklist), null));
            }

            if (chromatogramBeanCollection.Count != 0)
                return new ChromatogramMrmViewModel(chromatogramBeanCollection, ChromatogramEditMode.Display, ChromatogramDisplayLabel.None, ChromatogramQuantitativeMode.Height, ChromatogramIntensityMode.Relative, -1, -1, "", -1, "", "", "", "", -1, -1, peakAreaBean.RtAtPeakTop, null, -1, -1);
            else
                return null;
        }

        public static CompMs.Graphics.Core.Base.DrawVisual GetMs2ChromatogramDrawVisual(ObservableCollection<RawSpectrum> spectrumCollection, DriftSpotBean driftSpot, PeakAreaBean peakAreaBean, 
            MS2DecResult ms2decres, ProjectPropertyBean projectPropertyBean, AnalysisParametersBean param)
        {
            //if (peakAreaBean.Ms2LevelDatapointNumber == -1) return null;

            var targetSpectrum = new List<double[]>();
            if (ms2decres == null)
            {
                var ms1datapoint = driftSpot.Ms1LevelDatapointNumber;
                var targetSpec = spectrumCollection[ms1datapoint];
                for(var i = ms1datapoint; i < spectrumCollection.Count; i++)
                {
                    var spectrum = spectrumCollection[i];
                    if (spectrum.MsLevel <= 1) continue;
                    if (spectrum.DriftTime == driftSpot.DriftTimeAtPeakTop)
                    {
                        targetSpec = spectrum;
                        break;
                    }
                }
                for (var i = 0; i < targetSpec.Spectrum.Length; i++)
                {
                    targetSpectrum.Add(new double[] { targetSpec.Spectrum[i].Mz, targetSpec.Spectrum[i].Intensity });
                }

            }
            else
            {
                for (var i = 0; i < ms2decres.MassSpectra.Count; i++)
                {
                    targetSpectrum.Add(new double[] { ms2decres.MassSpectra[i][0], ms2decres.MassSpectra[i][1] });
                }
            }
            var peaklistlist = new List<List<float[]>>();
            var dtWidth = (driftSpot.DriftTimeAtRightPeakEdge - driftSpot.DriftTimeAtLeftPeakEdge) * 1.5;
            var minDt = driftSpot.DriftTimeAtPeakTop - dtWidth;
            var maxDt = driftSpot.DriftTimeAtPeakTop + dtWidth;
            var ms2peaklistlist = DataAccessLcUtility.GetAccumulatedMs2PeakListList(spectrumCollection, peakAreaBean, targetSpectrum, minDt, maxDt, projectPropertyBean.IonMode);
            var smoothedMs2peaklistlist = new List<List<double[]>>();
            foreach(var peaklist in ms2peaklistlist)
            {
                smoothedMs2peaklistlist.Add(DataAccessLcUtility.GetSmoothedPeaklist(peaklist, param.SmoothingMethod, param.SmoothingLevel));
            }

            var labelList = new List<string>();
            for (int i = 0; i < smoothedMs2peaklistlist.Count; i++)
            {
                var peaklist = new List<float[]>();
                for (var j = 0; j < smoothedMs2peaklistlist[i].Count; j++)
                {
                    peaklist.Add(new float[2] { (float)smoothedMs2peaklistlist[i][j][1], (float)smoothedMs2peaklistlist[i][j][3] });
                }
                peaklistlist.Add(peaklist);
            }
            return CompMs.Graphics.Core.Base.Utility.GetChromatogramV1(peaklistlist);
            
        }

        private static MassSpectrogramBean getMassSpectrogramBean(ObservableCollection<RawSpectrum> spectrumCollection,
            int msScanPoint, float massBin, 
            bool peakDetectionBasedCentroid,
            //bool isIonMobility, 
            DataType dataType)
        {
            if (msScanPoint < 0) return null;

            var masslist = new ObservableCollection<double[]>();
            var centroidedMassSpectra = new ObservableCollection<double[]>();
            var massSpectraDisplayLabelCollection = new ObservableCollection<MassSpectrogramDisplayLabel>();
            RawSpectrum spectrum;
            RawPeakElement[] massSpectra;

            spectrum = spectrumCollection[msScanPoint];
            massSpectra = spectrum.Spectrum;

            for (int i = 0; i < massSpectra.Length; i++)
                masslist.Add(new double[] { massSpectra[i].Mz, massSpectra[i].Intensity });
            if (dataType == DataType.Profile)
            {
                centroidedMassSpectra = SpectralCentroiding.Centroid(masslist, massBin, peakDetectionBasedCentroid);
            }
            else
            {
                centroidedMassSpectra = masslist;
            }

            if (centroidedMassSpectra == null || centroidedMassSpectra.Count == 0)
            {
                return null;
            }
            else
            {
                for (int i = 0; i < centroidedMassSpectra.Count; i++)
                    massSpectraDisplayLabelCollection.Add(new MassSpectrogramDisplayLabel() { Mass = centroidedMassSpectra[i][0], Intensity = centroidedMassSpectra[i][1], Label = Math.Round(centroidedMassSpectra[i][0], 4).ToString() });

                return new MassSpectrogramBean(Brushes.Blue, 1.0, centroidedMassSpectra, massSpectraDisplayLabelCollection);
            }
        }

        private static MassSpectrogramBean getAccumulatedMs1Spectrum(ObservableCollection<RawSpectrum> accumulatedSpectra,
            int scanNumber, float massBin,
            bool peakDetectionBasedCentroid,
            DataType dataType) {
            if (scanNumber < 0) return null;
            if (accumulatedSpectra == null) return null;
            if (accumulatedSpectra.Count - 1 < scanNumber) return null;

            var masslist = new ObservableCollection<double[]>();
            var centroidedMassSpectra = new ObservableCollection<double[]>();
            var massSpectraDisplayLabelCollection = new ObservableCollection<MassSpectrogramDisplayLabel>();
            RawSpectrum spectrum;
            RawPeakElement[] massSpectra;

            //var scanNumber = convertOriginalScanIdToAccumulatedSpectraScanNumberInIonmobilityProject(originalScanID, accumulatedSpectra);
            //if (scanNumber == -1) return null;
            spectrum = accumulatedSpectra[scanNumber];
            massSpectra = spectrum.Spectrum;

            var maxIntensity = spectrum.BasePeakIntensity;
            for (int i = 0; i < massSpectra.Length; i++) { // to reduce the required GUI refresh cost, 1/100 relative ion intensity is removed for the visualization 
                if (massSpectra[i].Intensity > maxIntensity * 0.01)
                    masslist.Add(new double[] { massSpectra[i].Mz, massSpectra[i].Intensity });
            }

            if (dataType == DataType.Profile) {
                centroidedMassSpectra = SpectralCentroiding.Centroid(masslist, massBin, peakDetectionBasedCentroid);
            }
            else {
                centroidedMassSpectra = masslist;
            }

            if (centroidedMassSpectra == null || centroidedMassSpectra.Count == 0) {
                return null;
            }
            else {
                for (int i = 0; i < centroidedMassSpectra.Count; i++)
                    massSpectraDisplayLabelCollection.Add(new MassSpectrogramDisplayLabel() { Mass = centroidedMassSpectra[i][0], Intensity = centroidedMassSpectra[i][1], Label = Math.Round(centroidedMassSpectra[i][0], 4).ToString() });

                return new MassSpectrogramBean(Brushes.Blue, 1.0, centroidedMassSpectra, massSpectraDisplayLabelCollection);
            }
        }

        private static int convertOriginalScanIdToAccumulatedSpectraScanNumberInIonmobilityProject(int originalScanID, ObservableCollection<RawSpectrum> accumulatedSpectra) {
            var startIndex = DataAccessLcUtility.GetRawSpectrumObjectsStartIndex(originalScanID, accumulatedSpectra, true);
            for (int i = startIndex; i < accumulatedSpectra.Count; i++) {
                var spectrum = accumulatedSpectra[i];
                if (spectrum.OriginalIndex == originalScanID) {
                    return i;
                }
            }
            return -1;
        }

        private static MassSpectrogramBean getMassSpectrogramBean(MS2DecResult deconvolutionResultBean, SolidColorBrush spectrumBrush)
        {
            ObservableCollection<MassSpectrogramDisplayLabel> massSpectraDisplayLabelCollection = new ObservableCollection<MassSpectrogramDisplayLabel>();
            List<double[]> masslist = new List<double[]>();

            for (int i = 0; i < deconvolutionResultBean.MassSpectra.Count; i++)
                 masslist.Add(new double[] { deconvolutionResultBean.MassSpectra[i][0], deconvolutionResultBean.MassSpectra[i][1] });

            if (masslist.Count > 0)
                masslist = masslist.OrderBy(n => n[0]).ToList();

            for (int i = 0; i < masslist.Count; i++)
                massSpectraDisplayLabelCollection.Add(new MassSpectrogramDisplayLabel() { Mass = masslist[i][0], Intensity = masslist[i][1], Label = Math.Round(masslist[i][0], 4).ToString() });

            if (masslist.Count > 0)
                return new MassSpectrogramBean(spectrumBrush, 1.0, new ObservableCollection<double[]>(masslist), massSpectraDisplayLabelCollection);
            else
                return new MassSpectrogramBean(spectrumBrush, 1.0, null); ;
        }

        private static MassSpectrogramBean getReferenceSpectra(List<MspFormatCompoundInformationBean> mspFormatCompoundInformationBeanList, 
            int libraryID, SolidColorBrush spectrumBrush)
        {
            var spectra = new ObservableCollection<double[]>();
            var labels = new ObservableCollection<MassSpectrogramDisplayLabel>();

            if (libraryID < 0) return new MassSpectrogramBean(Brushes.Red, 1.0, null);
            if (libraryID > mspFormatCompoundInformationBeanList.Count - 1) return new MassSpectrogramBean(Brushes.Red, 1.0, null);
            var record = mspFormatCompoundInformationBeanList[libraryID];
            for (int i = 0; i < record.MzIntensityCommentBeanList.Count; i++)
            {
                var peak = record.MzIntensityCommentBeanList[i];
                var mz = (double)peak.Mz;
                var intensity = (double)peak.Intensity;
                var comment = peak.Comment;
                var commentString = Math.Round(mz, 4).ToString();
                spectra.Add(new double[] { mz, intensity });

                labels.Add(
                    new MassSpectrogramDisplayLabel() {
                        Mass = mz,
                        Intensity = intensity,
                        Label = commentString
                    });
            }

            return new MassSpectrogramBean(spectrumBrush, 1.0, spectra, labels);
        }

        public static ChromatogramTicEicViewModel GetAlignedEicChromatogram(AlignedData alignedData,
            ObservableCollection<AlignedPeakPropertyBean> alignedPeakPropertyBeans, 
            ObservableCollection<AnalysisFileBean> files, ProjectPropertyBean projectPropety, 
            AnalysisParametersBean param, bool isDriftAxis) {
            if (alignedData == null) return null;

            var chromatogramBeanCollection = new ObservableCollection<ChromatogramBean>();
            var targetMz = alignedData.Mz;
            var numAnalysisfiles = alignedData.NumAnalysisFiles;
            var classnameToBytes = projectPropety.ClassnameToColorBytes;
            var classnameToBrushes = MsDialStatistics.ConvertToSolidBrushDictionary(classnameToBytes);
            //var classIdColorDictionary = MsDialStatistics.GetClassIdColorDictionary(files, solidColorBrushList);
            for (int i = 0; i < numAnalysisfiles; i++) { // draw the included samples
                var peaks = alignedData.PeakLists[i].PeakList;
                var peaklist = new List<double[]>();
                for (int j = 0; j < peaks.Count; j++) {
                    peaklist.Add(new double[] { j, (double)peaks[j][0], targetMz, (double)peaks[j][1] });
                }
                peaklist = DataAccessLcUtility.GetSmoothedPeaklist(peaklist, param.SmoothingMethod, param.SmoothingLevel);

                var rt = isDriftAxis ? alignedPeakPropertyBeans[i].DriftTime : alignedPeakPropertyBeans[i].RetentionTime;
                var rtLeft = isDriftAxis ? alignedPeakPropertyBeans[i].DriftTimeLeft : alignedPeakPropertyBeans[i].RetentionTimeLeft;
                var rtRight = isDriftAxis ? alignedPeakPropertyBeans[i].DriftTimeRight : alignedPeakPropertyBeans[i].RetentionTimeRight;

                var chromatogramBean = new ChromatogramBean(true, classnameToBrushes[files[i].AnalysisFilePropertyBean.AnalysisFileClass], 1.5, 
                    files[i].AnalysisFilePropertyBean.AnalysisFileName, 
                    targetMz, param.CentroidMs1Tolerance,
                    rt, rtLeft, rtRight,
                    alignedData.PeakLists[i].GapFilled,
                    new ObservableCollection<double[]>(peaklist));
                chromatogramBeanCollection.Add(chromatogramBean);
            }

            var xAxisTitle = isDriftAxis ? getDriftAxisTitle(param.IonMobilityType) : "Retention time [min]";

            return new ChromatogramTicEicViewModel(chromatogramBeanCollection, ChromatogramEditMode.Display, ChromatogramDisplayLabel.AnnotatedMetabolite, ChromatogramQuantitativeMode.Height, ChromatogramIntensityMode.Absolute, 
                "EICs of aligned results", -1, "Selected files", "Selected files", "Selected files", -1, xAxisTitle);
        }

       
    }
}
