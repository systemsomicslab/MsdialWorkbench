using CompMs.App.Msdial.Model.Dims;
using CompMs.App.Msdial.Model.Imms;
using CompMs.CommonMVVM;
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
    public class DataCollectionSettingModel : BindableBase {
        private readonly ParameterBase parameter;

        public DataCollectionSettingModel(ParameterBase parameter) {
            this.parameter = parameter;

            Ms1Tolerance = parameter.PeakPickBaseParam.CentroidMs1Tolerance;
            Ms2Tolerance = parameter.PeakPickBaseParam.CentroidMs2Tolerance;
            MaxChargeNumber = parameter.PeakPickBaseParam.MaxChargeNumber;
            IsBrClConsideredForIsotopes = parameter.PeakPickBaseParam.IsBrClConsideredForIsotopes;
            NumberOfThreads = parameter.ProcessBaseParam.NumThreads;
            DataCollectionRangeSettings = PrepareRangeSettings(parameter);
        }

        public DataCollectionSettingModel(MsdialDimsParameter parameter) : this((ParameterBase)parameter) {
            DimsProviderFactoryParameter = new DimsDataCollectionSettingModel(parameter.ProcessBaseParam, parameter.PeakPickBaseParam, parameter.ProviderFactoryParameter);
        }

        public DataCollectionSettingModel(MsdialImmsParameter parameter) : this((ParameterBase)parameter) {
            ImmsProviderFactoryParameter = new ImmsDataCollectionSettingModel(parameter);
        }

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

        public DimsDataCollectionSettingModel DimsProviderFactoryParameter { get; }
        public ImmsDataCollectionSettingModel ImmsProviderFactoryParameter { get; }

        public void Commit() {
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
