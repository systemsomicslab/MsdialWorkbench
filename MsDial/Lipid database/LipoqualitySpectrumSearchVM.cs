using Msdial.Lcms.Dataprocess.Algorithm;
using Msdial.Lcms.Dataprocess.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Rfx.Riken.OsakaUniv {
    class LipoqualitySpectrumSearchVM : ViewModelBase {
        private LipoqualitySpectrumSearchWin lipoqualitySpectrumSearchWin;
        private ObservableCollection<AlignmentPropertyBean> targetSpots;
        private ObservableCollection<MS2DecResult> ms2DecResults;
        private MS2DecResult ms2DecResult;
        private double ms1Tolerance;
        private double ms2Tolerance;
        private double rtTolerance;
        private double threshold;
        private List<double[]> peaks;
        private string lipoqualityLink;

        private ObservableCollection<RawData> spectrumRecords;
        private RawData selectedSpectrum;

        public List<AlignmentPropertyBean> LipoqualityProperties { get; set; } = null;
        public List<AnalysisFileBean> LipoqualityFiles { get; set; } = null;
        public List<RawData> LipoqualitySpectrumRecords { get; set; } = null;

        public string LipoqualityLink {
            get { return lipoqualityLink; }
            set {
                if (value == lipoqualityLink) return;
                lipoqualityLink = value;
                OnPropertyChanged("LipoqualityLink");
            }
        }

        public LipoqualitySpectrumSearchVM(PeakAreaBean peakSpot,
            MS2DecResult ms2DecResult, List<AlignmentPropertyBean> lipoqualityProperties, List<RawData> lipoqualitySpectrumRecords, List<AnalysisFileBean> lipoqualityFiles) {

            this.targetSpots = new ObservableCollection<AlignmentPropertyBean>() { new AlignmentPropertyBean() {
                 AlignmentID = peakSpot.PeakID, MetaboliteName = peakSpot.MetaboliteName, CentralRetentionTime = peakSpot.RtAtPeakTop, CentralAccurateMass = peakSpot.AccurateMass, AverageValiable = peakSpot.IntensityAtPeakTop, LibraryID = peakSpot.LibraryID
            } };
            this.ms2DecResults = new ObservableCollection<MS2DecResult>() { ms2DecResult };
            this.ms2DecResult = ms2DecResult;
            this.rtTolerance = 2.0;
            this.ms1Tolerance = 0.01;
            this.ms2Tolerance = 0.05;
            this.threshold = 70;

            this.peaks = this.ms2DecResult.MassSpectra;
            this.LipoqualityProperties = lipoqualityProperties;
            this.LipoqualityFiles = lipoqualityFiles;
            this.LipoqualitySpectrumRecords = lipoqualitySpectrumRecords;

            updateVM();
        }

        public LipoqualitySpectrumSearchVM(AlignmentPropertyBean alignmentSpot,
            MS2DecResult ms2DecResult, List<AlignmentPropertyBean> lipoqualityProperties, List<RawData> lipoqualitySpectrumRecords, List<AnalysisFileBean> lipoqualityFiles) {

            this.targetSpots = new ObservableCollection<AlignmentPropertyBean>() { alignmentSpot };
            this.ms2DecResults = new ObservableCollection<MS2DecResult>() { ms2DecResult };
            this.ms2DecResult = ms2DecResult;
            this.rtTolerance = 2.0;
            this.ms1Tolerance = 0.01;
            this.ms2Tolerance = 0.05;
            this.threshold = 70;

            this.peaks = this.ms2DecResult.MassSpectra;
            this.LipoqualityProperties = lipoqualityProperties;
            this.LipoqualityFiles = lipoqualityFiles;
            this.LipoqualitySpectrumRecords = lipoqualitySpectrumRecords;

            updateVM();
        }

        private void updateVM() {
            OnPropertyChanged("RtTolerance");
            OnPropertyChanged("Ms1Tolerance");
            OnPropertyChanged("Ms2Tolerance");
            OnPropertyChanged("Threshold");
            OnPropertyChanged("MS2DecResult");
            OnPropertyChanged("TargetSpots");
        }

        private void updateCollectionVM() {
            OnPropertyChanged("SpectrumRecords");
            OnPropertyChanged("SelectedSpectrum");
        }

        private void updateSpectrumUI() {
            if (this.selectedSpectrum == null) return;
            var expMassSpec = getMassSpectrogramBean(this.ms2DecResult);
            if (expMassSpec == null) return;

            var graphTitle = "MS/MS spectra of " + "m/z: " + Math.Round(ms2DecResult.Ms1AccurateMass, 5).ToString();
            var refMassSpec = getReferenceSpectra(this.selectedSpectrum);

            var specVM = new MassSpectrogramViewModel(expMassSpec, refMassSpec, MassSpectrogramIntensityMode.Relative, 0, (float)(Math.Round(this.ms1Tolerance, 2)), graphTitle);
            this.lipoqualitySpectrumSearchWin.SpectrumUI.Content = new MassSpectrogramWithReferenceUI(specVM);

            this.lipoqualitySpectrumSearchWin.Label_Name.Content = this.selectedSpectrum.MetaboliteName;
            this.lipoqualitySpectrumSearchWin.Label_Similarity.Content = this.selectedSpectrum.Similarity; // temporay, authors are used for similarity
            this.lipoqualitySpectrumSearchWin.Label_Comment.Content = this.selectedSpectrum.Comment;
            this.lipoqualitySpectrumSearchWin.Label_InChIKey.Content = this.selectedSpectrum.InchiKey;

            var metabolitename = this.selectedSpectrum.MetaboliteName;
            if (metabolitename == "Unknown") return;
            var queryAsMSP = getQueryAsMSP(metabolitename);
            var url = DatabaseLcUtility.GetLipoqualityDatabaseURL(queryAsMSP);
            LipoqualityLink = url;

        }

        private MspFormatCompoundInformationBean getQueryAsMSP(string metabolitename) {
            var mspQuery = new MspFormatCompoundInformationBean() {
                Name = metabolitename,
                CompoundClass = metabolitename.Split(' ')[0],
                AdductIonBean = new AdductIonBean() { AdductIonName = "[M+H]+" }
            };
            return mspQuery;
        }

        private MassSpectrogramBean getMassSpectrogramBean(MS2DecResult ms2DecResult) {
            var massSpectraDisplayLabelCollection = new ObservableCollection<MassSpectrogramDisplayLabel>();
            var masslist = new List<double[]>();

            if (ms2DecResult.MassSpectra == null || ms2DecResult.MassSpectra.Count == 0) return null;

            for (int i = 0; i < ms2DecResult.MassSpectra.Count; i++)
                masslist.Add(new double[] { ms2DecResult.MassSpectra[i][0], ms2DecResult.MassSpectra[i][1] });

            masslist = masslist.OrderBy(n => n[0]).ToList();

            for (int i = 0; i < masslist.Count; i++)
                massSpectraDisplayLabelCollection.Add(new MassSpectrogramDisplayLabel() { Mass = masslist[i][0], Intensity = masslist[i][1], Label = Math.Round(masslist[i][0], 4).ToString() });

            return new MassSpectrogramBean(Brushes.Black, 2.0, new ObservableCollection<double[]>(masslist), massSpectraDisplayLabelCollection);
        }

        private MassSpectrogramBean getReferenceSpectra(RawData rawData) {
            var masslist = new ObservableCollection<double[]>();
            var massSpectrogramDisplayLabelCollection = new ObservableCollection<MassSpectrogramDisplayLabel>();

            if (rawData == null || rawData.Ms2Spectrum == null || 
                rawData.Ms2Spectrum.PeakList == null || rawData.Ms2Spectrum.PeakList.Count == 0)
                return new MassSpectrogramBean(Brushes.Red, 2.0, null);

            var peaks = rawData.Ms2Spectrum.PeakList;
            for (int i = 0; i < peaks.Count; i++) {
                masslist.Add(new double[] { peaks[i].Mz, peaks[i].Intensity });
                massSpectrogramDisplayLabelCollection.Add(
                    new MassSpectrogramDisplayLabel() {
                        Mass = peaks[i].Mz,
                        Intensity = peaks[i].Intensity,
                        Label = peaks[i].Mz.ToString() });
            }

            return new MassSpectrogramBean(Brushes.Red, 2.0, masslist, massSpectrogramDisplayLabelCollection);
        }

        #region properties
        public ObservableCollection<MS2DecResult> Ms2DecResults {
            get { return ms2DecResults; }
            set { if (ms2DecResults == value) return; ms2DecResults = value; OnPropertyChanged("MS2DecResults"); }
        }

        public double RtTolerance {
            get { return rtTolerance; }
            set { if (rtTolerance == value) return; rtTolerance = value; OnPropertyChanged("RtTolerance"); }
        }

        public double Threshold {
            get { return threshold; }
            set { if (threshold == value) return; threshold = value; OnPropertyChanged("Threshold"); }
        }

        public double Ms1Tolerance {
            get { return ms1Tolerance; }
            set { if (ms1Tolerance == value) return; ms1Tolerance = value; OnPropertyChanged("Ms1Tolerance"); }
        }

        public double Ms2Tolerance {
            get { return ms2Tolerance; }
            set { if (ms2Tolerance == value) return; ms2Tolerance = value; OnPropertyChanged("Ms2Tolerance"); }
        }

        public ObservableCollection<RawData> SpectrumRecords {
            get { return spectrumRecords; }
            set { if (spectrumRecords == value) return; spectrumRecords = value; OnPropertyChanged("SpectrumRecords"); }
        }

        public RawData SelectedSpectrum {
            get { return selectedSpectrum; }
            set { if (selectedSpectrum == value) return;
                selectedSpectrum = value;
                OnPropertyChanged("SelectedSpectrum");
                updateSpectrumUI();
            }
        }


        #endregion

        /// <summary>
        /// Sets up the view model for the BinVestigate window in InvokeCommandAction
        /// </summary>
        private DelegateCommand windowLoaded;
        public DelegateCommand WindowLoaded {
            get {
                return windowLoaded ?? (windowLoaded = new DelegateCommand(Window_Loaded, obj => { return true; }));
            }
        }

        private void Window_Loaded(object obj) {
            this.lipoqualitySpectrumSearchWin = (LipoqualitySpectrumSearchWin)obj;
        }

        /// <summary>
        /// search BinBase spectra
        /// </summary>
        private DelegateCommand searchSpectrum;
        public DelegateCommand SearchSpectrum {
            get {
                return searchSpectrum ?? (searchSpectrum = new DelegateCommand(winobj => {

                    Mouse.OverrideCursor = Cursors.Wait;

                    var lqResults
                        = SpectralSimilarity.GetHighSimilarityQueries(this.ms2DecResult.Ms1AccurateMass, (float)this.ms1Tolerance,
                        this.ms2DecResult.PeakTopRetentionTime, (float)this.rtTolerance, (float)this.ms2Tolerance, this.ms2DecResult.MassSpectra, this.LipoqualitySpectrumRecords, (float)this.threshold, TargetOmics.Metablomics, true);
                    if (lqResults == null || lqResults.Count == 0) {
                        MessageBox.Show("No result in LipoQuality DB.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else {
                        var spectrumRecords = new ObservableCollection<RawData>();
                        foreach (var result in lqResults) {
                            spectrumRecords.Add(result);
                        }

                        if (spectrumRecords.Count == 0) {
                            MessageBox.Show("No result in BinBase.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else {
                            this.SpectrumRecords = spectrumRecords;
                            this.SelectedSpectrum = spectrumRecords[0];
                        }
                    }
                    BrowseStatistics.RaiseCanExecuteChanged();
                    Mouse.OverrideCursor = null;


                }, CanSearchSpectrum));
            }
        }

        /// <summary>
        /// Checks whether the BinVestigate search can be executed or not
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private bool CanSearchSpectrum(object arg) {
            if (this.HasViewError) return false;
            else return true;
        }

        /// <summary>
        /// 
        /// </summary>
        private DelegateCommand browseStatistics;
        public DelegateCommand BrowseStatistics {
            get {
                return browseStatistics ?? (browseStatistics = new DelegateCommand(winobj => {

                    if (this.selectedSpectrum == null || this.selectedSpectrum.Name == string.Empty) return;
                    Mouse.OverrideCursor = Cursors.Wait;
                    var lqResults = getLipoqualityStatistics(this.selectedSpectrum, this.LipoqualityProperties, this.LipoqualityFiles);
                    if (lqResults == null || lqResults.Count == 0) {
                        MessageBox.Show("No result in LipoQuality database.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else {
                        var window = new LipoqualityStatisticsBrowserWin();
                        window.Owner = (LipoqualitySpectrumSearchWin)winobj;

                        var mpvm = new LipoqualityStatisticsBrowserVM(this.selectedSpectrum.Name, lqResults);
                        window.DataContext = mpvm;

                        window.Show();
                    }

                    Mouse.OverrideCursor = null;


                }, CanBrowseStatistics));
            }
        }

        public ObservableCollection<AlignmentPropertyBean> TargetSpots {
            get {
                return targetSpots;
            }

            set {
                targetSpots = value;
            }
        }

        private LipoqualityQuantTree getLipoqualityStatistics(RawData selectedSpectrum, List<AlignmentPropertyBean> lipoqualityProperties, List<AnalysisFileBean> lipoqualityFiles) {
            var selectedProperty = new AlignmentPropertyBean();
            foreach (var prop in lipoqualityProperties) {
                if (prop.MasterIdString == selectedSpectrum.Name) {
                    selectedProperty = prop;
                    break;
                }
            }

            var fileIdToSuperClass = getFileIdToSuperClass(lipoqualityFiles);
            var fileIdToClass = getFileIdToClass(lipoqualityFiles);

            var quantTree = new LipoqualityQuantTree();
            var properties = selectedProperty.AlignedPeakPropertyBeanCollection;
            var files = new ObservableCollection<AnalysisFileBean>(lipoqualityFiles);
            var superClassToValues = MsDialStatistics.GetFileClassAndValueDictionary(selectedProperty, files, fileIdToSuperClass, BarChartDisplayMode.OriginalHeight);
            var classToValues = MsDialStatistics.GetFileClassAndValueDictionary(selectedProperty, files, fileIdToClass, BarChartDisplayMode.OriginalHeight);
            var classToCount = getClassToCount(files);
            var superclassToCount = getSuperClassToCount(files);

            var studyCount = 0.0;
            var intensityCount = 0.0;

            foreach (var superclassDict in superClassToValues) {
                var superclassName = superclassDict.Key;
                var superclassValues = superclassDict.Value;
                if (superclassValues == null || superclassValues.Count == 0) continue;
                var superclasAverage = superclassValues.Average();

                var superLayer = new LipoqualityQuantTree() {
                    ClassName = superclassName,
                    StudyCount = superclassToCount[superclassName],
                    Intensity = superclasAverage,
                    Error = 0.0,
                    Description = superclassName + " Study count: " + superclassToCount[superclassName] + " Intensity: " + Math.Round(superclasAverage, 0),
                    SubClass = new LipoqualityQuantTree()
                };

                foreach (var classDict in classToValues) {
                    var className = classDict.Key;
                    var classValues = classDict.Value;
                    if (!className.Contains(superclassName)) continue;
                    if (classValues == null || classValues.Count == 0) continue;

                    var classAverage = classValues.Average();
                    var stdev = 0.0;
                    if (classValues.Count > 1) {
                        var sumOfSquares = classValues.Select(val => (val - classAverage) * (val - classAverage)).Sum();
                        stdev = Math.Sqrt(sumOfSquares / (double)(classValues.Count - 1));
                    }

                    var layer = new LipoqualityQuantTree() {
                        ClassName = className,
                        StudyCount = classToCount[className],
                        Intensity = classAverage,
                        Error = stdev,
                        Description = className + " Study count: " + classToCount[className] + " Intensity: " + Math.Round(classAverage, 0),
                    };
                    //if (layer.Intensity < 1000) continue;
                    superLayer.SubClass.Add(layer);
                    studyCount += layer.StudyCount;
                    studyCount += layer.Intensity;
                }
                if (superLayer.Intensity < 1000) continue;
                quantTree.Add(superLayer);
            }

            quantTree.StudyCount = studyCount;
            quantTree.Intensity = intensityCount;
            quantTree.Description = selectedProperty.MasterIdString + " " + "Study count: " + studyCount + " Intensity: " + Math.Round(intensityCount, 0);

            return quantTree;
        }

        private Dictionary<string, int> getClassToCount(ObservableCollection<AnalysisFileBean> files) {
            var dict = new Dictionary<string, int>();
            foreach (var file in files) {
                if (dict.ContainsKey(file.AnalysisFilePropertyBean.AnalysisFileClass)) {
                    dict[file.AnalysisFilePropertyBean.AnalysisFileClass]++;
                }
                else {
                    dict[file.AnalysisFilePropertyBean.AnalysisFileClass] = 1;
                }
            }
            return dict;
        }

        private Dictionary<string, int> getSuperClassToCount(ObservableCollection<AnalysisFileBean> files) {
            var dict = new Dictionary<string, int>();
            foreach (var file in files) {
                if (dict.ContainsKey(file.AnalysisFilePropertyBean.AnalysisFileSuperClass)) {
                    dict[file.AnalysisFilePropertyBean.AnalysisFileSuperClass]++;
                }
                else {
                    dict[file.AnalysisFilePropertyBean.AnalysisFileSuperClass] = 1;
                }
            }
            return dict;
        }

        private Dictionary<int, string> getFileIdToSuperClass(List<AnalysisFileBean> files) {
            var dict = new Dictionary<int, string>();
            foreach (var file in files) {
                dict[file.AnalysisFilePropertyBean.AnalysisFileId] = file.AnalysisFilePropertyBean.AnalysisFileSuperClass;
            }
            return dict;
        }

        private Dictionary<int, string> getFileIdToClass(List<AnalysisFileBean> files) {
            var dict = new Dictionary<int, string>();
            foreach (var file in files) {
                dict[file.AnalysisFilePropertyBean.AnalysisFileId] = file.AnalysisFilePropertyBean.AnalysisFileClass;
            }
            return dict;
        }

        private bool CanBrowseStatistics(object arg) {
            if (this.selectedSpectrum == null || this.selectedSpectrum.Name == string.Empty) return false;
            else return true;
        }

    }
}
