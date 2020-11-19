using CompMs.Common.DataObj.Property;
using CompMs.Common.Parameter;
using CompMs.Common.Query;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.Common;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialLcmsApi.Parameter;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace CompMs.App.Msdial.LC
{
    class AnalysisParamSetForLcVM : ViewModelBase
    {
        #region Property
        public MsdialLcmsParameterVM Param {
            get => param;
            set => SetProperty(ref param, value);
        }

        public MsRefSearchParameterBaseVM MspSearchParam {
            get => mspSearchParam;
            set => SetProperty(ref mspSearchParam, value);
        }

        public MsRefSearchParameterBaseVM TextDbSearchParam {
            get => textDbSearchParam;
            set => SetProperty(ref textDbSearchParam, value);
        }

        public string AlignmentResultFileName {
            get => alignmentResultFileName;
            set => SetProperty(ref alignmentResultFileName, value);
        }

        public ObservableCollection<AnalysisFileBean> AnalysisFiles {
            get => analysisFiles;
            set => SetProperty(ref analysisFiles, value);
        }

        public string TargetFormulaLibraryFilePath {
            get => targetFormulaLibraryFilePath;
            set => SetProperty(ref targetFormulaLibraryFilePath, value);
        }

        public ObservableCollection<MzSearchQueryVM> ExcludedMassList {
            get => excludedMassList;
            set => SetProperty(ref excludedMassList, value);
        }

        public ObservableCollection<AdductIonVM> SearchedAdductIons {
            get => searchedAdductIons;
            set => SetProperty(ref searchedAdductIons, value);
        }

        #endregion

        #region Field
        MsdialLcmsParameter source;
        MsdialLcmsParameterVM param;
        MsRefSearchParameterBaseVM mspSearchParam, textDbSearchParam;
        string alignmentResultFileName;
        string targetFormulaLibraryFilePath;
        ObservableCollection<AnalysisFileBean> analysisFiles;
        ObservableCollection<MzSearchQueryVM> excludedMassList;
        ObservableCollection<AdductIonVM> searchedAdductIons;
        #endregion

        public AnalysisParamSetForLcVM(MsdialLcmsParameter parameter, IEnumerable<AnalysisFileBean> files) {
            source = parameter;
            Param = new MsdialLcmsParameterVM(parameter);
            MspSearchParam = new MsRefSearchParameterBaseVM(parameter.MspSearchParam);
            TextDbSearchParam = new MsRefSearchParameterBaseVM(parameter.TextDbSearchParam);

            var dt = DateTime.Now;
            AlignmentResultFileName = "AlignmentResult" + dt.ToString("_yyyy_MM_dd_hh_mm_ss");

            AnalysisFiles = new ObservableCollection<AnalysisFileBean>(files);

            ExcludedMassList = new ObservableCollection<MzSearchQueryVM>(
                parameter.ExcludedMassList?.Select(query => new MzSearchQueryVM { Mass=query.Mass, Tolerance=query.MassTolerance})
                         .Concat(Enumerable.Repeat<MzSearchQueryVM>(null, 200).Select(_ => new MzSearchQueryVM()))
            );
            SearchedAdductIons = new ObservableCollection<AdductIonVM>(parameter.SearchedAdductIons?.Select(ion => new AdductIonVM(ion)));
        }

        #region Command
        public DelegateCommand<Window> ContinueProcessCommand {
            get => continueProcessCommand ?? (continueProcessCommand = new DelegateCommand<Window>(ContinueProcess, ValidateAnalysisFilePropertySetWindow));
        }
        private DelegateCommand<Window> continueProcessCommand;

        private void ContinueProcess(Window window) {
            if (ClosingMethod()) {
                window.DialogResult = true;
                window.Close();
            }
        }

        private bool ClosingMethod() {
            source.ExcludedMassList = ExcludedMassList.Where(query => query.Mass.HasValue && query.Tolerance.HasValue && query.Mass > 0 && query.Tolerance > 0)
                                                      .Select(query => new MzSearchQuery { Mass = query.Mass.Value, MassTolerance = query.Tolerance.Value })
                                                      .ToList();
            return true;
        }

        private bool ValidateAnalysisFilePropertySetWindow(Window window) {
            return true;
        }

        public DelegateCommand<Window> CancelProcessCommand {
            get => cancelProcessCommand ?? (cancelProcessCommand = new DelegateCommand<Window>(CancelProcess));
        }
        private DelegateCommand<Window> cancelProcessCommand;

        private void CancelProcess(Window window) {
            window.DialogResult = false;
            window.Close();
        }

        public DelegateCommand<Window> AddAdductIonCommand {
            get => addAdductIonCommand ?? (addAdductIonCommand = new DelegateCommand<Window>(AddAdductIon));
        }
        private DelegateCommand<Window> addAdductIonCommand;

        private void AddAdductIon(Window owner) {
            var vm = new UserDefinedAdductSetVM();
            var window = new UserDefinedAdductSetWindow
            {
                DataContext = vm,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };

            if (window.ShowDialog() == true) {
                if (source.SearchedAdductIons == null)
                    source.SearchedAdductIons = new List<AdductIon>();
                source.SearchedAdductIons.Add(vm.AdductIon);
                SearchedAdductIons.Add(new AdductIonVM(vm.AdductIon));
            }
        }
        #endregion
    }

    class MsdialLcmsParameterVM : DynamicViewModelBase<MsdialLcmsParameter>
    {
        public MsdialLcmsParameterVM(MsdialLcmsParameter innerModel) : base(innerModel) { }
    }

    class MsRefSearchParameterBaseVM : DynamicViewModelBase<MsRefSearchParameterBase>
    {
        public MsRefSearchParameterBaseVM(MsRefSearchParameterBase innerModel) : base(innerModel) { }
    }

    public class MzSearchQueryVM : ViewModelBase
    {
        public double? Mass {
            get => mass;
            set => SetProperty(ref mass, value);
        }
        public double? Tolerance {
            get => tolerance;
            set => SetProperty(ref tolerance, value);
        }

        private double? mass, tolerance;
    }

    class AdductIonVM : DynamicViewModelBase<AdductIon>
    {
        public AdductIonVM(AdductIon innerModel) : base(innerModel) { }
    }
}
