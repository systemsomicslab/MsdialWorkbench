using CompMs.Common.Enum;
using CompMs.Common.Parameter;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialGcMsApi.Parameter;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.Msdial.Model.Setting
{
    public sealed class GcmsIdentificationSettingModel : BindableBase
    {
        private readonly MsdialGcmsParameter _parameter;

        public GcmsIdentificationSettingModel(MsdialGcmsParameter parameter, ProcessOption process) {
            SearchParameter = parameter.RefSpecMatchBaseParam.MspSearchParam is null
                ?  new MsRefSearchParameterBase()
                : new MsRefSearchParameterBase(parameter.RefSpecMatchBaseParam.MspSearchParam);
            IsReadOnly = (process & ProcessOption.Identification) == 0;
            RetentionType = parameter.RetentionType;
            RetentionIndexFiles = new ObservableCollection<RiDictionaryInfo>(parameter.RefSpecMatchBaseParam.FileIdRiInfoDictionary.Values);
            CompoundType = parameter.RiCompoundType;
            MspFilePath = parameter.ReferenceFileParam.MspFilePath;
            UseQuantmassDefinedInLibrary = parameter.IsReplaceQuantmassByUserDefinedValue;
            OnlyReportTopHit = parameter.RefSpecMatchBaseParam.OnlyReportTopHitInMspSearch;
            _parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
        }

        public bool IsReadOnly { get; }

        public MsRefSearchParameterBase SearchParameter { get; }

        public RetentionType RetentionType {
            get => _retentionType;
            set => SetProperty(ref _retentionType, value);
        }
        private RetentionType _retentionType;

        public ObservableCollection<RiDictionaryInfo> RetentionIndexFiles { get; }

        public RiCompoundType CompoundType {
            get => _compoundType;
            set => SetProperty(ref _compoundType, value);
        }
        private RiCompoundType _compoundType;

        public string MspFilePath {
            get => _mspFilePath;
            set => SetProperty(ref _mspFilePath, value);
        }
        private string _mspFilePath;

        public bool UseQuantmassDefinedInLibrary {
            get => _useQuantmassDefinedInLibrary;
            set => SetProperty(ref _useQuantmassDefinedInLibrary, value);
        }
        private bool _useQuantmassDefinedInLibrary;

        public bool OnlyReportTopHit {
            get => _onlyReportTopHit;
            set => SetProperty(ref _onlyReportTopHit, value);
        }
        private bool _onlyReportTopHit;

        public bool TryCommit() {
            if (IsReadOnly) {
                return false;
            }

            _parameter.RetentionType = RetentionType;
            _parameter.RiCompoundType = CompoundType;
            _parameter.ReferenceFileParam.MspFilePath = MspFilePath;
            _parameter.RefSpecMatchBaseParam.MspSearchParam = SearchParameter;
            _parameter.IsReplaceQuantmassByUserDefinedValue = UseQuantmassDefinedInLibrary;
            _parameter.RefSpecMatchBaseParam.OnlyReportTopHitInMspSearch = OnlyReportTopHit;
            return true;
        }

        public void LoadParameter(MsdialGcmsParameter parameter) {
            if (IsReadOnly) {
                return;
            }
            RetentionType = parameter.RetentionType;
            if (RetentionIndexFiles.Count == parameter.RefSpecMatchBaseParam.FileIdRiInfoDictionary.Values.Count) {
                foreach (var (x, y) in RetentionIndexFiles.Zip(parameter.RefSpecMatchBaseParam.FileIdRiInfoDictionary.Values, (x, y) => (x, y))) {
                    x.DictionaryFilePath = y.DictionaryFilePath;
                }
            }
            CompoundType = parameter.RiCompoundType;
            MspFilePath = parameter.ReferenceFileParam.MspFilePath;
            SearchParameter.RiTolerance = parameter.RefSpecMatchBaseParam.MspSearchParam.RiTolerance;
            SearchParameter.RtTolerance = parameter.RefSpecMatchBaseParam.MspSearchParam.RtTolerance;
            SearchParameter.Ms1Tolerance = parameter.RefSpecMatchBaseParam.MspSearchParam.Ms1Tolerance;
            SearchParameter.WeightedDotProductCutOff = parameter.RefSpecMatchBaseParam.MspSearchParam.WeightedDotProductCutOff;
            SearchParameter.TotalScoreCutoff = parameter.RefSpecMatchBaseParam.MspSearchParam.TotalScoreCutoff;
            SearchParameter.IsUseTimeForAnnotationScoring = parameter.RefSpecMatchBaseParam.MspSearchParam.IsUseTimeForAnnotationScoring;
            SearchParameter.IsUseTimeForAnnotationFiltering = parameter.RefSpecMatchBaseParam.MspSearchParam.IsUseTimeForAnnotationFiltering;
            UseQuantmassDefinedInLibrary = parameter.IsReplaceQuantmassByUserDefinedValue;
            OnlyReportTopHit = parameter.RefSpecMatchBaseParam.OnlyReportTopHitInMspSearch;
        }
    }
}
