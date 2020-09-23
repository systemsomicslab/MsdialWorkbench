using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Rfx.Riken.OsakaUniv;
using Riken.Metabolomics.Lipidomics;
using Riken.Metabolomics.Pathwaymap;
using Riken.Metabolomics.Pathwaymap.Parser;
using Riken.PathwayView;

namespace Riken.Metabolomics.Msdial.Pathway {
    public sealed class MappingToPathways {
        private MappingToPathways() { }

        public static List<Riken.Metabolomics.Pathwaymap.Node> GetNodeObjects(MainWindow mainWindow, 
            float winPixelWidth, float winPixelHeight, float chartWidth, float chartHeight, float chartMargin, 
            string filterName = "") {

            var nodes = new List<Riken.Metabolomics.Pathwaymap.Node>();
            var startX = chartMargin;
            var startY = chartMargin;

            var alignmentResult = mainWindow.FocusedAlignmentResult;
            var alignedSpots = alignmentResult.AlignmentPropertyBeanCollection;
            var mspDB = mainWindow.MspDB;
            var files = mainWindow.AnalysisFiles;
            var isBoxPlot = mainWindow.ProjectProperty.IsBoxPlotForAlignmentResult;
            var projectProp = mainWindow.ProjectProperty;

            var counter = 0;
            foreach (var spot in alignedSpots) {
                var metabolite = spot.MetaboliteName;
                var ontology = GetMetaboliteOntology(mspDB, spot.LibraryID);
                if (filterName != "") { // now lipidomics only
                    if (metabolite == null || metabolite == string.Empty || metabolite.Contains("w/o")) continue;
                    //var lipidheader = metabolite.Split(' ')[0];
                    if (ontology != filterName) continue;
                    //var nameAray = metabolite.Split(';');
                    //if (nameAray.Length == 2) metabolite = nameAray[0];
                    //if (nameAray.Length == 3) metabolite = nameAray[1];
                    var nameAray = metabolite.Split('|');
                    if (nameAray.Length == 2) metabolite = nameAray[1];
                }
                var barchart = MsDialStatistics.GetBarChartBean(spot,
                          files, projectProp, mainWindow.BarChartDisplayMode, isBoxPlot);

                barchart.MainTitle = metabolite;
                barchart.XAxisTitle = "";
                var node = new Riken.Metabolomics.Pathwaymap.Node() {
                    X = startX,
                    Y = startY, 
                    Width = chartWidth,
                    Height = chartHeight,
                    ID = counter.ToString(),
                    Ontology = ontology,
                    Label = metabolite,
                    Key = metabolite,
                    NodeType = NodeType.Rectangle, 
                    BarChart = barchart
                };
                counter++;
                nodes.Add(node);

                if (counter % 4 == 0) {
                    startX = chartMargin;
                    startY += chartHeight + chartMargin;
                }
                else {
                    startX += chartWidth + chartMargin;
                }
            }

            return nodes;
        }

        public static string GetMetaboliteOntology(List<MspFormatCompoundInformationBean> mspDB, int libraryID) {
            var returnOnt = "null";
            if (libraryID < 0) return returnOnt;
            if (mspDB == null || mspDB.Count == 0) return returnOnt;
            if (mspDB.Count - 1 < libraryID) return returnOnt;
            returnOnt = mspDB[libraryID].CompoundClass != null && mspDB[libraryID].CompoundClass != string.Empty
                ? LipidomicsConverter.ConvertMsdialLbmStringToMsdialOfficialOntology(mspDB[libraryID].CompoundClass) : mspDB[libraryID].Ontology;
            if (returnOnt == null || returnOnt == string.Empty) returnOnt = "null";
            return returnOnt;
        }

        // X, Y are not defined
        public static Pathwaymap.Node AlignmentPropertyToNodeObject(AlignmentPropertyBean alignmentProp, 
            ObservableCollection<AnalysisFileBean> files, List<MspFormatCompoundInformationBean> mspDB,
            ProjectPropertyBean projectProp, BarChartDisplayMode displayMode, bool isBoxPlot) {
            var metabolite = alignmentProp.MetaboliteName;
            var ontology = GetMetaboliteOntology(mspDB, alignmentProp.LibraryID);
            var barchart = MsDialStatistics.GetBarChartBean(alignmentProp,
                      files, projectProp, displayMode, isBoxPlot);

            barchart.MainTitle = metabolite;
            barchart.XAxisTitle = "";
            var node = new Pathwaymap.Node() {
                Width = 200,
                Height = 150,
                Label = metabolite,
                Ontology = ontology,
                Key = metabolite,
                NodeType = NodeType.Rectangle,
                BarChart = barchart
            };

            return node;
        }
 
