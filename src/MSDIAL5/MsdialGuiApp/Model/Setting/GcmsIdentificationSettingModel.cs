using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Parameter;
using CompMs.Common.Parser;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialGcMsApi.Parameter;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.Msdial.Model.Setting
{
    public sealed class RiDictionaryModel : BindableBase {
        private readonly RiDictionaryInfo _dictionaryInfo;

        public RiDictionaryModel(string file, RiDictionaryInfo dictionaryInfo) {
            File = file;
            _dictionaryInfo = dictionaryInfo;
        }

        public string File { get; }
        public string DictionaryPath {
            get => _dictionaryPath;
            set => SetProperty(ref _dictionaryPath, value);
        }
        private string _dictionaryPath = string.Empty;

        public void Commit() {
            _dictionaryInfo.DictionaryFilePath = DictionaryPath;
        }
    }

    public sealed class GcmsIdentificationSettingModel : BindableBase, IIdentificationSettingModel
    {
        private readonly MsdialGcmsParameter _parameter;

        public GcmsIdentificationSettingModel(MsdialGcmsParameter parameter, AnalysisFileBeanModelCollection files, ProcessOption process) {
            SearchParameter = parameter.RefSpecMatchBaseParam.MspSearchParam is null
                ?  new MsRefSearchParameterBase()
                : new MsRefSearchParameterBase(parameter.RefSpecMatchBaseParam.MspSearchParam);
            IsReadOnly = (process & ProcessOption.Identification) == 0;
            RetentionType = parameter.RetentionType;
            var dictionaries = parameter.RefSpecMatchBaseParam.FileIdRiInfoDictionary.Select(kvp => new RiDictionaryModel(files.FindById(kvp.Key).AnalysisFilePath, kvp.Value));
            RetentionIndexFiles = new ObservableCollection<RiDictionaryModel>(dictionaries);
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

        public ObservableCollection<RiDictionaryModel> RetentionIndexFiles { get; }

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

        public DataBaseStorage Create(IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer) {
            var result = DataBaseStorage.CreateEmpty();
            if (IsReadOnly) {
                return result;
            }
            var storage = DataBaseStorage.CreateEmpty();
            var database = new MoleculeDataBase(MspFileParser.MspFileReader(MspFilePath), "0", DataBaseSource.Msp, SourceType.MspDB);
            var annotator = new MassAnnotator(database, _parameter.RefSpecMatchBaseParam.MspSearchParam, TargetOmics.Metabolomics, SourceType.MspDB, "annotator_0", 0);
            var pair = new MetabolomicsAnnotatorParameterPair(annotator.Save(), new AnnotationQueryFactory(annotator, _parameter.PeakPickBaseParam, _parameter.RefSpecMatchBaseParam.MspSearchParam));
            storage.AddMoleculeDataBase(database, new List<IAnnotatorParameterPair<MoleculeDataBase>> { pair });
            return result;
        }


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
            foreach (var riFile in RetentionIndexFiles) {
                riFile.Commit();
            }
            return true;
        }

        public void LoadParameter(MsdialGcmsParameter parameter) {
            if (IsReadOnly) {
                return;
            }
            RetentionType = parameter.RetentionType;
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
