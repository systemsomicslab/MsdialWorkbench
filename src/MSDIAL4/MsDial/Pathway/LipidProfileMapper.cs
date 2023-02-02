using Newtonsoft.Json;
using Riken.Metabolomics.Lipoquality;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;

namespace Rfx.Riken.OsakaUniv {
    public sealed class LipidProfileMapper {
        private LipidProfileMapper() { }

        public static void LipidProfileProjectionToCytoscape(MainWindow mainWindow) {

            var height = 150;
            var width = 300;
            var alignedSpots = mainWindow.FocusedAlignmentResult.AlignmentPropertyBeanCollection;
            var mspDB = mainWindow.MspDB;
            var analysisFiles = mainWindow.AnalysisFiles;
            var colorBrushes = mainWindow.SolidColorBrushList;
            var project = mainWindow.ProjectProperty;
            var barChartDisplayMode = mainWindow.BarChartDisplayMode;
            var isBoxPlot = mainWindow.ProjectProperty.IsBoxPlotForAlignmentResult;

            var curDir = System.AppDomain.CurrentDomain.BaseDirectory;
            var exportfolder = curDir + "CytoscapeLocalBrowser/lipidgraphs";
            if (!System.IO.Directory.Exists(exportfolder)) {
                var di = System.IO.Directory.CreateDirectory(exportfolder);
            }

            var ticPropeties = new Dictionary<string, AlignmentPropertyBean>();
            //initialization
            #region
            ticPropeties["MAG"] = new AlignmentPropertyBean() { MetaboliteName = "MAG" };
            ticPropeties["DAG"] = new AlignmentPropertyBean() { MetaboliteName = "DAG" };
            ticPropeties["TAG"] = new AlignmentPropertyBean() { MetaboliteName = "TAG" };
            ticPropeties["LPC"] = new AlignmentPropertyBean() { MetaboliteName = "LPC" };
            ticPropeties["LPE"] = new AlignmentPropertyBean() { MetaboliteName = "LPE" };
            ticPropeties["LPG"] = new AlignmentPropertyBean() { MetaboliteName = "LPG" };
            ticPropeties["LPI"] = new AlignmentPropertyBean() { MetaboliteName = "LPI" };
            ticPropeties["LPS"] = new AlignmentPropertyBean() { MetaboliteName = "LPS" };
            ticPropeties["LPA"] = new AlignmentPropertyBean() { MetaboliteName = "LPA" };
            ticPropeties["LDGTS"] = new AlignmentPropertyBean() { MetaboliteName = "LDGTS" };
            ticPropeties["LDGTA"] = new AlignmentPropertyBean() { MetaboliteName = "LDGTA" };
            ticPropeties["PC"] = new AlignmentPropertyBean() { MetaboliteName = "PC" };
            ticPropeties["PE"] = new AlignmentPropertyBean() { MetaboliteName = "PE" };
            ticPropeties["PG"] = new AlignmentPropertyBean() { MetaboliteName = "PG" };
            ticPropeties["PI"] = new AlignmentPropertyBean() { MetaboliteName = "PI" };
            ticPropeties["PS"] = new AlignmentPropertyBean() { MetaboliteName = "PS" };
            ticPropeties["PA"] = new AlignmentPropertyBean() { MetaboliteName = "PA" };
            ticPropeties["BMP"] = new AlignmentPropertyBean() { MetaboliteName = "BMP" };
            ticPropeties["HBMP"] = new AlignmentPropertyBean() { MetaboliteName = "HBMP" };
            ticPropeties["CL"] = new AlignmentPropertyBean() { MetaboliteName = "CL" };
            ticPropeties["EtherPC"] = new AlignmentPropertyBean() { MetaboliteName = "EtherPC" };
            ticPropeties["EtherPE"] = new AlignmentPropertyBean() { MetaboliteName = "EtherPE" };
            ticPropeties["OxPC"] = new AlignmentPropertyBean() { MetaboliteName = "OxPC" };
            ticPropeties["OxPE"] = new AlignmentPropertyBean() { MetaboliteName = "OxPE" };
            ticPropeties["OxPG"] = new AlignmentPropertyBean() { MetaboliteName = "OxPG" };
            ticPropeties["OxPI"] = new AlignmentPropertyBean() { MetaboliteName = "OxPI" };
            ticPropeties["OxPS"] = new AlignmentPropertyBean() { MetaboliteName = "OxPS" };
            ticPropeties["EtherOxPC"] = new AlignmentPropertyBean() { MetaboliteName = "EtherOxPC" };
            ticPropeties["EtherOxPE"] = new AlignmentPropertyBean() { MetaboliteName = "EtherOxPE" };
            ticPropeties["PMeOH"] = new AlignmentPropertyBean() { MetaboliteName = "PMeOH" };
            ticPropeties["PEtOH"] = new AlignmentPropertyBean() { MetaboliteName = "PEtOH" };
            ticPropeties["MGDG"] = new AlignmentPropertyBean() { MetaboliteName = "MGDG" };
            ticPropeties["DGDG"] = new AlignmentPropertyBean() { MetaboliteName = "DGDG" };
            ticPropeties["SQDG"] = new AlignmentPropertyBean() { MetaboliteName = "SQDG" };
            ticPropeties["DGTS"] = new AlignmentPropertyBean() { MetaboliteName = "DGTS" };
            ticPropeties["DGTA"] = new AlignmentPropertyBean() { MetaboliteName = "DGTA" };
            ticPropeties["GlcADG"] = new AlignmentPropertyBean() { MetaboliteName = "GlcADG" };
            ticPropeties["AcylGlcADG"] = new AlignmentPropertyBean() { MetaboliteName = "AcylGlcADG" };
            ticPropeties["CE"] = new AlignmentPropertyBean() { MetaboliteName = "CE" };
            ticPropeties["ACar"] = new AlignmentPropertyBean() { MetaboliteName = "ACar" };
            ticPropeties["FA"] = new AlignmentPropertyBean() { MetaboliteName = "FA" };
            ticPropeties["FAHFA"] = new AlignmentPropertyBean() { MetaboliteName = "FAHFA" };
            ticPropeties["SM"] = new AlignmentPropertyBean() { MetaboliteName = "SM" };
            ticPropeties["Cer-ADS"] = new AlignmentPropertyBean() { MetaboliteName = "Cer-ADS" };
            ticPropeties["Cer-AS"] = new AlignmentPropertyBean() { MetaboliteName = "Cer-AS" };
            ticPropeties["Cer-BDS"] = new AlignmentPropertyBean() { MetaboliteName = "Cer-BDS" };
            ticPropeties["Cer-BS"] = new AlignmentPropertyBean() { MetaboliteName = "Cer-BS" };
            ticPropeties["Cer-EODS"] = new AlignmentPropertyBean() { MetaboliteName = "Cer-EODS" };
            ticPropeties["Cer-EOS"] = new AlignmentPropertyBean() { MetaboliteName = "Cer-EOS" };
            ticPropeties["Cer-NDS"] = new AlignmentPropertyBean() { MetaboliteName = "Cer-NDS" };
            ticPropeties["Cer-NS"] = new AlignmentPropertyBean() { MetaboliteName = "Cer-NS" };
            ticPropeties["Cer-NP"] = new AlignmentPropertyBean() { MetaboliteName = "Cer-NP" };
            ticPropeties["Cer-AP"] = new AlignmentPropertyBean() { MetaboliteName = "Cer-AP" };
            ticPropeties["HexCer-NS"] = new AlignmentPropertyBean() { MetaboliteName = "HexCer-NS" };
            ticPropeties["HexCer-NDS"] = new AlignmentPropertyBean() { MetaboliteName = "HexCer-NDS" };
            ticPropeties["HexCer-AP"] = new AlignmentPropertyBean() { MetaboliteName = "HexCer-AP" };
            ticPropeties["SHexCer"] = new AlignmentPropertyBean() { MetaboliteName = "SHexCer" };
            ticPropeties["GM3"] = new AlignmentPropertyBean() { MetaboliteName = "GM3" };
            #endregion

            var detailProperties = new Dictionary<string, List<AlignmentPropertyBean>>();
            //initialization
            #region
            detailProperties["MAG"] = new List<AlignmentPropertyBean>();
            detailProperties["DAG"] = new List<AlignmentPropertyBean>();
            detailProperties["TAG"] = new List<AlignmentPropertyBean>();
            detailProperties["LPC"] = new List<AlignmentPropertyBean>();
            detailProperties["LPE"] = new List<AlignmentPropertyBean>();
            detailProperties["LPG"] = new List<AlignmentPropertyBean>();
            detailProperties["LPI"] = new List<AlignmentPropertyBean>();
            detailProperties["LPS"] = new List<AlignmentPropertyBean>();
            detailProperties["LPA"] = new List<AlignmentPropertyBean>();
            detailProperties["LDGTS"] = new List<AlignmentPropertyBean>();
            detailProperties["LDGTA"] = new List<AlignmentPropertyBean>();
            detailProperties["PC"] = new List<AlignmentPropertyBean>();
            detailProperties["PE"] = new List<AlignmentPropertyBean>();
            detailProperties["PG"] = new List<AlignmentPropertyBean>();
            detailProperties["PI"] = new List<AlignmentPropertyBean>();
            detailProperties["PS"] = new List<AlignmentPropertyBean>();
            detailProperties["PA"] = new List<AlignmentPropertyBean>();
            detailProperties["BMP"] = new List<AlignmentPropertyBean>();
            detailProperties["HBMP"] = new List<AlignmentPropertyBean>();
            detailProperties["CL"] = new List<AlignmentPropertyBean>();
            detailProperties["EtherPC"] = new List<AlignmentPropertyBean>();
            detailProperties["EtherPE"] = new List<AlignmentPropertyBean>();
            detailProperties["OxPC"] = new List<AlignmentPropertyBean>();
            detailProperties["OxPE"] = new List<AlignmentPropertyBean>();
            detailProperties["OxPG"] = new List<AlignmentPropertyBean>();
            detailProperties["OxPI"] = new List<AlignmentPropertyBean>();
            detailProperties["OxPS"] = new List<AlignmentPropertyBean>();
            detailProperties["EtherOxPC"] = new List<AlignmentPropertyBean>();
            detailProperties["EtherOxPE"] = new List<AlignmentPropertyBean>();
            detailProperties["PMeOH"] = new List<AlignmentPropertyBean>();
            detailProperties["PEtOH"] = new List<AlignmentPropertyBean>();
            detailProperties["MGDG"] = new List<AlignmentPropertyBean>();
            detailProperties["DGDG"] = new List<AlignmentPropertyBean>();
            detailProperties["SQDG"] = new List<AlignmentPropertyBean>();
            detailProperties["DGTS"] = new List<AlignmentPropertyBean>();
            detailProperties["DGTA"] = new List<AlignmentPropertyBean>();
            detailProperties["GlcADG"] = new List<AlignmentPropertyBean>();
            detailProperties["AcylGlcADG"] = new List<AlignmentPropertyBean>();
            detailProperties["CE"] = new List<AlignmentPropertyBean>();
            detailProperties["ACar"] = new List<AlignmentPropertyBean>();
            detailProperties["FA"] = new List<AlignmentPropertyBean>();
            detailProperties["FAHFA"] = new List<AlignmentPropertyBean>();
            detailProperties["SM"] = new List<AlignmentPropertyBean>();
            detailProperties["Cer-ADS"] = new List<AlignmentPropertyBean>();
            detailProperties["Cer-AS"] = new List<AlignmentPropertyBean>();
            detailProperties["Cer-BDS"] = new List<AlignmentPropertyBean>();
            detailProperties["Cer-BS"] = new List<AlignmentPropertyBean>();
            detailProperties["Cer-EODS"] = new List<AlignmentPropertyBean>();
            detailProperties["Cer-EOS"] = new List<AlignmentPropertyBean>();
            detailProperties["Cer-NDS"] = new List<AlignmentPropertyBean>();
            detailProperties["Cer-NS"] = new List<AlignmentPropertyBean>();
            detailProperties["Cer-NP"] = new List<AlignmentPropertyBean>();
            detailProperties["Cer-AP"] = new List<AlignmentPropertyBean>();
            detailProperties["HexCer-NS"] = new List<AlignmentPropertyBean>();
            detailProperties["HexCer-NDS"] = new List<AlignmentPropertyBean>();
            detailProperties["HexCer-AP"] = new List<AlignmentPropertyBean>();
            detailProperties["SHexCer"] = new List<AlignmentPropertyBean>();
            detailProperties["GM3"] = new List<AlignmentPropertyBean>();
            #endregion

            //adding the values to ticProperties
            foreach (var spot in alignedSpots) {
                //do not export unknowsn corrently
                if (spot.LibraryID < 0) continue;
                if (spot.MetaboliteName.Contains("w/o MS2:")) continue;
                if (spot.MetaboliteName.Contains("Unsettled:")) continue;
                if (spot.MetaboliteName.Contains("Unknown")) continue;

                var mspQuery = mspDB[spot.LibraryID];
                var annotation = LipoqualityDatabaseManagerUtility.ConvertMsdialLipidnameToLipidAnnotation(mspQuery, spot.MetaboliteName);
                if (annotation.LipidClass == null || annotation.LipidClass == string.Empty) continue;

                annotation.SpotID = spot.AlignmentID;
                annotation.Rt = spot.CentralRetentionTime;
                annotation.Mz = spot.CentralAccurateMass;

                if (ticPropeties.ContainsKey(annotation.LipidClass)) {

                    var ticProp = ticPropeties[annotation.LipidClass];
                    if (ticProp.AlignedPeakPropertyBeanCollection == null || ticProp.AlignedPeakPropertyBeanCollection.Count == 0) {
                        ticProp.AlignedPeakPropertyBeanCollection = new ObservableCollection<AlignedPeakPropertyBean>();
                        foreach (var spotPeak in spot.AlignedPeakPropertyBeanCollection) {
                            ticProp.AlignedPeakPropertyBeanCollection.Add(new AlignedPeakPropertyBean() {
                                AccurateMass = spotPeak.AccurateMass,
                                FileID = spotPeak.FileID,
                                FileName = spotPeak.FileName,
                                PeakID = spotPeak.PeakID,
                                RetentionTime = spotPeak.RetentionTime,
                                RetentionTimeLeft = spotPeak.RetentionTimeLeft,
                                RetentionTimeRight = spotPeak.RetentionTimeRight,
                                Variable = spotPeak.Variable,
                                Area = spotPeak.Area,
                                PeakWidth = spotPeak.PeakWidth,
                                Ms2ScanNumber = spotPeak.Ms2ScanNumber,
                                MetaboliteName = spotPeak.MetaboliteName,
                                LibraryID = spotPeak.LibraryID,
                                PostIdentificationLibraryID = spotPeak.PostIdentificationLibraryID,
                                MassSpectraSimilarity = spotPeak.MassSpectraSimilarity,
                                ReverseSimilarity = spotPeak.ReverseSimilarity,
                                FragmentPresencePercentage = spotPeak.FragmentPresencePercentage,
                                IsotopeSimilarity = spotPeak.IsotopeSimilarity,
                                AdductIonName = spotPeak.AdductIonName,
                                IsotopeNumber = spotPeak.IsotopeNumber,
                                IsotopeParentID = spotPeak.IsotopeParentID,
                                ChargeNumber = spotPeak.ChargeNumber,
                                RetentionTimeSimilarity = spotPeak.RetentionTimeSimilarity,
                                AccurateMassSimilarity = spotPeak.AccurateMassSimilarity,
                                TotalSimilairty = spotPeak.TotalSimilairty
                            });
                        }
                    }
                    else {
                        for (int i = 0; i < spot.AlignedPeakPropertyBeanCollection.Count; i++) {
                            var spotPeak = spot.AlignedPeakPropertyBeanCollection[i];
                            ticProp.AlignedPeakPropertyBeanCollection[i].Variable += spotPeak.Variable;
                        }
                    }
                }

                if (detailProperties.ContainsKey(annotation.LipidClass)) {
                    detailProperties[annotation.LipidClass].Add(spot);
                }
            }

            foreach (var ticProp in ticPropeties) {
                var exportFilePath = exportfolder + "\\" + ticProp.Key + ".png";
                if (ticProp.Value.AlignedPeakPropertyBeanCollection == null || ticProp.Value.AlignedPeakPropertyBeanCollection.Count == 0)
                    continue;

                var barChartBean = MsDialStatistics.GetBarChartBean(ticProp.Value, analysisFiles, project, barChartDisplayMode, isBoxPlot);
                var image = new Common.BarChart.PlainBarChartForTable(height, width, 300, 300).DrawBarChart2BitmapSource(barChartBean, true);
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));