        public static List<Pathwaymap.Node> GetNodeObjects(List<Pathwaymap.Node> allNodes,
            float winPixelWidth, float winPixelHeight, float chartWidth, float chartHeight, float chartMargin,
            string filterName = "") {

            var nodes = new List<Riken.Metabolomics.Pathwaymap.Node>();
            var startX = chartMargin;
            var startY = chartMargin;

            var counter = 0;
            foreach (var nodeObj in allNodes) {
                var metabolite = nodeObj.Label;
                var ontology = nodeObj.Ontology;
                if (filterName == "All others") {
                    if (nodeObj.IsMapped == true) continue;
                }
                else if (filterName != "") { // now lipidomics only
                    if (metabolite == null || metabolite == string.Empty || metabolite.Contains("w/o")) continue;
                    //var lipidheader = metabolite.Split(' ')[0];
                    //if (lipidheader != filterName) continue;
                    if (ontology != filterName) continue;
                }
                counter++;
                nodeObj.ID = counter.ToString();
                nodeObj.X = startX;
                nodeObj.Y = startY;
                nodes.Add(nodeObj);

                if (counter % 4 == 0) {
                    startX = chartMargin;
                    startY += chartHeight + chartMargin;
                }
                else {
                    startX += chartWidth + chartMargin;
                }
            }

            return nodes;
        }

        public static void MappingToPathwayByInChIKey(string graphfile, MainWindow mainWindow, 
            ObservableCollection<AnalysisFileBean> otherFiles1 = null, AlignmentResultBean otherResult1 = null, List<MspFormatCompoundInformationBean> mspDB1 = null, List<PostIdentificatioinReferenceBean> textDB1 = null,
            ObservableCollection<AnalysisFileBean> otherFiles2 = null, AlignmentResultBean otherResult2 = null, List<MspFormatCompoundInformationBean> mspDB2 = null, List<PostIdentificatioinReferenceBean> textDB2 = null,
            ObservableCollection<AnalysisFileBean> otherFiles3 = null, AlignmentResultBean otherResult3 = null, List<MspFormatCompoundInformationBean> mspDB3 = null, List<PostIdentificatioinReferenceBean> textDB3 = null,
            ObservableCollection<AnalysisFileBean> otherFiles4 = null, AlignmentResultBean otherResult4 = null, List<MspFormatCompoundInformationBean> mspDB4 = null, List<PostIdentificatioinReferenceBean> textDB4 = null) {
            var selector = new FormatSelector();
            selector.ReadPathwayData(graphfile);

            var nodes = selector.Nodes;
            var edges = selector.Edges;
            if (nodes.Count == 0) return;

            MappingToPathwayByInChIKey(nodes, edges, mainWindow,
             otherFiles1, otherResult1, mspDB1, textDB1,
             otherFiles2, otherResult2, mspDB2, textDB2,
             otherFiles3, otherResult3, mspDB3, textDB3,
             otherFiles4, otherResult4, mspDB4, textDB4);
        }

        public static void MappingToPathwayByInChIKey(Stream graphfile, string extenstion, MainWindow mainWindow,
            ObservableCollection<AnalysisFileBean> otherFiles1 = null, AlignmentResultBean otherResult1 = null, List<MspFormatCompoundInformationBean> mspDB1 = null, List<PostIdentificatioinReferenceBean> textDB1 = null,
            ObservableCollection<AnalysisFileBean> otherFiles2 = null, AlignmentResultBean otherResult2 = null, List<MspFormatCompoundInformationBean> mspDB2 = null, List<PostIdentificatioinReferenceBean> textDB2 = null,
            ObservableCollection<AnalysisFileBean> otherFiles3 = null, AlignmentResultBean otherResult3 = null, List<MspFormatCompoundInformationBean> mspDB3 = null, List<PostIdentificatioinReferenceBean> textDB3 = null,
            ObservableCollection<AnalysisFileBean> otherFiles4 = null, AlignmentResultBean otherResult4 = null, List<MspFormatCompoundInformationBean> mspDB4 = null, List < PostIdentificatioinReferenceBean > textDB4 = null) {
            var selector = new FormatSelector();
            selector.ReadPathwayData(graphfile, extenstion);

            var nodes = selector.Nodes;
            var edges = selector.Edges;
            if (nodes.Count == 0) return;

            MappingToPathwayByInChIKey(nodes, edges, mainWindow,
              otherFiles1, otherResult1, mspDB1, textDB1,
              otherFiles2, otherResult2, mspDB2, textDB2,
              otherFiles3, otherResult3, mspDB3, textDB3,
              otherFiles4, otherResult4, mspDB4, textDB4);
        }

