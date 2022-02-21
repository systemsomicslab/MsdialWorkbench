using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.ObjectModel;

namespace CompMs.App.Msdial.Model.Core
{
    public class ProjectModel : BindableBase, IProjectModel
    {
        public ProjectModel(ProjectParameter parameter) {
            Parameter = parameter;
        }

        public ProjectParameter Parameter { get; }

        public ObservableCollection<IDatasetModel> Datasets { get; }

        public ProjectSettingModel ProjectSetting {
            get {
                if (projectSetting is null || projectSetting.IsComplete) {
                    projectSetting = new ProjectSettingModel(this);
                }
                return projectSetting;
            }
        }
        private ProjectSettingModel projectSetting;

        public ProjectSettingModel DatasetAllSetting {
            get {
                if (datasetAllSetting is null || datasetAllSetting.IsComplete) {
                    datasetAllSetting = new ProjectSettingModel(this, (DatasetModel)CurrentDataset, ProcessOption.All);
                }
                return datasetAllSetting;
            }
        }
        private ProjectSettingModel datasetAllSetting;

        public ProjectSettingModel DatasetFromAnnotationSetting {
            get {
                if (datasetFromAnnotationSetting is null || datasetFromAnnotationSetting.IsComplete) {
                    datasetFromAnnotationSetting = new ProjectSettingModel(this, (DatasetModel)CurrentDataset, ProcessOption.IdentificationPlusAlignment);
                }
                return datasetFromAnnotationSetting;
            }
        }
        private ProjectSettingModel datasetFromAnnotationSetting;

        public ProjectSettingModel DatasetFromAlignmentSetting {
            get {
                if (datasetFromAlignmentSetting is null || datasetFromAlignmentSetting.IsComplete) {
                    datasetFromAlignmentSetting = new ProjectSettingModel(this, (DatasetModel)CurrentDataset, ProcessOption.Alignment);
                }
                return datasetFromAlignmentSetting;
            }
        }
        private ProjectSettingModel datasetFromAlignmentSetting;


        public DatasetFileSettingModel DatasetFileSetting {
            get {
                if (datasetFileSetting is null) {
                    datasetFileSetting = new DatasetFileSettingModel(DateTime.UtcNow);
                    OnPropertyChanged(nameof(DatasetFileSetting));
                }
                return datasetFileSetting;
            }
        }
        private DatasetFileSettingModel datasetFileSetting;

        public DatasetParameterSettingModel DatasetParameterSetting {
            get {
                if (datasetParameterSetting is null || datasetParameterSetting.IsComplete) {
                    datasetParameterSetting = new DatasetParameterSettingModel(SetNewDataset, DatasetFileSetting);
                    OnPropertyChanged(nameof(DatasetParameterSetting));
                }
                return datasetParameterSetting;
            }
        }
        private DatasetParameterSettingModel datasetParameterSetting;

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
