using CompMs.App.Msdial.Model.Setting;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Parameter;
using System.Collections.ObjectModel;

namespace CompMs.App.Msdial.Model.Core
{
    public class ProjectModel : BindableBase, IProjectModel
    {
        public ProjectModel(ProjectParameter parameter) {
            Parameter = parameter;
            Datasets = new ObservableCollection<IDatasetModel>();
            DatasetSettingModel = new DatasetSettingModel(false, SetNewDataset);
        }

        public ProjectParameter Parameter { get; }

        public ObservableCollection<IDatasetModel> Datasets { get; }

        public DatasetSettingModel DatasetSettingModel {
            get => datasetSettingModel;
            private set => SetProperty(ref datasetSettingModel, value);
        }
        private DatasetSettingModel datasetSettingModel;

        public IDatasetModel CurrentDataset {
            get => currentDataset;
            private set => SetProperty(ref currentDataset, value);
        }
        private IDatasetModel currentDataset;

        public void SetNewDataset(IDatasetModel dataset) {
            Datasets.Add(dataset);
            CurrentDataset = dataset;
            DatasetSettingModel = new DatasetSettingModel(false, SetNewDataset);
        }
    }
}