        private static void MappingToPathwayByInChIKey(List<Pathwaymap.Node> nodes, List<Pathwaymap.Edge> edges, MainWindow mainWindow, 
            ObservableCollection<AnalysisFileBean> otherFiles1, AlignmentResultBean otherResult1, List<MspFormatCompoundInformationBean> mspDB1, List<PostIdentificatioinReferenceBean> textDB1,
            ObservableCollection<AnalysisFileBean> otherFiles2, AlignmentResultBean otherResult2, List<MspFormatCompoundInformationBean> mspDB2, List<PostIdentificatioinReferenceBean> textDB2,
            ObservableCollection<AnalysisFileBean> otherFiles3, AlignmentResultBean otherResult3, List<MspFormatCompoundInformationBean> mspDB3, List<PostIdentificatioinReferenceBean> textDB3,
            ObservableCollection<AnalysisFileBean> otherFiles4, AlignmentResultBean otherResult4, List<MspFormatCompoundInformationBean> mspDB4, List<PostIdentificatioinReferenceBean> textDB4) {
            var alignmentResult = mainWindow.FocusedAlignmentResult;
            var alignedSpots = alignmentResult.AlignmentPropertyBeanCollection;
            var mspDB = mainWindow.MspDB;
            var textDB = mainWindow.PostIdentificationTxtDB;
            var files = mainWindow.AnalysisFiles;
            var allNodes = new List<Pathwaymap.Node>();
            var brushes = mainWindow.SolidColorBrushList;
            var displayMode = mainWindow.BarChartDisplayMode;
            var isBoxPlot = mainWindow.ProjectProperty.IsBoxPlotForAlignmentResult;
            var projectProp = mainWindow.ProjectProperty;

            setMetabolicPathwayProperties(nodes, alignedSpots, files, projectProp, displayMode, mspDB, textDB, allNodes, isBoxPlot);
            if (otherResult1 != null && otherResult1.AlignmentPropertyBeanCollection != null && otherResult1.AlignmentPropertyBeanCollection.Count != 0) {
                var otherSpots = otherResult1.AlignmentPropertyBeanCollection;
                setMetabolicPathwayProperties(nodes, otherSpots, otherFiles1, projectProp, displayMode, mspDB1, textDB1, allNodes, isBoxPlot);
            }
            if (otherResult2 != null && otherResult2.AlignmentPropertyBeanCollection != null && otherResult2.AlignmentPropertyBeanCollection.Count != 0) {
                var otherSpots = otherResult2.AlignmentPropertyBeanCollection;
                setMetabolicPathwayProperties(nodes, otherSpots, otherFiles2, projectProp, displayMode, mspDB2, textDB2, allNodes, isBoxPlot);
            }
            if (otherResult3 != null && otherResult3.AlignmentPropertyBeanCollection != null && otherResult3.AlignmentPropertyBeanCollection.Count != 0) {
                var otherSpots = otherResult3.AlignmentPropertyBeanCollection;
                setMetabolicPathwayProperties(nodes, otherSpots, otherFiles3, projectProp, displayMode, mspDB3, textDB3, allNodes, isBoxPlot);
            }
            if (otherResult4 != null && otherResult4.AlignmentPropertyBeanCollection != null && otherResult4.AlignmentPropertyBeanCollection.Count != 0) {
                var otherSpots = otherResult4.AlignmentPropertyBeanCollection;
                setMetabolicPathwayProperties(nodes, otherSpots, otherFiles4, projectProp, displayMode, mspDB4, textDB4, allNodes, isBoxPlot);
            }

            var pathwayObj = new PathwayMapObj(nodes, edges);
            var window = new PathwayMapWin(mainWindow, pathwayObj, allNodes);
            window.Owner = mainWindow;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.Show();
        }

