using CompMs.App.Msdial.Model.Core;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using System;

namespace CompMs.App.Msdial.Model.Setting
{
    public class ProjectSettingModel : BindableBase
    {
        public ProjectSettingModel(Action<IProjectModel> continuous) {
            ProjectParameterSettingModel = new ProjectParameterSettingModel(continuous);
            option = ProcessOption.All;
            IsReadOnlyProjectParameter = false;
            IsReadOnlyDatasetParameter = false;
            IsReadOnlyPeakPickParameter = false;
            IsReadOnlyAnnotationParameter = false;
            IsReadOnlyAlignmentParameter = false;
        }

        public ProjectSettingModel(ProjectModel project) {
            ProjectModel = project;
            option = ProcessOption.All;
            IsReadOnlyProjectParameter = true;
            IsReadOnlyDatasetParameter = false;
            IsReadOnlyPeakPickParameter = false;
            IsReadOnlyAnnotationParameter = false;
            IsReadOnlyAlignmentParameter = false;
        }

        public ProjectSettingModel(ProjectModel project, DatasetModel dataset, ProcessOption option) {
            ProjectModel = project;
            DatasetModel = dataset;
            this.option = option;
            IsReadOnlyProjectParameter = true;
            IsReadOnlyDatasetParameter = true;
            IsReadOnlyPeakPickParameter = !option.HasFlag(ProcessOption.PeakSpotting);
            IsReadOnlyAnnotationParameter = !option.HasFlag(ProcessOption.Identification);
            IsReadOnlyAlignmentParameter = !option.HasFlag(ProcessOption.Alignment);
        }

        private readonly ProcessOption option;

        public ProjectModel ProjectModel { get; private set; }

        public DatasetModel DatasetModel { get; private set; }

        public ProjectParameterSettingModel ProjectParameterSettingModel { get; }

        public DatasetFileSettingModel DatasetFileSettingModel => ProjectModel?.DatasetFileSetting;

        public DatasetParameterSettingModel DatasetParameterSettingModel => ProjectModel?.DatasetParameterSetting;

        public DataCollectionSettingModel DataCollectionSettingModel => DatasetModel?.DataCollectionSettingModel;

        public PeakDetectionSettingModel PeakDetectionSettingModel => DatasetModel?.PeakDetectionSettingModel;

        public DeconvolutionSettingModel DeconvolutionSettingModel => DatasetModel?.DeconvolutionSettingModel;

        public IdentifySettingModel IdentifySettingModel => DatasetModel?.IdentifySettingModel;

        public AdductIonSettingModel AdductIonSettingModel => DatasetModel?.AdductIonSettingModel;

        public AlignmentParameterSettingModel AlignmentParameterSettingModel => DatasetModel?.AlignmentParameterSettingModel;

        public MobilitySettingModel MobilitySettingModel => DatasetModel?.MobilitySettingModel;

        public IsotopeTrackSettingModel IsotopeTrackSettingModel => DatasetModel?.IsotopeTrackSettingModel;

        public bool IsReadOnlyProjectParameter { get; }

        public bool IsReadOnlyDatasetParameter { get; }

        public bool IsReadOnlyPeakPickParameter { get; }

        public bool IsReadOnlyAnnotationParameter { get; }

        public bool IsReadOnlyAlignmentParameter { get; }

        public bool IsComplete {
            get => isComplete;
            private set => SetProperty(ref isComplete, value);
        }
        private bool isComplete;

        public void AfterProjectSetting() {
            ProjectModel = ProjectParameterSettingModel.Build();
        }

        public void AfterDatasetSetting() {
            DatasetModel = DatasetParameterSettingModel.Build();
        }

        public void RunAll() {
            DataCollectionSettingModel.Commit();
            PeakDetectionSettingModel.Commit();
            DeconvolutionSettingModel.Commit();
            DatasetModel.Storage.DataBases = IdentifySettingModel.Create();
            AdductIonSettingModel.Commit();
            AlignmentParameterSettingModel.Commit();
            MobilitySettingModel.Commit();
            IsotopeTrackSettingModel.Commit();
            DatasetModel.Run(option);
            IsComplete = true;
        }

        public void RunFromAnnotation() {
            DatasetModel.Storage.DataBases = IdentifySettingModel.Create();
            AdductIonSettingModel.Commit();
            AlignmentParameterSettingModel.Commit();
            MobilitySettingModel.Commit();
            IsotopeTrackSettingModel.Commit();
            DatasetModel.Run(option);
            IsComplete = true;
        }

        public void RunFromAlignment() {
            AdductIonSettingModel.Commit();
            AlignmentParameterSettingModel.Commit();
            MobilitySettingModel.Commit();
            IsotopeTrackSettingModel.Commit();
            DatasetModel.Run(option);
            IsComplete = true;
        }
    }
}
