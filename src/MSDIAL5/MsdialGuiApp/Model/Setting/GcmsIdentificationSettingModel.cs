using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.ViewModel.Service;
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
using Reactive.Bindings.Notifiers;
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

        public bool IsValid() {
            throw new NotImplementedException();
        }

        public void Commit() {
            _dictionaryInfo.DictionaryFilePath = DictionaryPath;
        }
    }

    public sealed class RiDictionarySettingModel : BindableBase {
        private readonly IMessageBroker _broker;

        public RiDictionarySettingModel(AnalysisFileBeanModelCollection files, Dictionary<int, RiDictionaryInfo> fileIdToRiInfo, IMessageBroker broker) {
            var dictionaries = fileIdToRiInfo.Select(kvp => new RiDictionaryModel(files.FindById(kvp.Key).AnalysisFilePath, kvp.Value));
            RetentionIndexFiles = new ObservableCollection<RiDictionaryModel>(dictionaries);
            _broker = broker;
            IsImported = RetentionIndexFiles.All(file => file.IsValid());
        }

        public ObservableCollection<RiDictionaryModel> RetentionIndexFiles { get; }

        public RiDictionaryModel SelectedRetentionIndexFile {
            get => _selectedRetentionIndexFile;
            set => SetProperty(ref _selectedRetentionIndexFile, value);
        }
        private RiDictionaryModel _selectedRetentionIndexFile;

        public bool IsImported {
            get => _isImported;
            private set => SetProperty(ref _isImported, value);
        }
        private bool _isImported;

        public void AutoFill() {
            if (SelectedRetentionIndexFile is null || string.IsNullOrEmpty(SelectedRetentionIndexFile.DictionaryPath)) {
                return;
            }
            foreach (var file in RetentionIndexFiles) {
                file.DictionaryPath = SelectedRetentionIndexFile.DictionaryPath;
            }
        }

        public bool TrySet() {
            if (RetentionIndexFiles.All(file => file.IsValid())) {
                IsImported = true;
                return true;
            }
            else {
                var request = new ErrorMessageBoxRequest
                {
                    Content = string.Join("\n",
                        "Invalid RI information. Please confirm your file and prepare the following information.",
                        "Carbon number\tRT(min)",
                        "10\t4.72",
                        "11\t5.63",
                        "12\t6.81",
                        "13\t8.08",
                        "14\t9.12",
                        "15\t10.33",
                        "16\t11.91",
                        "18\t14.01",
                        "20\t16.15",
                        "22\t18.28",
                        "24\t20.33",
                        "26\t22.17",
                        "",
                        "This information should be required for RI calculation."),
                };
                _broker?.Publish(request);
                return false;
            }
        }

        public bool TryCommit() {
            foreach (var file in RetentionIndexFiles) {
                file.Commit();
            }
            return true;
        }
    }

    public sealed class GcmsIdentificationSettingModel : BindableBase, IIdentificationSettingModel
    {
        private readonly MsdialGcmsParameter _parameter;

        public GcmsIdentificationSettingModel(MsdialGcmsParameter parameter, AnalysisFileBeanModelCollection files, ProcessOption process, IMessageBroker broker) {
            SearchParameter = parameter.RefSpecMatchBaseParam.MspSearchParam is null
                ?  new MsRefSearchParameterBase()
                : new MsRefSearchParameterBase(parameter.RefSpecMatchBaseParam.MspSearchParam);
            IsReadOnly = (process & ProcessOption.Identification) == 0;
            RetentionType = parameter.RetentionType;
            RiDictionarySettingModel = new RiDictionarySettingModel(files, parameter.RefSpecMatchBaseParam.FileIdRiInfoDictionary, broker);
            RetentionIndexFiles = RiDictionarySettingModel.RetentionIndexFiles;
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
        public RiDictionarySettingModel RiDictionarySettingModel { get; }

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

            if (!RiDictionarySettingModel.TryCommit()) {
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