        private static void setMetabolicPathwayProperties(List<Pathwaymap.Node> pathwayNodes, 
            ObservableCollection<AlignmentPropertyBean> alignedSpots, ObservableCollection<AnalysisFileBean> files, 
            ProjectPropertyBean projectProp, BarChartDisplayMode displayMode,
            List<MspFormatCompoundInformationBean> mspDB, 
            List<PostIdentificatioinReferenceBean> textDB, List<Pathwaymap.Node> allNodes, bool isBoxPlot) {
            foreach (var spot in alignedSpots) {
                var metabolite = spot.MetaboliteName;
                //Console.WriteLine(spot.AlignmentID + "\t" + spot.MetaboliteName);
                if (metabolite == null || metabolite == string.Empty || metabolite.Contains("w/o")) continue;
                var mspID = spot.LibraryID;
                var postTextID = spot.PostIdentificationLibraryID;
                if (mspID >= 0) {
                    //Console.WriteLine(mspID + "\t" + mspDB[mspID].InchiKey);
                }

                if ((mspDB == null || mspDB.Count == 0 || mspID > mspDB.Count - 1 || mspID < 0) && 
                    (textDB == null || textDB.Count == 0 || postTextID > textDB.Count - 1 || postTextID < 0)) continue;

                var inchikey = string.Empty;
                if (textDB != null && textDB.Count > 0 && postTextID < textDB.Count && postTextID >= 0) {
                    var textInChIKey = textDB[postTextID].Inchikey;
                    if (textInChIKey != null && textInChIKey != string.Empty && textInChIKey.Trim().Length == 27) {
                        inchikey = textInChIKey;
                    }
                }
                if (inchikey == string.Empty && mspDB != null && mspDB.Count > 0 && mspID < mspDB.Count && mspID >= 0) {
                    var mspInChIKey = mspDB[mspID].InchiKey;
                    if (mspInChIKey != null && mspInChIKey != string.Empty && mspInChIKey.Trim().Length == 27) {
                        inchikey = mspInChIKey;
                    }
                }
                if (inchikey == string.Empty) continue;
                var shortInChIKey = inchikey.Split('-')[0];
                var isMapped = false;
                foreach (var node in pathwayNodes) {
                    var nInChIkey = node.Key;
                    var nShortInChIkey = nInChIkey.Split('-')[0];
                    if ((node.IsStereoValidated && inchikey == nInChIkey) || 
                        (!node.IsStereoValidated && shortInChIKey == nShortInChIkey)) {
                        if (node.BarChart == null) {
                            var barchart = MsDialStatistics.GetBarChartBean(spot,
                                files, projectProp, displayMode, isBoxPlot);
                            barchart.XAxisTitle = "";
                            node.BarChart = barchart;
                        }
                        else { // replace barchart if max value is higher in new one.
                            var barchart = MsDialStatistics.GetBarChartBean(spot,
                               files, projectProp, displayMode, isBoxPlot);
                            barchart.XAxisTitle = "";
                            if (node.BarChart.MaxValue < barchart.MaxValue) {
                                node.BarChart = barchart;
                            }
                        }
                        node.IsMapped = true;
                        node.IsMappedByInChIKey = true;
                        isMapped = true;
                    }
                }
                var nodeObj = AlignmentPropertyToNodeObject(spot, files, mspDB, projectProp, displayMode, isBoxPlot);
                nodeObj.IsMapped = isMapped;
                nodeObj.IsMappedByInChIKey = true;
                allNodes.Add(nodeObj);

            }
        }

        public static void MappingToLipidPathway(string graphfile, MainWindow mainWindow, 
            ObservableCollection<AnalysisFileBean> otherFiles1 = null, AlignmentResultBean otherResult1 = null, List<MspFormatCompoundInformationBean> mspDB1 = null, List<PostIdentificatioinReferenceBean> textDB1 = null,
            ObservableCollection<AnalysisFileBean> otherFiles2 = null, AlignmentResultBean otherResult2 = null, List<MspFormatCompoundInformationBean> mspDB2 = null, List<PostIdentificatioinReferenceBean> textDB2 = null,
            ObservableCollection<AnalysisFileBean> otherFiles3 = null, AlignmentResultBean otherResult3 = null, List<MspFormatCompoundInformationBean> mspDB3 = null, List<PostIdentificatioinReferenceBean> textDB3 = null,
            ObservableCollection<AnalysisFileBean> otherFiles4 = null, AlignmentResultBean otherResult4 = null, List<MspFormatCompoundInformationBean> mspDB4 = null, List<PostIdentificatioinReferenceBean> textDB4 = null) {
            var selector = new FormatSelector();
            selector.ReadPathwayData(graphfile);
            var nodes = selector.Nodes;
            var edges = selector.Edges;
            if (nodes.Count == 0) return;

            MappingToLipidPathway(nodes, edges, mainWindow,
                otherFiles1, otherResult1, mspDB1,
                otherFiles2, otherResult2, mspDB2,
                otherFiles3, otherResult3, mspDB3,
                otherFiles4, otherResult4, mspDB4);
        }

