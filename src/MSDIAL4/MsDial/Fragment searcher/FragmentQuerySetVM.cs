using Msdial.Lcms.Dataprocess.Searcher;
using Riken.Metabolomics.Common.Query;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;

namespace Rfx.Riken.OsakaUniv {
    public class FragmentQuerySetVM : ViewModelBase {

        private ObservableCollection<FragmentSearcherQuery> fragmentQueriesInView;
        private AnalysisParametersBean param;
        private MainWindow mainWindow;

        private bool isPeakSpots;
        private bool isAlignmentSpots;

        private bool isAndOption;
        private bool isOrOption;


        /// <summary>
        /// Sets up the view model for the FragmentQuerySet window
        /// </summary>
        private DelegateCommand windowLoaded;
        public DelegateCommand WindowLoaded {
            get {
                return windowLoaded ?? (windowLoaded = new DelegateCommand(Window_Loaded, obj => { return true; }));
            }
        }
        
        private void Window_Loaded(object obj) {
            var view = (FragmentQuerySetWin)obj;
            this.mainWindow = (MainWindow)view.Owner;
            this.param = this.mainWindow.AnalysisParamForLC;
            this.fragmentQueriesInView = new ObservableCollection<FragmentSearcherQuery>();
            if (this.mainWindow.TabItem_RtMzPairwisePlotAlignmentView.IsSelected)
                this.IsAlignmentSpots = true;
            else
                this.IsPeakSpots = true;

            for (int i = 0; i < 100; i++)
                this.fragmentQueriesInView.Add(new FragmentSearcherQuery());

            if (this.param.FragmentSearcherQueries == null) {
                this.param.FragmentSearcherQueries = new List<FragmentSearcherQuery>();
            }

            if (this.param.FragmentSearcherQueries.Count > 0) {
                copyFragmentQueries(this.param.FragmentSearcherQueries);
            }

            if (this.param.IsAndAsFragmentSearcherOption == true)
                this.IsAndOption = true;
            else
                this.IsOrOption = true;

            OnPropertyChanged("FragmentQueriesInView");
        }

        private void copyFragmentQueries(List<FragmentSearcherQuery> fragmentSearcherQueries) {

            var counter = 0;
            foreach (var query in fragmentSearcherQueries) {
                this.fragmentQueriesInView[counter].Mass = query.Mass;
                this.fragmentQueriesInView[counter].MassTolerance = query.MassTolerance;
                this.fragmentQueriesInView[counter].RelativeIntensity = query.RelativeIntensity;
                this.fragmentQueriesInView[counter].SearchType = query.SearchType;
                counter++;
                if (counter >= 100) break; 
            }
        }

        /// <summary>
		/// Searching fragment queries
		/// </summary>
		private DelegateCommand fragmentSearch;
        public DelegateCommand FragmentSearch {
            get {
                return fragmentSearch ?? (fragmentSearch = new DelegateCommand(winobj => {

                    var view = (FragmentQuerySetWin)winobj;

                    var queries = this.fragmentQueriesInView;
                    var cQueries = new List<FragmentSearcherQuery>();
                    foreach (var query in queries) {
                        if (query.Mass <= 0) continue;
                        if (query.RelativeIntensity <= 0) continue;
                        if (query.MassTolerance <= 0) continue;

                        cQueries.Add(query);
                    }

                    if (cQueries.Count == 0) {
                        MessageBox.Show("Enter at least one query", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    this.param.FragmentSearcherQueries = new List<FragmentSearcherQuery>();
                    foreach (var query in cQueries) {
                        this.param.FragmentSearcherQueries.Add(query);
                    }
                    if (this.isAndOption)
                        this.param.IsAndAsFragmentSearcherOption = true;
                    else
                        this.param.IsAndAsFragmentSearcherOption = false;

                    if (this.isPeakSpots) {
                        var fileid = this.mainWindow.FocusedFileID;
                        var peakSpots = this.mainWindow.AnalysisFiles[fileid].PeakAreaBeanCollection;
                        FragmentSearcher.SearchingFragmentQueries(peakSpots, this.mainWindow.PeakViewDecFS,
                            this.mainWindow.PeakViewDecSeekPoints, this.param);
                    }
                    else if (this.isAlignmentSpots) {
                        FragmentSearcher.SearchingFragmentQueries(this.mainWindow.FocusedAlignmentResult.AlignmentPropertyBeanCollection, 
                            this.mainWindow.AlignViewDecFS, this.mainWindow.AlignViewDecSeekPoints, this.param);
                    }

                    view.Close();
                }, CanFragmentSearch));
            }
        }

        /// <summary>
        /// Checks whether the fragment search command can be executed or not
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private bool CanFragmentSearch(object obj) {

            if (this.mainWindow == null) return true;

            var isSelectedFile = this.mainWindow.FocusedFileID;
            var isSelectedAlignmentFile = this.mainWindow.FocusedAlignmentFileID;

            if (this.isPeakSpots && isSelectedFile < 0) return false;
            if (this.isAlignmentSpots && isSelectedAlignmentFile < 0) return false;

            return true;
        }

        private DelegateCommand clearList;
        public DelegateCommand ClearList {
            get {
                return clearList ?? (clearList = new DelegateCommand(obj => {
                    this.fragmentQueriesInView = new ObservableCollection<FragmentSearcherQuery>();
                    for (int i = 0; i < 100; i++)
                        this.fragmentQueriesInView.Add(new FragmentSearcherQuery());
                    OnPropertyChanged("FragmentQueriesInView");
                }, obj => { return true; }));
            }
        }

        /// <summary>
		/// Closes the window (on Cancel)
		/// </summary>
		private DelegateCommand cancel;
        public DelegateCommand Cancel {
            get {
                return cancel ?? (cancel = new DelegateCommand(obj => {
                    Window view = (Window)obj;
                    view.Close();
                }, obj => { return true; }));
            }
        }

        public ObservableCollection<FragmentSearcherQuery> FragmentQueriesInView {
            get {
                return fragmentQueriesInView;
            }

            set {
                fragmentQueriesInView = value; OnPropertyChanged("FragmentQueriesInView");
            }
        }

        public bool IsPeakSpots {
            get {
                return isPeakSpots;
            }

            set {
                if (isPeakSpots == value) return;
                isPeakSpots = value;
                OnPropertyChanged("IsPeakSpots");
            }
        }

        public bool IsAlignmentSpots {
            get {
                return isAlignmentSpots;
            }

            set {
                if (isAlignmentSpots == value) return;
                isAlignmentSpots = value;
                OnPropertyChanged("IsAlignmentSpots");
            }
        }

        public bool IsAndOption {
            get {
                return isAndOption;
            }

            set {
                if (isAndOption == value) return;
                isAndOption = value;
                OnPropertyChanged("IsAndOption");
            }
        }

        public bool IsOrOption {
            get {
                return isOrOption;
            }

            set {
                if (isOrOption == value) return;
                isOrOption = value;
                OnPropertyChanged("IsOrOption");
            }
        }
    }
}
