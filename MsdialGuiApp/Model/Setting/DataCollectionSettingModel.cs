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

namespace CompMs.App.Msdial.Model.Setting
{
    public class DataCollectionSettingModel : BindableBase {
        private readonly ParameterBase parameter;
        private readonly IReadOnlyList<AnalysisFileBean> analysisFiles;

        public DataCollectionSettingModel(ParameterBase parameter, IReadOnlyList<AnalysisFileBean> analysisFiles, ProcessOption process) {
            this.parameter = parameter;
            this.analysisFiles = analysisFiles;
            IsReadOnly = (process & ProcessOption.PeakSpotting) == 0;

            Ms1Tolerance = parameter.PeakPickBaseParam.CentroidMs1Tolerance;
            Ms2Tolerance = parameter.PeakPickBaseParam.CentroidMs2Tolerance;
            MaxChargeNumber = parameter.PeakPickBaseParam.MaxChargeNumber;
            IsBrClConsideredForIsotopes = parameter.PeakPickBaseParam.IsBrClConsideredForIsotopes;
            NumberOfThreads = parameter.ProcessBaseParam.NumThreads;
            ExcuteRtCorrection = parameter.AdvancedProcessOptionBaseParam.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.ExcuteRtCorrection;
            DataCollectionRangeSettings = PrepareRangeSettings(parameter);
        }

        public DataCollectionSettingModel(MsdialDimsParameter parameter, IReadOnlyList<AnalysisFileBean> analysisFiles, ProcessOption process) : this((ParameterBase)parameter, analysisFiles, process) {
            DimsProviderFactoryParameter = new DimsDataCollectionSettingModel(parameter.ProcessBaseParam, parameter.PeakPickBaseParam, parameter.ProviderFactoryParameter);
        }

        public DataCollectionSettingModel(MsdialImmsParameter parameter, IReadOnlyList<AnalysisFileBean> analysisFiles, ProcessOption process) : this((ParameterBase)parameter, analysisFiles, process) {
            ImmsProviderFactoryParameter = new ImmsDataCollectionSettingModel(parameter);
        }

        public bool IsReadOnly { get; }

        public float Ms1Tolerance {
            get => ms1Tolerance;
            set => SetProperty(ref ms1Tolerance, value);
        }
        private float ms1Tolerance;

        public float Ms2Tolerance {
            get => ms2Tolerance;
            set => SetProperty(ref ms2Tolerance, value);
        }
        private float ms2Tolerance;

        public List<IDataCollectionRangeSetting> DataCollectionRangeSettings { get; }

        public int MaxChargeNumber {
            get => maxChargeNumber;
            set => SetProperty(ref maxChargeNumber, value);
        }
        private int maxChargeNumber;

        public bool IsBrClConsideredForIsotopes {
            get => isBrClConsideredForIsotopes;
            set => SetProperty(ref isBrClConsideredForIsotopes, value);
        }
        private bool isBrClConsideredForIsotopes;

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

        public DimsDataCollectionSettingModel DimsProviderFactoryParameter { get; }
        public ImmsDataCollectionSettingModel ImmsProviderFactoryParameter { get; }

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
            parameter.PeakPickBaseParam.CentroidMs1Tolerance = Ms1Tolerance;
            parameter.PeakPickBaseParam.CentroidMs2Tolerance = Ms2Tolerance;
            parameter.PeakPickBaseParam.MaxChargeNumber = MaxChargeNumber;
            parameter.PeakPickBaseParam.IsBrClConsideredForIsotopes = IsBrClConsideredForIsotopes;
            parameter.ProcessBaseParam.NumThreads = NumberOfThreads;
            DataCollectionRangeSettings.ForEach(s => s.Commit());
            switch (parameter) {
                case MsdialDimsParameter dimsParameter:
                    dimsParameter.ProviderFactoryParameter = DimsProviderFactoryParameter.CreateDataProviderFactoryParameter();
                    break;
                case MsdialImmsParameter immsParameter:
                    immsParameter.ProviderFactoryParameter = ImmsProviderFactoryParameter.CreateDataProviderFactoryParameter();
                    break;
            }
            return true;
        }

        private static List<IDataCollectionRangeSetting> PrepareRangeSettings(ParameterBase parameter) {
            switch (parameter) {
                case MsdialLcImMsParameter lcimmsParameter:
                    return new List<IDataCollectionRangeSetting>
                    {
                        new RetentionTimeCollectionRangeSetting(lcimmsParameter, needAccmulation: true),
                        new DriftTimeCollectionRangeSetting(lcimmsParameter, needAccmulation: false),
                        new Ms1CollectionRangeSetting(parameter.PeakPickBaseParam, needAccmulation: false),
                        new Ms2CollectionRangeSetting(parameter.PeakPickBaseParam, needAccmulation: false),
                    };
                case MsdialGcmsParameter _:
                case MsdialLcmsParameter _:
                    return new List<IDataCollectionRangeSetting>
                    {
                        new RetentionTimeCollectionRangeSetting(parameter.PeakPickBaseParam, needAccmulation: false),
                        new Ms1CollectionRangeSetting(parameter.PeakPickBaseParam, needAccmulation: false),
                        new Ms2CollectionRangeSetting(parameter.PeakPickBaseParam, needAccmulation: false),
                    };
                case MsdialImmsParameter immsParameter:
                    return new List<IDataCollectionRangeSetting>
                    {
                        new DriftTimeCollectionRangeSetting(immsParameter, needAccmulation: false),
                        new Ms1CollectionRangeSetting(parameter.PeakPickBaseParam, needAccmulation: false),
                        new Ms2CollectionRangeSetting(parameter.PeakPickBaseParam, needAccmulation: false),
                    };
                case MsdialDimsParameter _:
                    return new List<IDataCollectionRangeSetting>
                    {
                        new Ms1CollectionRangeSetting(parameter.PeakPickBaseParam, needAccmulation: false),
                        new Ms2CollectionRangeSetting(parameter.PeakPickBaseParam, needAccmulation: false),
                    };
                default:
                    return new List<IDataCollectionRangeSetting>();
            }
        }
    }
}