        public static void PathwayMapping(Stream graphfile, string extension, MainWindow mainWindow, bool IsInChIKeyAsKey,
           ObservableCollection<AnalysisFileBean> otherFiles1 = null, AlignmentResultBean otherResult1 = null, List<MspFormatCompoundInformationBean> mspDB1 = null, List<PostIdentificatioinReferenceBean> textDB1 = null, bool IsInChIKeyAsKey1 = false,
           ObservableCollection<AnalysisFileBean> otherFiles2 = null, AlignmentResultBean otherResult2 = null, List<MspFormatCompoundInformationBean> mspDB2 = null, List<PostIdentificatioinReferenceBean> textDB2 = null, bool IsInChIKeyAsKey2 = false,
           ObservableCollection<AnalysisFileBean> otherFiles3 = null, AlignmentResultBean otherResult3 = null, List<MspFormatCompoundInformationBean> mspDB3 = null, List<PostIdentificatioinReferenceBean> textDB3 = null, bool IsInChIKeyAsKey3 = false,
           ObservableCollection<AnalysisFileBean> otherFiles4 = null, AlignmentResultBean otherResult4 = null, List<MspFormatCompoundInformationBean> mspDB4 = null, List<PostIdentificatioinReferenceBean> textDB4 = null, bool IsInChIKeyAsKey4 = false) {

            var selector = new FormatSelector();
            selector.ReadPathwayData(graphfile, extension);
            var nodes = selector.Nodes;
            var edges = selector.Edges;
            if (nodes.Count == 0) return;

            PathwayMapping(nodes, edges, mainWindow, IsInChIKeyAsKey,
              otherFiles1, otherResult1, mspDB1, textDB1, IsInChIKeyAsKey1,
              otherFiles2, otherResult2, mspDB2, textDB2, IsInChIKeyAsKey2,
              otherFiles3, otherResult3, mspDB3, textDB3, IsInChIKeyAsKey3,
              otherFiles4, otherResult4, mspDB4, textDB4, IsInChIKeyAsKey4);
        }

        public static void PathwayMapping(string graphfile, MainWindow mainWindow, bool IsInChIKeyAsKey,
          ObservableCollection<AnalysisFileBean> otherFiles1 = null, AlignmentResultBean otherResult1 = null, List<MspFormatCompoundInformationBean> mspDB1 = null, List<PostIdentificatioinReferenceBean> textDB1 = null, bool IsInChIKeyAsKey1 = false,
          ObservableCollection<AnalysisFileBean> otherFiles2 = null, AlignmentResultBean otherResult2 = null, List<MspFormatCompoundInformationBean> mspDB2 = null, List<PostIdentificatioinReferenceBean> textDB2 = null, bool IsInChIKeyAsKey2 = false,
          ObservableCollection<AnalysisFileBean> otherFiles3 = null, AlignmentResultBean otherResult3 = null, List<MspFormatCompoundInformationBean> mspDB3 = null, List<PostIdentificatioinReferenceBean> textDB3 = null, bool IsInChIKeyAsKey3 = false,
          ObservableCollection<AnalysisFileBean> otherFiles4 = null, AlignmentResultBean otherResult4 = null, List<MspFormatCompoundInformationBean> mspDB4 = null, List<PostIdentificatioinReferenceBean> textDB4 = null, bool IsInChIKeyAsKey4 = false) {

            var selector = new FormatSelector();
            selector.ReadPathwayData(graphfile);
            var nodes = selector.Nodes;
            var edges = selector.Edges;
            if (nodes.Count == 0) return;

            PathwayMapping(nodes, edges, mainWindow, IsInChIKeyAsKey,
               otherFiles1, otherResult1, mspDB1, textDB1, IsInChIKeyAsKey1,
               otherFiles2, otherResult2, mspDB2, textDB2, IsInChIKeyAsKey2,
               otherFiles3, otherResult3, mspDB3, textDB3, IsInChIKeyAsKey3,
               otherFiles4, otherResult4, mspDB4, textDB4, IsInChIKeyAsKey4);
        }

