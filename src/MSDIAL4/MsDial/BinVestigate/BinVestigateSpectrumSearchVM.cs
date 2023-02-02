using Msdial.Gcms.Dataprocess.Algorithm;
using Riken.Metabolomics.BinVestigate;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Rfx.Riken.OsakaUniv
{
    public class BinVestigateSpectrumSearchVM : ViewModelBase
    {
        private BinVestigateSpectrumSearchWin binVestigateWin;

        private ObservableCollection<MS1DecResult> ms1DecResults;
        private MS1DecResult ms1DecResult;
        private double retentionIndex;
        private double riTolerance;
        private double threshold;
        private List<Peak> peaks;
        private string binvestigateLink;

        private ObservableCollection<BinBaseSpectrum> binBaseSpectrumRecords;
        private BinBaseSpectrum selectedBinBaseSpectrum;

        public string BinvestigateLink
        { get { return binvestigateLink; }
            set { if (value == binvestigateLink) return;
                binvestigateLink = value;
                OnPropertyChanged("BinvestigateLink");
            }
        }

        public BinVestigateSpectrumSearchVM(MS1DecResult ms1DecResult)
        {
            this.ms1DecResults = new ObservableCollection<MS1DecResult>() { ms1DecResult };
            this.ms1DecResult = ms1DecResult;
            this.threshold = 700;
            this.peaks = this.ms1DecResult.Spectrum;

            if (this.ms1DecResult.RetentionIndex < 0) {
                MessageBox.Show("Your project does not have retention index information. Therefore, this program tentatively enters '2500' as the RI value.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                this.retentionIndex = 2500;
                this.riTolerance = 200;
            }
            else {
                this.retentionIndex = ms1DecResult.RetentionIndex;
                if (this.retentionIndex > 200000) this.riTolerance = 2000; //estimated as Fiehn RI
                else this.riTolerance = 10; //estimated as Kovats RI
            }
            updateVM();
        }

        private void updateVM()
        {
            OnPropertyChanged("RiTolerance");
            OnPropertyChanged("Threshold");
            OnPropertyChanged("RetentionIndex");
            OnPropertyChanged("MS1DecResult");
        }

        private void updateCollectionVM()
        {
            OnPropertyChanged("BinBaseSpectrumRecords");
            OnPropertyChanged("SelectedBinBaseSpectrum");
        }

        private void updateSpectrumUI()
        {
            if (this.selectedBinBaseSpectrum == null) return;
            var expMassSpec = getMassSpectrogramBean(this.ms1DecResult);
            if (expMassSpec == null) return;

            var graphTitle = "EI spectra " + "Quant mass: " + Math.Round(ms1DecResult.BasepeakMz, 5).ToString();
            var refMassSpec = getReferenceSpectra(this.selectedBinBaseSpectrum);

            var specVM = new MassSpectrogramViewModel(expMassSpec, refMassSpec, MassSpectrogramIntensityMode.Relative, 0, (float)(Math.Round(this.retentionIndex, 2)), graphTitle);
            this.binVestigateWin.SpectrumUI.Content = new MassSpectrogramWithReferenceUI(specVM);

            this.binVestigateWin.Label_Name.Content = this.selectedBinBaseSpectrum.name;
            this.binVestigateWin.Label_Similarity.Content = this.selectedBinBaseSpectrum.BinVestigateSearchSimilarity;
            this.binVestigateWin.Label_Purity.Content = this.selectedBinBaseSpectrum.purity;
            this.binVestigateWin.Label_InChIKey.Content = this.selectedBinBaseSpectrum.inchikey;
            this.binVestigateWin.Label_Splash.Content = this.selectedBinBaseSpectrum.splash;
            BinvestigateLink = @"https://binvestigate.fiehnlab.ucdavis.edu/#/bin/" + this.selectedBinBaseSpectrum.id.ToString();

        }

        private MassSpectrogramBean getMassSpectrogramBean(MS1DecResult ms1DecResult)
        {
            var massSpectraDisplayLabelCollection = new ObservableCollection<MassSpectrogramDisplayLabel>();
            var masslist = new List<double[]>();

            if (ms1DecResult.Spectrum == null || ms1DecResult.Spectrum.Count == 0) return null;

            for (int i = 0; i < ms1DecResult.Spectrum.Count; i++)
                masslist.Add(new double[] { ms1DecResult.Spectrum[i].Mz, ms1DecResult.Spectrum[i].Intensity });

            masslist = masslist.OrderBy(n => n[0]).ToList();

            for (int i = 0; i < masslist.Count; i++)
                massSpectraDisplayLabelCollection.Add(new MassSpectrogramDisplayLabel() { Mass = masslist[i][0], Intensity = masslist[i][1], Label = Math.Round(masslist[i][0], 4).ToString() });

            return new MassSpectrogramBean(Brushes.Black, 2.0, new ObservableCollection<double[]>(masslist), massSpectraDisplayLabelCollection);
        }

        private MassSpectrogramBean getReferenceSpectra(BinBaseSpectrum binBaseSpectrum)
        {
            var masslist = new ObservableCollection<double[]>();
            var massSpectrogramDisplayLabelCollection = new ObservableCollection<MassSpectrogramDisplayLabel>();

            if (binBaseSpectrum.spectra == string.Empty)
                return new MassSpectrogramBean(Brushes.Red, 2.0, null);

            for (int i = 0; i < binBaseSpectrum.Peaks.Count; i++) {
                masslist.Add(new double[] { binBaseSpectrum.Peaks[i].Mz, binBaseSpectrum.Peaks[i].Intensity });
                massSpectrogramDisplayLabelCollection.Add(new MassSpectrogramDisplayLabel() { Mass = binBaseSpectrum.Peaks[i].Mz, Intensity = binBaseSpectrum.Peaks[i].Intensity, Label = binBaseSpectrum.Peaks[i].Mz.ToString() });
            }

            return new MassSpectrogramBean(Brushes.Red, 2.0, masslist, massSpectrogramDisplayLabelCollection);
        }

        #region properties
        public ObservableCollection<MS1DecResult> Ms1DecResults
        {
            get { return ms1DecResults; }
            set { if (ms1DecResults == value) return; ms1DecResults = value; OnPropertyChanged("MS1DecResults"); }
        }

        public double RiTolerance
        {
            get { return riTolerance; }
            set { if (riTolerance == value) return; riTolerance = value; OnPropertyChanged("RiTolerance"); }
        }

        public double Threshold
        {
            get { return threshold; }
            set { if (threshold == value) return; threshold = value; OnPropertyChanged("Threshold"); }
        }

        public double RetentionIndex
        {
            get { return retentionIndex; }
            set { if (retentionIndex == value) return; retentionIndex = value; OnPropertyChanged("RetentionIndex"); }
        }

        public ObservableCollection<BinBaseSpectrum> BinBaseSpectrumRecords
        {
            get { return binBaseSpectrumRecords; }
            set { if (binBaseSpectrumRecords == value) return; binBaseSpectrumRecords = value; OnPropertyChanged("BinBaseSpectrumRecords"); }
        }

        public BinBaseSpectrum SelectedBinBaseSpectrum
        {
            get { return selectedBinBaseSpectrum; }
            set { if (selectedBinBaseSpectrum == value) return; selectedBinBaseSpectrum = value; OnPropertyChanged("SelectedBinBaseSpectrum"); updateSpectrumUI(); }
        }


        #endregion

        /// <summary>
        /// Sets up the view model for the BinVestigate window in InvokeCommandAction
        /// </summary>
        private DelegateCommand windowLoaded;
        public DelegateCommand WindowLoaded
        {
            get {
                return windowLoaded ?? (windowLoaded = new DelegateCommand(Window_Loaded, obj => { return true; }));
            }
        }

        private void Window_Loaded(object obj)
        {
            this.binVestigateWin = (BinVestigateSpectrumSearchWin)obj;
        }

        /// <summary>
        /// search BinBase spectra
        /// </summary>
        private DelegateCommand searchSpectrum;
        public DelegateCommand SearchSpectrum
        {
            get {
                return searchSpectrum ?? (searchSpectrum = new DelegateCommand(winobj => {

                    Mouse.OverrideCursor = Cursors.Wait;

                    var bvProtocol = new BinVestigateRestProtocol();
                    //var dummyRes = new ObservableCollection<BinBaseSpectrum>() {
                    //    new BinBaseSpectrum() {
                    //        id = "1", BinVestigateSearchSimilarity = 9.0, Peaks = new List<Peak>() { new Peak() { Mz = 85, Intensity = 100 } }
                    //    }
                    //};

                    //this.BinBaseSpectrumRecords = dummyRes;
                    //this.SelectedBinBaseSpectrum = dummyRes[0];
                    //BrowseStatistics.RaiseCanExecuteChanged();

                    //Mouse.OverrideCursor = null;

                    //return;

                    var bvResults
                        = bvProtocol.SimilaritySearch(this.retentionIndex, this.riTolerance, this.peaks, this.threshold);
                    if (bvResults == null || bvResults.Count == 0) {
                        MessageBox.Show("No result in BinBase.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else {
                        var spectrumRecords = new ObservableCollection<BinBaseSpectrum>();
                        foreach (var result in bvResults) {
                            if (result.bin >= 0) {
                                var binRecord = bvProtocol.GetBinBaseDiagnosis((int)result.bin);
                                binRecord.BinVestigateSearchSimilarity = result.similarity;
                                binRecord.Peaks = new List<Peak>();
                                var specArray = binRecord.spectra.Split(' ').ToArray();
                                foreach (var spec in specArray) {
                                    var mzInt = spec.Split(':').ToArray();
                                    var peak = new Peak() { Mz = int.Parse(mzInt[0]), Intensity = double.Parse(mzInt[1]) };
                                    binRecord.Peaks.Add(peak);
                                }
                                spectrumRecords.Add(binRecord);
                            }
                        }

                        if (spectrumRecords.Count == 0) {
                            MessageBox.Show("No result in BinBase.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else {
                            this.BinBaseSpectrumRecords = spectrumRecords;
                            this.SelectedBinBaseSpectrum = spectrumRecords[0];
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
        private bool CanSearchSpectrum(object arg)
        {
            if (this.HasViewError) return false;
            else return true;
        }

        /// <summary>
        /// 
        /// </summary>
        private DelegateCommand browseStatistics;
        public DelegateCommand BrowseStatistics
        {
            get {
                return browseStatistics ?? (browseStatistics = new DelegateCommand(winobj => {

                    if (this.selectedBinBaseSpectrum == null || this.selectedBinBaseSpectrum.id == string.Empty) return;
                    //Mouse.OverrideCursor = Cursors.Wait;

                    var bvProtocol = new BinVestigateRestProtocol();
                    var binID = 0;
                    if (int.TryParse(this.selectedBinBaseSpectrum.id, out binID)) {
                        var bvResults = bvProtocol.GetBinBaseQuantStatisticsResults(binID);
                        
                        if (bvResults == null || bvResults.Count == 0) {
                            MessageBox.Show("No result in BinBase.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                        }else {
                            var window = new BinVestigateStatisticsBrowserWin();
                            window.Owner = (BinVestigateSpectrumSearchWin)winobj;

                            var mpvm = new BinVestigateStatisticsBrowserVM(binID, bvResults);
                            window.DataContext = mpvm;

                            window.Show();
                        }
                    }

                    //Mouse.OverrideCursor = null;


                }, CanBrowseStatistics));
            }
        }

        private bool CanBrowseStatistics(object arg)
        {
            if (this.selectedBinBaseSpectrum == null || this.selectedBinBaseSpectrum.id == string.Empty) return false;
            else return true;
        }
    }
}