                using (var fs = File.Open(exportFilePath, FileMode.Create)) { encoder.Save(fs); }
            }

            foreach (var detailProp in detailProperties) {
                var exportFilePath = exportfolder + "\\" + detailProp.Key + ".json";
                if (detailProp.Value.Count() == 0)
                    continue;

                var nodes = new List<Node>();
                var properties = detailProp.Value;

                for (int i = 0; i < properties.Count; i++) {
                    var prop = properties[i];
                    var barChartBean = MsDialStatistics.GetBarChartBean(prop, analysisFiles, project, barChartDisplayMode, isBoxPlot);
                    var chartJs = new Chart() { type = "bar", data = new ChartData() };
                    chartJs.data.labels = new List<string>();
                    chartJs.data.datasets = new List<ChartElement>();
                    var chartJsElement = new ChartElement() { label = "", data = new List<double>(), backgroundColor = new List<string>() };

                    foreach (var chartElem in barChartBean.BarElements) {
                        chartJs.data.labels.Add(chartElem.Legend);
                        chartJsElement.data.Add(chartElem.Value);
                        chartJsElement.backgroundColor.Add("rgba(" + chartElem.Brush.Color.R + "," + chartElem.Brush.Color.G + "," + chartElem.Brush.Color.B + ", 0.8)");
                    }
                    chartJs.data.datasets.Add(chartJsElement);

                    var node = new Node() {
                        data = new NodeData() {
                            id = prop.AlignmentID,
                            Name = prop.MetaboliteName,
                            BarGraph = chartJs
                        }
                    };
                    nodes.Add(node);
                }

                var root = new RootObject() { nodes = nodes };
                var json = JsonConvert.SerializeObject(root, Formatting.Indented);
                using (StreamWriter sw = new StreamWriter(exportFilePath, false, Encoding.ASCII)) {
                    sw.WriteLine(json.ToString());
                }

                
            }

            var url = curDir + "/CytoscapeLocalBrowser/MsdialLipidNetworkViewer.html";
            System.Diagnostics.Process.Start(url);
        }
    }
}