        private static void PathwayMapping(List<Pathwaymap.Node> nodes, List<Pathwaymap.Edge> edges, MainWindow mainWindow, bool isInChIKeyAsKey, 
            ObservableCollection<AnalysisFileBean> otherFiles1, AlignmentResultBean otherResult1, List<MspFormatCompoundInformationBean> mspDB1, List<PostIdentificatioinReferenceBean> textDB1, bool isInChIKeyAsKey1,
            ObservableCollection<AnalysisFileBean> otherFiles2, AlignmentResultBean otherResult2, List<MspFormatCompoundInformationBean> mspDB2, List<PostIdentificatioinReferenceBean> textDB2, bool isInChIKeyAsKey2, 
            ObservableCollection<AnalysisFileBean> otherFiles3, AlignmentResultBean otherResult3, List<MspFormatCompoundInformationBean> mspDB3, List<PostIdentificatioinReferenceBean> textDB3, bool isInChIKeyAsKey3, 
            ObservableCollection<AnalysisFileBean> otherFiles4, AlignmentResultBean otherResult4, List<MspFormatCompoundInformationBean> mspDB4, List<PostIdentificatioinReferenceBean> textDB4, bool isInChIKeyAsKey4) {

            var allNodes = new List<Pathwaymap.Node>();
            var alignmentResult = mainWindow.FocusedAlignmentResult;
            var alignedSpots = alignmentResult.AlignmentPropertyBeanCollection;
            var files = mainWindow.AnalysisFiles;
            var brushes = mainWindow.SolidColorBrushList;
            var displayMode = mainWindow.BarChartDisplayMode;
            var mspDB = mainWindow.MspDB;
            var textDB = mainWindow.PostIdentificationTxtDB;
            var project = mainWindow.ProjectProperty;
            var isBoxPlot = mainWindow.ProjectProperty.IsBoxPlotForAlignmentResult;

            // initialization
            var lipidNameToVariables = getLipidNameToVariables(files, alignedSpots[0].IonAbundanceUnit);
            if (isInChIKeyAsKey) {
                setMetabolicPathwayProperties(nodes, alignedSpots, files, project, displayMode, mspDB, textDB, allNodes, isBoxPlot);
            }
            else {
                setLipidPathwayProperties(lipidNameToVariables, alignedSpots, mspDB, files, project, displayMode, allNodes, isBoxPlot);
            }

            if (otherResult1 != null && otherResult1.AlignmentPropertyBeanCollection != null && otherResult1.AlignmentPropertyBeanCollection.Count != 0) {
                var otherSpots = otherResult1.AlignmentPropertyBeanCollection;
                if (isInChIKeyAsKey1) {
                    setMetabolicPathwayProperties(nodes, otherSpots, otherFiles1, project, displayMode, mspDB1, textDB1, allNodes, isBoxPlot);
                }
                else {
                    setLipidPathwayProperties(lipidNameToVariables, otherSpots, mspDB1, otherFiles1, project, displayMode, allNodes, isBoxPlot);
                }
            }

            if (otherResult2 != null && otherResult2.AlignmentPropertyBeanCollection != null && otherResult2.AlignmentPropertyBeanCollection.Count != 0) {
                var otherSpots = otherResult2.AlignmentPropertyBeanCollection;
                if (isInChIKeyAsKey2) {
                    setMetabolicPathwayProperties(nodes, otherSpots, otherFiles2, project, displayMode, mspDB2, textDB2, allNodes, isBoxPlot);
                }
                else {
                    setLipidPathwayProperties(lipidNameToVariables, otherSpots, mspDB2, otherFiles2, project, displayMode, allNodes, isBoxPlot);
                }
            }

            if (otherResult3 != null && otherResult3.AlignmentPropertyBeanCollection != null && otherResult3.AlignmentPropertyBeanCollection.Count != 0) {
                var otherSpots = otherResult3.AlignmentPropertyBeanCollection;
                if (isInChIKeyAsKey3) {
                    setMetabolicPathwayProperties(nodes, otherSpots, otherFiles3, project, displayMode, mspDB3, textDB3, allNodes, isBoxPlot);
                }
                else {
                    setLipidPathwayProperties(lipidNameToVariables, otherSpots, mspDB3, otherFiles3, project, displayMode, allNodes, isBoxPlot);
                }
            }

            if (otherResult4 != null && otherResult4.AlignmentPropertyBeanCollection != null && otherResult4.AlignmentPropertyBeanCollection.Count != 0) {
                var otherSpots = otherResult4.AlignmentPropertyBeanCollection;
                if (isInChIKeyAsKey4) {
                    setMetabolicPathwayProperties(nodes, otherSpots, otherFiles4, project, displayMode, mspDB4, textDB4, allNodes, isBoxPlot);
                }
                else {
                    setLipidPathwayProperties(lipidNameToVariables, otherSpots, mspDB4, otherFiles4, project, displayMode, allNodes, isBoxPlot);
                }
            }

            foreach (var node in nodes) {
                var key = node.Label;
                if (lipidNameToVariables.ContainsKey(key)) {
                    var barchart = MsDialStatistics.GetBarChartBean(lipidNameToVariables[key],
                        files, project, displayMode, isBoxPlot);
                    if (barchart.MaxValue <= 0) continue;
                    barchart.XAxisTitle = "";
                    node.BarChart = barchart;
                }
            }

            var pathwayObj = new PathwayMapObj(nodes, edges);
            var window = new PathwayMapWin(mainWindow, pathwayObj, allNodes);
            window.Owner = mainWindow;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.Show();
        }

        private static Dictionary<string, AlignmentPropertyBean> getLipidNameToVariables(ObservableCollection<AnalysisFileBean> files, IonAbundanceUnit unit) {
            // initialization
            var mappedClasses = LipidomicsConverter.GetLipidClasses();
            var lipidNameToVariables = new Dictionary<string, AlignmentPropertyBean>();
            foreach (var lipidclass in mappedClasses) {
                lipidNameToVariables[lipidclass] = new AlignmentPropertyBean() {
                    MetaboliteName = lipidclass,
                    IonAbundanceUnit = unit,
                    AlignedPeakPropertyBeanCollection = new ObservableCollection<AlignedPeakPropertyBean>()
                };
                foreach (var file in files) {
                    var fileProp = file.AnalysisFilePropertyBean;
                    lipidNameToVariables[lipidclass].AlignedPeakPropertyBeanCollection.Add(new AlignedPeakPropertyBean() {
                        FileID = fileProp.AnalysisFileId,
                        FileName = fileProp.AnalysisFileName,
                        AdductIonName = fileProp.AnalysisFileClass // temporally, adductionname fild is used for stroring class names
                    });
                }
            }
            return lipidNameToVariables;
        }

