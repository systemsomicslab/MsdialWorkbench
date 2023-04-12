using CompMs.Common.Enum;
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
        public GcmsIdentificationSettingModel(MsdialGcmsParameter parameter, ProcessOption process) {
            IsReadOnly = (process & ProcessOption.Identification) == 0;
            RetentionType = parameter.RetentionType;
            RetentionIndexFiles = new ObservableCollection<RiDictionaryInfo>(parameter.RefSpecMatchBaseParam.FileIdRiInfoDictionary.Values);
            CompoundType = parameter.RiCompoundType;
            MspFilePath = parameter.ReferenceFileParam.MspFilePath;
            RetentionIndexTolerance = parameter.RefSpecMatchBaseParam.MspSearchParam.RiTolerance;
            RetentionTimeTolerance = parameter.RefSpecMatchBaseParam.MspSearchParam.RtTolerance;
            MzTolerance = parameter.RefSpecMatchBaseParam.MspSearchParam.Ms1Tolerance;
            EISimilarityCutoff = parameter.RefSpecMatchBaseParam.MspSearchParam.WeightedDotProductCutOff;
            IdentificationScoreCutoff = parameter.RefSpecMatchBaseParam.MspSearchParam.TotalScoreCutoff;
            UseRetentionInformationForScoring = parameter.RefSpecMatchBaseParam.MspSearchParam.IsUseTimeForAnnotationScoring;
            UseRetentionInformationForFiltering = parameter.RefSpecMatchBaseParam.MspSearchParam.IsUseTimeForAnnotationFiltering;
            UseQuantmassDefinedInLibrary = parameter.IsReplaceQuantmassByUserDefinedValue;
            ReportOnlyTopHit = parameter.RefSpecMatchBaseParam.OnlyReportTopHitInMspSearch;
            _parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
        }

        public bool IsReadOnly { get; }

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

        public float RetentionIndexTolerance {
            get => _retentionIndexTolerance;
            set => SetProperty(ref _retentionIndexTolerance, value);
        }
        private float _retentionIndexTolerance;

        public float RetentionTimeTolerance {
            get => _retentionTimeTolerance;
            set => SetProperty(ref _retentionTimeTolerance, value);
        }
        private float _retentionTimeTolerance;

        public float MzTolerance {
            get => _mzTolerance;
            set => SetProperty(ref _mzTolerance, value);
        }
        private float _mzTolerance;

        public float EISimilarityCutoff {
            get => _eISimilarityCutoff;
            set => SetProperty(ref _eISimilarityCutoff, value);
        }
        private float _eISimilarityCutoff;

        public float IdentificationScoreCutoff {
            get => _identificationScoreCutoff;
            set => SetProperty(ref _identificationScoreCutoff, value);
        }
        private float _identificationScoreCutoff;

        public bool UseRetentionInformationForScoring {
            get => _useRetentionInformationForScoring;
            set => SetProperty(ref _useRetentionInformationForScoring, value);
        }
        private bool _useRetentionInformationForScoring;

        public bool UseRetentionInformationForFiltering {
            get => _useRetentionInformationForFiltering;
            set => SetProperty(ref _useRetentionInformationForFiltering, value);
        }
        private bool _useRetentionInformationForFiltering;

        public bool UseQuantmassDefinedInLibrary {
            get => _useQuantmassDefinedInLibrary;
            set => SetProperty(ref _useQuantmassDefinedInLibrary, value);
        }
        private bool _useQuantmassDefinedInLibrary;

        public bool ReportOnlyTopHit {
            get => _reportOnlyTopHit;
            set => SetProperty(ref _reportOnlyTopHit, value);
        }
        private bool _reportOnlyTopHit;
        private readonly MsdialGcmsParameter _parameter;

        public bool TryCommit() {
            if (IsReadOnly) {
                return false;
            }

            _parameter.RetentionType = RetentionType;
            _parameter.RiCompoundType = CompoundType;
            _parameter.ReferenceFileParam.MspFilePath = MspFilePath;
            _parameter.RefSpecMatchBaseParam.MspSearchParam.RiTolerance = RetentionIndexTolerance;
            _parameter.RefSpecMatchBaseParam.MspSearchParam.RtTolerance = RetentionTimeTolerance;
            _parameter.RefSpecMatchBaseParam.MspSearchParam.Ms1Tolerance = MzTolerance;
            _parameter.RefSpecMatchBaseParam.MspSearchParam.WeightedDotProductCutOff = EISimilarityCutoff;
            _parameter.RefSpecMatchBaseParam.MspSearchParam.TotalScoreCutoff = IdentificationScoreCutoff;
            _parameter.RefSpecMatchBaseParam.MspSearchParam.IsUseTimeForAnnotationScoring = UseRetentionInformationForScoring;
            _parameter.RefSpecMatchBaseParam.MspSearchParam.IsUseTimeForAnnotationFiltering = UseRetentionInformationForFiltering;
            _parameter.IsReplaceQuantmassByUserDefinedValue = UseQuantmassDefinedInLibrary;
            _parameter.RefSpecMatchBaseParam.OnlyReportTopHitInMspSearch = ReportOnlyTopHit;
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
            RetentionIndexTolerance = parameter.RefSpecMatchBaseParam.MspSearchParam.RiTolerance;
            RetentionTimeTolerance = parameter.RefSpecMatchBaseParam.MspSearchParam.RtTolerance;
            MzTolerance = parameter.RefSpecMatchBaseParam.MspSearchParam.Ms1Tolerance;
            EISimilarityCutoff = parameter.RefSpecMatchBaseParam.MspSearchParam.WeightedDotProductCutOff;
            IdentificationScoreCutoff = parameter.RefSpecMatchBaseParam.MspSearchParam.TotalScoreCutoff;
            UseRetentionInformationForScoring = parameter.RefSpecMatchBaseParam.MspSearchParam.IsUseTimeForAnnotationScoring;
            UseRetentionInformationForFiltering = parameter.RefSpecMatchBaseParam.MspSearchParam.IsUseTimeForAnnotationFiltering;
            UseQuantmassDefinedInLibrary = parameter.IsReplaceQuantmassByUserDefinedValue;
            ReportOnlyTopHit = parameter.RefSpecMatchBaseParam.OnlyReportTopHitInMspSearch;
        }
    }
}
