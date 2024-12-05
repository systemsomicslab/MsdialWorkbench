using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Dims;
using CompMs.App.Msdial.Model.Imms;
using CompMs.App.Msdial.View.Setting;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialDimsCore.Parameter;
using CompMs.MsdialGcMsApi.Parameter;
using CompMs.MsdialImmsCore.Parameter;
using CompMs.MsdialLcImMsApi.Parameter;
using CompMs.MsdialLcmsApi.Parameter;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CompMs.App.Msdial.Model.Setting
{
    public interface IDataCollectionSettingModel {
        bool IsReadOnly { get; }
        void LoadParameter(ParameterBase parameter);
        bool TryCommit();
    }

    public sealed class DataCollectionSettingModel : BindableBase, IDataCollectionSettingModel {
        private readonly ParameterBase parameter;
        private readonly IReadOnlyList<AnalysisFileBean> analysisFiles;

        public DataCollectionSettingModel(ParameterBase parameter, PeakPickBaseParameterModel peakPickBaseParameterModel, IReadOnlyList<AnalysisFileBean> analysisFiles, ProcessOption process) {
            this.parameter = parameter;
            PeakPickBaseParameterModel = peakPickBaseParameterModel;
            this.analysisFiles = analysisFiles;
            IsReadOnly = (process & ProcessOption.PeakSpotting) == 0;

            NumberOfThreads = parameter.ProcessBaseParam.NumThreads;
            ExcuteRtCorrection = parameter.AdvancedProcessOptionBaseParam.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.ExcuteRtCorrection;
            DataCollectionRangeSettings = new ObservableCollection<IDataCollectionRangeSetting>(PrepareRangeSettings(parameter, peakPickBaseParameterModel));
        }

        public DataCollectionSettingModel(MsdialDimsParameter parameter, PeakPickBaseParameterModel peakPickBaseParameterModel, IReadOnlyList<AnalysisFileBean> analysisFiles, ProcessOption process) : this((ParameterBase)parameter, peakPickBaseParameterModel, analysisFiles, process) {
            DimsProviderFactoryParameter = new DimsDataCollectionSettingModel(parameter.ProcessBaseParam, peakPickBaseParameterModel, parameter.ProviderFactoryParameter);
        }

        public DataCollectionSettingModel(MsdialImmsParameter parameter, PeakPickBaseParameterModel peakPickBaseParameterModel, IReadOnlyList<AnalysisFileBean> analysisFiles, ProcessOption process) : this((ParameterBase)parameter, peakPickBaseParameterModel, analysisFiles, process) {
            ImmsProviderFactoryParameter = new ImmsDataCollectionSettingModel(parameter, peakPickBaseParameterModel);
        }

        public bool IsReadOnly { get; }

        public ObservableCollection<IDataCollectionRangeSetting> DataCollectionRangeSettings { get; }

        public int NumberOfThreads {
            get => numberOfThreads;
            set => SetProperty(ref numberOfThreads, value);
        }
        private int numberOfThreads;

        public bool ExcuteRtCorrection {
            get => excuteRtCorrection;
            set => SetProperty(ref excuteRtCorrection, value);
        }
        private bool excuteRtCorrection;

        public DimsDataCollectionSettingModel? DimsProviderFactoryParameter { get; }
        public ImmsDataCollectionSettingModel? ImmsProviderFactoryParameter { get; }
        public PeakPickBaseParameterModel PeakPickBaseParameterModel { get; }

        public bool TryCommit() {
            if (IsReadOnly) {
                return false;
            }
            if (ExcuteRtCorrection) {
                var rtCorrectionWin = new RetentionTimeCorrectionWinLegacy(analysisFiles, parameter, false);
                if (!(rtCorrectionWin.ShowDialog() ?? false)) {
                    return false;
                }
            }
            PeakPickBaseParameterModel.Commit();
            parameter.ProcessBaseParam.NumThreads = NumberOfThreads;
            foreach (var s in DataCollectionRangeSettings) {
                s.Commit(); 
            }
            switch (parameter) {
                case MsdialDimsParameter dimsParameter:
                    if (DimsProviderFactoryParameter is not null) {
                        dimsParameter.ProviderFactoryParameter = DimsProviderFactoryParameter.CreateDataProviderFactoryParameter();
                    }
                    break;
                case MsdialImmsParameter immsParameter:
                    if (ImmsProviderFactoryParameter is not null) {
                        immsParameter.ProviderFactoryParameter = ImmsProviderFactoryParameter.CreateDataProviderFactoryParameter();
                    }
                    break;
            }
            return true;
        }

        public void LoadParameter(ParameterBase parameter) {
            if (IsReadOnly) {
                return;
            }
            PeakPickBaseParameterModel.LoadParameter(parameter.PeakPickBaseParam);
            NumberOfThreads = parameter.ProcessBaseParam.NumThreads;
            ExcuteRtCorrection = parameter.AdvancedProcessOptionBaseParam.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.ExcuteRtCorrection;
            foreach (var s in DataCollectionRangeSettings) {
                s.Update(parameter);
            }
            if (DimsProviderFactoryParameter != null) {
                DimsProviderFactoryParameter?.LoadParameter(((MsdialDimsParameter)parameter).ProviderFactoryParameter);
            }
            if (ImmsProviderFactoryParameter != null) {
                ImmsProviderFactoryParameter?.LoadParameter(((MsdialImmsParameter)parameter).ProviderFactoryParameter);
            }
        }

        private static List<IDataCollectionRangeSetting> PrepareRangeSettings(ParameterBase parameter, PeakPickBaseParameterModel peakPickBaseParameterModel) {
            switch (parameter) {
                case MsdialLcImMsParameter lcimmsParameter:
                    return new List<IDataCollectionRangeSetting>
                    {
                        new RetentionTimeCollectionRangeSetting(lcimmsParameter, peakPickBaseParameterModel, needAccmulation: true),
                        new DriftTimeCollectionRangeSetting(lcimmsParameter, needAccmulation: false),
                        new Ms1CollectionRangeSetting(peakPickBaseParameterModel, needAccmulation: false),
                        new Ms2CollectionRangeSetting(peakPickBaseParameterModel, needAccmulation: false),
                    };
                case MsdialGcmsParameter _:
                case MsdialLcmsParameter _:
                    return new List<IDataCollectionRangeSetting>
                    {
                        new RetentionTimeCollectionRangeSetting(peakPickBaseParameterModel, needAccmulation: false),
                        new Ms1CollectionRangeSetting(peakPickBaseParameterModel, needAccmulation: false),
                        new Ms2CollectionRangeSetting(peakPickBaseParameterModel, needAccmulation: false),
                    };
                case MsdialImmsParameter immsParameter:
                    return new List<IDataCollectionRangeSetting>
                    {
                        new DriftTimeCollectionRangeSetting(immsParameter, needAccmulation: false),
                        new Ms1CollectionRangeSetting(peakPickBaseParameterModel, needAccmulation: false),
                        new Ms2CollectionRangeSetting(peakPickBaseParameterModel, needAccmulation: false),
                    };
                case MsdialDimsParameter _:
                    return new List<IDataCollectionRangeSetting>
                    {
                        new Ms1CollectionRangeSetting(peakPickBaseParameterModel, needAccmulation: false),
                        new Ms2CollectionRangeSetting(peakPickBaseParameterModel, needAccmulation: false),
                    };
                default:
                    return new List<IDataCollectionRangeSetting>();
            }
        }
    }
}