        public static void MappingToLipidPathway(Stream graphfile, string extension, MainWindow mainWindow,
           ObservableCollection<AnalysisFileBean> otherFiles1 = null, AlignmentResultBean otherResult1 = null, List<MspFormatCompoundInformationBean> mspDB1 = null,
           ObservableCollection<AnalysisFileBean> otherFiles2 = null, AlignmentResultBean otherResult2 = null, List<MspFormatCompoundInformationBean> mspDB2 = null,
           ObservableCollection<AnalysisFileBean> otherFiles3 = null, AlignmentResultBean otherResult3 = null, List<MspFormatCompoundInformationBean> mspDB3 = null,
           ObservableCollection<AnalysisFileBean> otherFiles4 = null, AlignmentResultBean otherResult4 = null, List<MspFormatCompoundInformationBean> mspDB4 = null) {
            var selector = new FormatSelector();
            selector.ReadPathwayData(graphfile, extension);
            var nodes = selector.Nodes;
            var edges = selector.Edges;
            if (nodes.Count == 0) return;

            MappingToLipidPathway(nodes, edges, mainWindow,
                otherFiles1, otherResult1, mspDB1,
                otherFiles2, otherResult2, mspDB2,
                otherFiles3, otherResult3, mspDB3,
                otherFiles4, otherResult4, mspDB4);
        }

        private static void MappingToLipidPathway(List<Pathwaymap.Node> nodes, List<Pathwaymap.Edge> edges, MainWindow mainWindow, 
            ObservableCollection<AnalysisFileBean> otherFiles1, AlignmentResultBean otherResult1, List<MspFormatCompoundInformationBean> mspDB1, 
            ObservableCollection<AnalysisFileBean> otherFiles2, AlignmentResultBean otherResult2, List<MspFormatCompoundInformationBean> mspDB2, 
            ObservableCollection<AnalysisFileBean> otherFiles3, AlignmentResultBean otherResult3, List<MspFormatCompoundInformationBean> mspDB3, 
            ObservableCollection<AnalysisFileBean> otherFiles4, AlignmentResultBean otherResult4, List<MspFormatCompoundInformationBean> mspDB4) {
            var allNodes = new List<Pathwaymap.Node>();
            var alignmentResult = mainWindow.FocusedAlignmentResult;
            var alignedSpots = alignmentResult.AlignmentPropertyBeanCollection;
            var mappedClasses = LipidomicsConverter.GetLipidClasses();
            var files = mainWindow.AnalysisFiles;
            var brushes = mainWindow.SolidColorBrushList;
            var displayMode = mainWindow.BarChartDisplayMode;
            var mspDB = mainWindow.MspDB;
            var isBoxPlot = mainWindow.ProjectProperty.IsBoxPlotForAlignmentResult;
            var project = mainWindow.ProjectProperty;

            // initialization
            var lipidNameToVariables = new Dictionary<string, AlignmentPropertyBean>();
            foreach (var lipidclass in mappedClasses) {
                lipidNameToVariables[lipidclass] = new AlignmentPropertyBean() {
                    MetaboliteName = lipidclass,
                    AlignedPeakPropertyBeanCollection = new ObservableCollection<AlignedPeakPropertyBean>()
                };
                foreach (var file in files) {
                    var fileProp = file.AnalysisFilePropertyBean;
                    lipidNameToVariables[lipidclass].AlignedPeakPropertyBeanCollection.Add(new AlignedPeakPropertyBean() {
                        FileID = fileProp.AnalysisFileId,
                        FileName = fileProp.AnalysisFileName,
                        AdductIonName = fileProp.AnalysisFileClass // temporally, adductionname fild is used for stroring class names
                    });
                }
            }

            setLipidPathwayProperties(lipidNameToVariables, alignedSpots, mspDB, files, project, displayMode, allNodes, isBoxPlot);
            if (otherResult1 != null && otherResult1.AlignmentPropertyBeanCollection != null && otherResult1.AlignmentPropertyBeanCollection.Count != 0) {
                var otherSpots = otherResult1.AlignmentPropertyBeanCollection;
                setLipidPathwayProperties(lipidNameToVariables, otherSpots, mspDB1, otherFiles1, project, displayMode, allNodes, isBoxPlot);
            }

            if (otherResult2 != null && otherResult2.AlignmentPropertyBeanCollection != null && otherResult2.AlignmentPropertyBeanCollection.Count != 0) {
                var otherSpots = otherResult2.AlignmentPropertyBeanCollection;
                setLipidPathwayProperties(lipidNameToVariables, otherSpots, mspDB2, otherFiles2, project, displayMode, allNodes, isBoxPlot);
            }

            if (otherResult3 != null && otherResult3.AlignmentPropertyBeanCollection != null && otherResult3.AlignmentPropertyBeanCollection.Count != 0) {
                var otherSpots = otherResult3.AlignmentPropertyBeanCollection;
                setLipidPathwayProperties(lipidNameToVariables, otherSpots, mspDB3, otherFiles3, project, displayMode, allNodes, isBoxPlot);
            }

            if (otherResult4 != null && otherResult4.AlignmentPropertyBeanCollection != null && otherResult4.AlignmentPropertyBeanCollection.Count != 0) {
                var otherSpots = otherResult4.AlignmentPropertyBeanCollection;
                setLipidPathwayProperties(lipidNameToVariables, otherSpots, mspDB4, otherFiles4, project, displayMode, allNodes, isBoxPlot);
            }

            foreach (var node in nodes) {
                var key = node.Label;
                if (lipidNameToVariables.ContainsKey(key)) {
                    var barchart = MsDialStatistics.GetBarChartBean(lipidNameToVariables[key],
                        files, project, displayMode, isBoxPlot);
                    if (barchart.MaxValue <= 0) continue;
                    barchart.XAxisTitle = "";
                    node.BarChart = barchart;
                    node.IsMapped = true;
                    node.IsMappedByInChIKey = false;
                }
            }

            var pathwayObj = new PathwayMapObj(nodes, edges);
            var window = new PathwayMapWin(mainWindow, pathwayObj, allNodes);
            window.Owner = mainWindow;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.Show();
        }

