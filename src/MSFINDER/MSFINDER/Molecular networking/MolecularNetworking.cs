using Riken.Metabolomics.StructureFinder.Parser;
using Riken.Metabolomics.StructureFinder.Result;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Collections.ObjectModel;
using Msdial.Lcms.Dataprocess.Algorithm.Clustering;
using Newtonsoft.Json;
using Riken.Metabolomics.MsfinderCommon.Query;

namespace Rfx.Riken.OsakaUniv {
    public class MsfinderMolecularNetworking {
        #region // members
        private BackgroundWorker bgWorker;
        private ProgressBarWindow pbw;
        private string progressHeader = "Progress: ";

        private MsfinderQueryStorage msfinderQueryStorage;
        #endregion

        #region // Processing method summary
        public void ExportProcess(MainWindow mainWindow, MsfinderQueryStorage msfinderQueryStorage) {
            bgWorkerInitialize(mainWindow);
            this.bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgWorker_ExportRunWorkerCompleted);
            this.bgWorker.DoWork += new DoWorkEventHandler(bgWorker_ExportProcess_DoWork);
            this.bgWorker.RunWorkerAsync(new Object[] { mainWindow, msfinderQueryStorage });
        }

        public void CytoscapeVisualizationProcess(MainWindow mainWindow, MsfinderQueryStorage msfinderQueryStorage) {
            bgWorkerInitialize(mainWindow);
            this.bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgWorker_CytoscapeVisualizationRunWorkerCompleted);
            this.bgWorker.DoWork += new DoWorkEventHandler(bgWorker_CytoscapeVisualizationProcess_DoWork);
            this.bgWorker.RunWorkerAsync(new Object[] { mainWindow, msfinderQueryStorage });
        }
       
        private void bgWorkerInitialize(MainWindow mainWindow) {
            mainWindow.IsEnabled = false;
            Mouse.OverrideCursor = Cursors.Wait;

            this.pbw = new ProgressBarWindow();
            this.pbw.Owner = mainWindow;
            this.pbw.Title = "Generating nodes and edges for molecular network";
            this.pbw.ProgressBar_Label.Content = "0%";
            this.pbw.ProgressView.Minimum = 0;
            this.pbw.ProgressView.Maximum = 100;
            this.pbw.ProgressView.Value = 0;
            this.pbw.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            this.pbw.Show();

            this.bgWorker = new BackgroundWorker();
            this.bgWorker.WorkerReportsProgress = true;
            this.bgWorker.WorkerSupportsCancellation = true;
            this.bgWorker.ProgressChanged += new ProgressChangedEventHandler(bgWorker_ProgressChanged);
        }

        private void bgWorker_CytoscapeVisualizationProcess_DoWork(object sender, DoWorkEventArgs e) {
            var arg = (object[])e.Argument;

            var mainWindow = (MainWindow)arg[0];
            var msfinderQueryStorage = (MsfinderQueryStorage)arg[1];

            var targetFile = mainWindow.MainWindowVM.SelectedRawFileId;

            var files = msfinderQueryStorage.QueryFiles;
            var param = msfinderQueryStorage.AnalysisParameter;
            var targetFileID = targetFile >= 0 ? targetFile : 0;
            var mspQueries = getMspQueries(files, param, 25.0, targetFileID);
            var targetRawdata = RawDataParcer.RawDataFileReader(files[targetFileID].RawDataFilePath, param);
            var targetQuery = getMspFormatQuery(targetRawdata, files[targetFileID], targetFileID);

            var appPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var appDirectory = System.IO.Path.GetDirectoryName(appPath);
            var reactionFiles = System.IO.Directory.GetFiles(Path.Combine(appDirectory, "Resources"), "*.fbt", System.IO.SearchOption.TopDirectoryOnly);

            var msmsEdges = new List<EdgeInformation>();
            var ontologyEdges = new List<EdgeInformation>();
            var reactionEdges = new List<EdgeInformation>();

            if (param.IsMmnSelectedFileCentricProcess) {
                msmsEdges = MsmsClustering.GetEdgeInformations(targetQuery, mspQueries, param.MmnRelativeCutoff, param.MmnMassTolerance, param.MmnMassSimilarityCutOff, this.bgWorker, 25.0, 25.0);
                if (param.IsMmnOntologySimilarityUsed)
                    ontologyEdges = OntologyClustering.GetEdgeInformations(targetQuery, mspQueries, param.MmnOntologySimilarityCutOff, 25.0, 50.0, this.bgWorker);
                if (param.IsMmnFormulaBioreaction && reactionFiles.Length > 0)
                    reactionEdges = FormulaClustering.GetEdgeInformations(targetQuery, mspQueries, reactionFiles[0], 25.0, 75.0, this.bgWorker);
            }
            else {
                msmsEdges = MsmsClustering.GetEdgeInformations(mspQueries, param.MmnRelativeCutoff, param.MmnMassTolerance, param.MmnMassSimilarityCutOff, this.bgWorker, 25.0, 25.0);
                if (param.IsMmnOntologySimilarityUsed)
                    ontologyEdges = OntologyClustering.GetEdgeInformations(mspQueries, param.MmnOntologySimilarityCutOff, 25.0, 50.0, this.bgWorker);
                if (param.IsMmnFormulaBioreaction && reactionFiles.Length > 0)
                    reactionEdges = FormulaClustering.GetEdgeInformations(mspQueries, reactionFiles[0], 25.0, 75.0, this.bgWorker);
            }

            var rootObj = new RootObject();
            var cytoscapeNodes = getCytoscapeNodes(mspQueries);
            var edgeIds = new List<int>();
            var cytoscapeEdges = getCytoscapeEdges(msmsEdges, ontologyEdges, reactionEdges, out edgeIds);

            //final node checker
            var cCytoscapeNodes = new List<Node>();
            foreach (var node in cytoscapeNodes) {
                if (edgeIds.Contains(node.data.id))
                    cCytoscapeNodes.Add(node);
            }

            rootObj.nodes = cCytoscapeNodes;
            rootObj.edges = cytoscapeEdges;

            this.bgWorker.ReportProgress(100);
            e.Result = new object[] { mainWindow, rootObj };
        }

        private List<Edge> getCytoscapeEdges(List<EdgeInformation> msmsEdges, List<EdgeInformation> ontologyEdges, List<EdgeInformation> reactionEdges, out List<int> edgeIds) {
            var cytoscapeEdges = new List<Edge>();
            edgeIds = new List<int>();
            var edgeSpecMax = 4000;
            var edgeOntoMax = 2000;
            var edgeFormulaMax = 2000;

            var counter = 0;
            foreach (var edge in msmsEdges.OrderByDescending(n => n.Score)) {
                cytoscapeEdges.Add(new Edge() {
                    classes = "e_red",
                    data = new EdgeData() {
                        source = edge.SourceID,
                        target = edge.TargetID,
                        linecolor = "red",
                        comment = "MSMS similarity",
                        score = edge.Score
                    }
                });
                if (!edgeIds.Contains(edge.SourceID)) edgeIds.Add(edge.SourceID);
                if (!edgeIds.Contains(edge.TargetID)) edgeIds.Add(edge.TargetID);

                counter++;
                if (counter > edgeSpecMax) break;
            }

            counter = 0;
            foreach (var edge in ontologyEdges.OrderByDescending(n => n.Score)) {
                cytoscapeEdges.Add(new Edge() {
                    classes = "e_blue",
                    data = new EdgeData() {
                        source = edge.SourceID,
                        target = edge.TargetID,
                        linecolor = "blue",
                        comment = "Ontology similarity",
                        score = edge.Score
                    }
                });
                if (!edgeIds.Contains(edge.SourceID)) edgeIds.Add(edge.SourceID);
                if (!edgeIds.Contains(edge.TargetID)) edgeIds.Add(edge.TargetID);

                counter++;
                if (counter > edgeOntoMax) break;
            }

            counter = 0;
            foreach (var edge in reactionEdges) {
                cytoscapeEdges.Add(new Edge() {
                    classes = "e_yellow",
                    data = new EdgeData() {
                        source = edge.SourceID,
                        target = edge.TargetID,
                        linecolor = "yellow",
                        comment = edge.Comment,
                        score = edge.Score
                    }
                });
                if (!edgeIds.Contains(edge.SourceID)) edgeIds.Add(edge.SourceID);
                if (!edgeIds.Contains(edge.TargetID)) edgeIds.Add(edge.TargetID);

                counter++;
                if (counter > edgeFormulaMax) break;
            }

            return cytoscapeEdges;
        }

        private List<Node> getCytoscapeNodes(List<MspFormatCompoundInformationBean> mspQueries) {
            var cytoscapeNodes = new List<Node>();
            foreach (var query in mspQueries) {
                var node = new Node() {
                    //classes = "blue b_white hits",
                    data = new NodeData() {
                        id = query.Id,
                        Name = query.Instrument, // meaning metabolite name
                        Title = query.Name, // meaning file name
                        Rt = query.RetentionTime.ToString(),
                        Mz = query.PrecursorMz.ToString(),
                        Property = "RT=" + query.RetentionTime.ToString() + "; m/z=" + query.PrecursorMz.ToString(),
                        Adduct = query.AdductIonBean == null ? "Unknown" : query.AdductIonBean.AdductIonName,
                        IonMode = query.IonMode.ToString(),
                        Method = "MSMS",
                        Comment = query.Comment,
                        Formula = query.Formula,
                        InChiKey = query.InchiKey,
                        Ontology = query.Ontology,
                        Smiles = query.Smiles,
                        Size = 30,
                        bordercolor = "white",
                        backgroundcolor = query.CompoundClass
                    },
                };
                var ms2spec = new List<List<double>>();
                var ms2label = new List<string>();

                if (query.MzIntensityCommentBeanList != null && query.MzIntensityCommentBeanList.Count > 0) {
                    var maxIntensity = query.MzIntensityCommentBeanList.Max(n => n.Intensity);

                    foreach (var spec in query.MzIntensityCommentBeanList) {
                        ms2spec.Add(new List<double>() { spec.Mz, spec.Intensity });
                        ms2label.Add(Math.Round(spec.Mz, 4).ToString());
                    }
                    node.data.MsmsMin = ms2spec[0][0];
                }
                else {
                    node.data.MsmsMin = 0;
                }
                node.data.MSMS = ms2spec;
                node.data.MsMsLabel = ms2label;


                cytoscapeNodes.Add(node);
            }
            return cytoscapeNodes;
        }

        private void bgWorker_CytoscapeVisualizationRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            this.pbw.Close();

            object[] arg = (object[])e.Result;

            var mainWindow = (MainWindow)arg[0];
            var rootObj = (RootObject)arg[1];

            var json = JsonConvert.SerializeObject(rootObj, Formatting.Indented);
            var curDir = System.AppDomain.CurrentDomain.BaseDirectory;

            var exportpath = curDir + "CytoscapeLocal/elements.json";
            using (StreamWriter sw = new StreamWriter(exportpath, false, Encoding.ASCII)) {
                sw.WriteLine(json.ToString());
            }

            var url = curDir + "/CytoscapeLocal/MsfinderCytoscapeViewer.html";
            var error = false;
            try {
                System.Diagnostics.Process.Start(url);
            }
            catch (Win32Exception ex) {
                error = true;
            }
            catch (ObjectDisposedException ex) {
                error = true;
            }
            catch (FileNotFoundException ex) {
                error = true;
            }

            Mouse.OverrideCursor = null;
            mainWindow.IsEnabled = true;
        }

        private void bgWorker_ExportProcess_DoWork(object sender, DoWorkEventArgs e) {

            var arg = (object[])e.Argument;

            var mainWindow = (MainWindow)arg[0];
            var msfinderQueryStorage = (MsfinderQueryStorage)arg[1];
            var files = msfinderQueryStorage.QueryFiles;
            var param = msfinderQueryStorage.AnalysisParameter;
            var mspQueries = getMspQueries(files, param, 25.0, -1);
            var msmsEdges = MsmsClustering.GetEdgeInformations(mspQueries, param.MmnRelativeCutoff, param.MmnMassTolerance, param.MmnMassSimilarityCutOff, this.bgWorker, 25.0, 25.0);

            var ontologyEdges = new List<EdgeInformation>();
            if (param.IsMmnOntologySimilarityUsed)
                ontologyEdges = OntologyClustering.GetEdgeInformations(mspQueries, param.MmnOntologySimilarityCutOff, 25.0, 50.0, this.bgWorker);

            var appPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var appDirectory = System.IO.Path.GetDirectoryName(appPath);
            var reactionFiles = System.IO.Directory.GetFiles(Path.Combine(appDirectory, "Resources"), "*.fbt", System.IO.SearchOption.TopDirectoryOnly);

            var reactionEdges = new List<EdgeInformation>();
            if (param.IsMmnFormulaBioreaction && reactionFiles.Length > 0)
                reactionEdges = FormulaClustering.GetEdgeInformations(mspQueries, reactionFiles[0], 25.0, 75.0, this.bgWorker);

            var dt = DateTime.Now;
            var dtString = dt.Year + dt.Month + dt.Day + dt.Hour + dt.Minute + ".txt";
            var outputFolder = param.MmnOutputFolderPath;
            var nodepath = Path.Combine(outputFolder, "node-" + dtString);
            var edgepath = Path.Combine(outputFolder, "edge-" + dtString);

            using (StreamWriter sw = new StreamWriter(nodepath, false, Encoding.ASCII)) {

                sw.WriteLine("Title\tComment\tID\tPredicted metabolite\tRt\tMz\tAdduct\tFormula\tOntology\tInChIKey\tSMILES\tBackgroundColor\tMSMS");

                foreach (var query in mspQueries) {
                    var adduct = query.AdductIonBean != null ? query.AdductIonBean.AdductIonName : "Unknown";
                    sw.Write(query.Name + "\t" + query.Comment + "\t" + query.Id + "\t" + query.Instrument + "\t" + query.RetentionTime + "\t" +
                        query.PrecursorMz + "\t" + adduct + "\t" + query.Formula + "\t" + query.Ontology + "\t" +
                            query.InchiKey + "\t" + query.Smiles + "\t" + query.CompoundClass + "\t");

                    var ms2String = getMsString(query.MzIntensityCommentBeanList);
                    sw.WriteLine(ms2String);
                }
            }

            using (StreamWriter sw = new StreamWriter(edgepath, false, Encoding.ASCII)) {

                sw.WriteLine("Source (title)\tTarget (title)\tSource (Comment)\tTarget (Comment)\tSource (ID)\tTarget (ID)\tScore\tColor\tEdge information");
                foreach (var edge in msmsEdges) {
                    sw.WriteLine(edge.SourceName + "\t" + edge.TargetName
                        + "\t" + edge.SourceComment + "\t" + edge.TargetComment
                        + "\t" + edge.SourceID + "\t" + edge.TargetID
                        + "\t" + edge.Score + "\t" + "red" + "\t" + "MSMS similarity");
                }

                foreach (var edge in ontologyEdges) {
                    sw.WriteLine(edge.SourceName + "\t" + edge.TargetName 
                        + "\t" + edge.SourceComment + "\t" + edge.TargetComment 
                        + "\t" + edge.SourceID + "\t" + edge.TargetID
                        + "\t" + edge.Score + "\t" + "blue" + "\t" + "Ontology similarity");
                }

                foreach (var edge in reactionEdges) {
                    sw.WriteLine(edge.SourceName + "\t" + edge.TargetName 
                        + "\t" + edge.SourceComment + "\t" + edge.TargetComment
                        + "\t" + edge.SourceID + "\t" + edge.TargetID
                        + "\t" + edge.Score + "\t" + "gray" + "\t" + edge.Comment);
                }
            }

            this.bgWorker.ReportProgress(100);
            e.Result = new object[] { mainWindow };
        }

        private string getMsString(List<MzIntensityCommentBean> peaks) {
            if (peaks == null || peaks.Count == 0) return string.Empty;

            var specString = string.Empty;

            for (int i = 0; i < peaks.Count; i++) {
                var mz = peaks[i].Mz;
                var intensity = peaks[i].Intensity;

                if (i == peaks.Count - 1)
                    specString += Math.Round(mz, 5).ToString() + ":" + Math.Round(intensity, 0).ToString();
                else
                    specString += Math.Round(mz, 5).ToString() + ":" + Math.Round(intensity, 0).ToString() + " ";
            }

            return specString;
        }

        private List<MspFormatCompoundInformationBean> getMspQueries(ObservableCollection<MsfinderQueryFile> files, AnalysisParamOfMsfinder param, double maxProgress, int targetFileID) {
            var counter = 0;
            var mspQueries = new List<MspFormatCompoundInformationBean>();
            var importProgressMax = 25.0;

            RawData selectedFile = null;
            if (targetFileID >= 0)
                selectedFile = RawDataParcer.RawDataFileReader(files[targetFileID].RawDataFilePath, param);
            foreach (var file in files) {
                counter++;
                var rawData = RawDataParcer.RawDataFileReader(file.RawDataFilePath, param);
                if (targetFileID >= 0 && rawData.RetentionTime > 0 && Math.Abs(rawData.RetentionTime - selectedFile.RetentionTime) > param.MmnRtTolerance) {
                    continue;
                }
                var query = getMspFormatQuery(rawData, file, counter - 1);
                mspQueries.Add(query);

                var progress = (double)counter / (double)files.Count * importProgressMax;
                this.bgWorker.ReportProgress((int)progress);
            }
            return mspQueries;
        }

        private MspFormatCompoundInformationBean getMspFormatQuery(RawData rawData, MsfinderQueryFile file, int id) {
            var error = string.Empty;
            var formulaResults = FormulaResultParcer.FormulaResultFastReader(file.FormulaFilePath, out error);
            if (error != string.Empty) {
                Console.WriteLine(error);
            }
            var sfdResults = new List<FragmenterResult>();

            if (formulaResults.Count > 0) {
                formulaResults = formulaResults.OrderByDescending(n => n.TotalScore).ToList();
                var repFormula = formulaResults[0].Formula.FormulaString;

                var sfdFiles = System.IO.Directory.GetFiles(file.StructureFolderPath);

                foreach (var sfdFile in sfdFiles) {
                    var formulaString = System.IO.Path.GetFileNameWithoutExtension(sfdFile);
                    if (formulaString == repFormula) {
                        sfdResults = FragmenterResultParcer.FragmenterResultFastReader(sfdFile);
                    }
                }
            }

            if (sfdResults.Count > 0)
                sfdResults = sfdResults.OrderByDescending(n => n.TotalScore).ToList();

            var query = getMspFormatQuery(rawData, formulaResults, sfdResults, id);
            return query;
        }

        private MspFormatCompoundInformationBean getMspFormatQuery(RawData rawData, List<FormulaResult> formulaResults, List<FragmenterResult> sfdResults, int counter) {
            var filename = System.IO.Path.GetFileNameWithoutExtension(rawData.RawdataFilePath);
            var query = new MspFormatCompoundInformationBean() {
                Name = filename,
                PrecursorMz = (float)rawData.PrecursorMz,
                RetentionTime = (float)rawData.RetentionTime,
                Comment = rawData.Comment,
                IonMode = rawData.IonMode,
                Id = counter
            };
            if (rawData.ScanNumber >= 0) query.Id = rawData.ScanNumber;

            var adduct = AdductIonParcer.GetAdductIonBean(rawData.PrecursorType);
            if (adduct != null) {
                var adductIonBean = new AdductIonBean() {
                    AdductIonAccurateMass = (float)adduct.AdductIonAccurateMass,
                    AdductIonName = adduct.AdductIonName,
                    AdductIonXmer = adduct.AdductIonXmer,
                    ChargeNumber = adduct.ChargeNumber,
                    IonType = adduct.IonMode == IonMode.Positive ? IonType.Positive : IonType.Negative,
                    FormatCheck = adduct.FormatCheck
                };

                query.AdductIonBean = adductIonBean;
            }

            query.MzIntensityCommentBeanList = new List<MzIntensityCommentBean>();
            foreach (var peak in rawData.Ms2Spectrum.PeakList) {
                query.MzIntensityCommentBeanList.Add(new MzIntensityCommentBean() {
                     Mz = (float)peak.Mz, Intensity = (float)peak.Intensity
                });
            }
            query.PeakNumber = query.MzIntensityCommentBeanList.Count;

            if (formulaResults.Count > 0) {
                var result = formulaResults[0];
                query.Formula = result.Formula.FormulaString;
                if (query.Formula != null)
                    query.FormulaBean = FormulaStringParcer.OrganicElementsReader(query.Formula);
                if (result.ChemicalOntologyDescriptions.Count > 0)
                    query.Ontology = result.ChemicalOntologyDescriptions[0];
                else
                    query.Ontology = "Unknown";
            }
            else {
                query.Formula = "Unknown";
                query.Ontology = "Unknown";
            }

            if (sfdResults.Count > 0) {
                query.InchiKey = sfdResults[0].Inchikey;
                query.Smiles = sfdResults[0].Smiles;
                query.Ontology = sfdResults[0].Ontology;
                query.Instrument = sfdResults[0].Title; //temp

                if (MetaboliteColorCode.metabolite_colorcode.ContainsKey(query.Ontology))
                    query.CompoundClass = MetaboliteColorCode.metabolite_colorcode[query.Ontology]; // temp
            }
            else {
                query.InchiKey = "Unknown";
                query.Smiles = "Unknown";
                query.Instrument = "Unknown"; //temp
            }

            if (query.CompoundClass == null || query.CompoundClass == string.Empty)
                query.CompoundClass = "rgb(128, 128, 128)"; //gray

            return query;
        }

        private void bgWorker_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            this.pbw.ProgressView.Value = e.ProgressPercentage;
            this.pbw.ProgressBar_Label.Content = this.progressHeader + " " + e.ProgressPercentage + "%";
        }

        private void bgWorker_ExportRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            this.pbw.Close();
            var arg = (object[])e.Result;

            var mainWindow = (MainWindow)arg[0];

            Mouse.OverrideCursor = null;
            mainWindow.IsEnabled = true;
        }

        private string getMsString(List<List<double>> msList) {
            if (msList == null || msList.Count == 0) return string.Empty;

            var specString = string.Empty;

            for (int i = 0; i < msList.Count; i++) {
                var mz = msList[i][0];
                var intensity = msList[i][1];

                if (i == msList.Count - 1)
                    specString += Math.Round(mz, 5).ToString() + ":" + Math.Round(intensity, 0).ToString();
                else
                    specString += Math.Round(mz, 5).ToString() + ":" + Math.Round(intensity, 0).ToString() + " ";
            }

            return specString;
        }
        #endregion
    }
}
