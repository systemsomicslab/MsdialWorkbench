using CompMs.App.Msdial.Model.Setting;
using CompMs.CommonMVVM;
using System.Collections.ObjectModel;

namespace CompMs.App.Msdial.Model.Core
{
    public class ProjectModel : BindableBase, IProjectModel
    {
        public ObservableCollection<IDatasetModel> Datasets { get; }

        public DatasetSettingModel DatasetSetting {
            get {
                if (datasetSetting is null || datasetSetting.IsComplete) {
                    datasetSetting = new DatasetSettingModel(SetNewDataset);
                }
                return datasetSetting;
            }
        }
        private DatasetSettingModel datasetSetting;

        public IDatasetModel CurrentDataset {
            get => currentDataset;
            private set => SetProperty(ref currentDataset, value);
        }
        private IDatasetModel currentDataset;

        public void SetNewDataset(IDatasetModel dataset) {
            Datasets.Add(dataset);
            CurrentDataset = dataset;
        }
    }
}