        private static void setLipidPathwayProperties(Dictionary<string, AlignmentPropertyBean> lipidNameToVariables, ObservableCollection<AlignmentPropertyBean> alignedSpots, 
            List<MspFormatCompoundInformationBean> mspDB, ObservableCollection<AnalysisFileBean> files, 
            ProjectPropertyBean project, BarChartDisplayMode displayMode, List<Pathwaymap.Node> allNodes, bool isBoxPlot,
            bool isTargetFile = false) {
            foreach (var spot in alignedSpots) {
                var lipid = spot.MetaboliteName;
                if (lipid == null || lipid == string.Empty || lipid.Contains("w/o")) continue;
               
                var libraryID = spot.LibraryID;
                var lipidClass = string.Empty;
                if (mspDB != null && mspDB.Count > libraryID && libraryID >= 0) {
                    lipidClass = mspDB[libraryID].CompoundClass;
                    var lbmClass = LipidomicsConverter.ConvertMsdialClassDefinitionToLbmClassEnumVS2(lipidClass);
                    lipidClass = LipidomicsConverter.ConvertLbmClassEnumToMsdialClassDefinitionVS2(lbmClass);
                }
                var isMapped = false;
                //var lipidheader = lipid.Split(' ')[0];
                //if (lipidNameToVariables.ContainsKey(lipidheader) || (lipidNameToVariables.ContainsKey(lipidClass) && !lipid.Contains("w/o"))) {
                if ((lipidNameToVariables.ContainsKey(lipidClass) && !lipid.Contains("w/o"))) {
                    //if (!lipidNameToVariables.ContainsKey(lipidClass)) {
                    //    lipidClass = lipidheader;
                    //}
                    var variables = lipidNameToVariables[lipidClass].AlignedPeakPropertyBeanCollection;

                    if (isTargetFile == true)
                        lipidNameToVariables[lipidClass].IonAbundanceUnit = spot.IonAbundanceUnit;

                    var peakCollection = spot.AlignedPeakPropertyBeanCollection;
                    var dones = new List<int>();

                    // to integrate other projects, the source code is a little bit complicated...
                    for (int i = 0; i < variables.Count; i++) {
                        for (int j = 0; j < peakCollection.Count; j++) {
                            if (dones.Contains(j)) continue;
                            var fileclass = files[j].AnalysisFilePropertyBean.AnalysisFileClass;
                            if (variables[i].AdductIonName == fileclass) { // adductionname now stores class name.
                                variables[i].Variable += peakCollection[j].Variable;
                                variables[i].NormalizedVariable += peakCollection[j].NormalizedVariable;
                                variables[i].Area += peakCollection[j].Area;
                                dones.Add(j);
                                break;
                            }
                        }
                    }
                    isMapped = true;
                }
                var nodeObj = AlignmentPropertyToNodeObject(spot, files, mspDB, project, displayMode, isBoxPlot);
                nodeObj.IsMapped = isMapped;
                allNodes.Add(nodeObj);
            }
        }
    }
}
