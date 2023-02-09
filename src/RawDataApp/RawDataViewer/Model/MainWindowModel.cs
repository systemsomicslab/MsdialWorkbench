using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using System.Collections.ObjectModel;
using System.IO;

namespace CompMs.App.RawDataViewer.Model
{
    public class MainWindowModel : DisposableModelBase
    {
        public MainWindowModel() {
            summarizedDataModels = new ObservableCollection<SummarizedDataModel>();
            SummarizedDataModels = new ReadOnlyObservableCollection<SummarizedDataModel>(summarizedDataModels);
        }

        public AnalysisFileBean File { 
            get => _file;
            set => SetProperty(ref _file, value);
        }
        private AnalysisFileBean _file;

        public MachineCategory MachineCategory {
            get => _machineCategory;
            set => SetProperty(ref _machineCategory, value);
        }
        private MachineCategory _machineCategory = MachineCategory.LCMS;

        public IonMode IonMode {
            get => _ionMode;
            set => SetProperty(ref _ionMode, value);
        }
        private IonMode _ionMode = IonMode.Positive;

        public ReadOnlyObservableCollection<SummarizedDataModel> SummarizedDataModels { get; }
        private readonly ObservableCollection<SummarizedDataModel> summarizedDataModels;

        public SummarizedDataModel SelectedSummarizedDataModel { 
            get => selectedSummarizedDataModel; 
            set => SetProperty(ref selectedSummarizedDataModel, value);
        }
        private SummarizedDataModel selectedSummarizedDataModel = null;

        public void AddAnalysisDataModel() {
            try {
                var model = new AnalysisDataModel(File, MachineCategory, IonMode, MachineCategory == MachineCategory.IFMS);
                var summarizedDataModel = new SummarizedDataModel(model);
                summarizedDataModels.Add(summarizedDataModel);
                SelectedSummarizedDataModel = summarizedDataModel;
            }
            catch (IOException ex) {
                System.Console.WriteLine(ex.Message);
            }
        }

        public void RemoveAnalysisDataModel() {
            var model = SelectedSummarizedDataModel;
            if (!(model is null) && SummarizedDataModels.Contains(model)) {
                summarizedDataModels.Remove(model);
            }
        }
    }
}
