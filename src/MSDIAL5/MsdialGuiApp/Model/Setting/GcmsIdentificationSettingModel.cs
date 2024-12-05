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
using System.IO;
using System.Linq;

namespace CompMs.App.Msdial.Model.Setting
{
    public sealed class RiDictionaryModel : BindableBase {
        private readonly RiDictionaryInfo _dictionaryInfo;

        public RiDictionaryModel(string file, RiDictionaryInfo dictionaryInfo) {
            File = file;
            _dictionaryInfo = dictionaryInfo;
            DictionaryPath = dictionaryInfo.DictionaryFilePath;
        }

        public string File { get; }
        public string DictionaryPath {
            get => _dictionaryPath;
            set => SetProperty(ref _dictionaryPath, value);
        }
        private string _dictionaryPath = string.Empty;

        public List<RiDictionaryError> Validate(RiCompoundType compoundType) {
            var result = new List<RiDictionaryError>();
            if (!System.IO.File.Exists(DictionaryPath)) {
                result.Add(RiDictionaryError.FileNotFound(DictionaryPath));
            }
            var dictionary = RiDictionaryInfo.FromDictionaryFile(DictionaryPath);
            if (dictionary.IsIncorrectFormat) {
                result.Add(RiDictionaryError.Incorrect(DictionaryPath));
            }
            if (compoundType == RiCompoundType.Fames && !dictionary.IsFamesContents) {
                result.Add(RiDictionaryError.IsFamesContents(DictionaryPath));
            }
            if (!dictionary.IsSequentialCarbonRtOrdering) {
                result.Add(RiDictionaryError.IsSequencialOrder(DictionaryPath));
            }
            return result;
        }

        public void Commit() {
            var dictionary = RiDictionaryInfo.FromDictionaryFile(DictionaryPath);
            _dictionaryInfo.DictionaryFilePath = dictionary.DictionaryFilePath;
            _dictionaryInfo.RiDictionary = dictionary.RiDictionary;
        }

        public class RiDictionaryError {
            private readonly string[] _files;
            private readonly string _message;

            private RiDictionaryError(string file, string message) {
                _files = new[] { file };
                _message = message;
            }

            private RiDictionaryError(string[] files, string message) {
                _files = files;
                _message = message;
            }

            public void PrintError(Action<string> handler) {
                handler?.Invoke(string.Join("\r\n", _files) + "\r\n\r\n" + _message);
            }

            public static List<RiDictionaryError> MergeErrors(List<RiDictionaryError> errors) {
                return errors.GroupBy(e => e._message).Select(group => new RiDictionaryError(group.SelectMany(e => e._files).ToArray(), group.Key)).ToList();
            }

            public static RiDictionaryError FileNotFound(string file) {
                return new RiDictionaryError(file, $"{file} does not found.");
            }

            public static RiDictionaryError Incorrect(string file) {
                const string message = "Invalid RI information. Please confirm your file and prepare the following information.\r\n"
                    + "Carbon number\tRT(min)\r\n"
                    + "10\t4.72\r\n"
                    + "11\t5.63\r\n"
                    + "12\t6.81\r\n"
                    + "13\t8.08\r\n"
                    + "14\t9.12\r\n"
                    + "15\t10.33\r\n"
                    + "16\t11.91\r\n"
                    + "18\t14.01\r\n"
                    + "20\t16.15\r\n"
                    + "22\t18.28\r\n"
                    + "24\t20.33\r\n"
                    + "26\t22.17\r\n"
                    + "\r\nThis information should be required for RI calculation.";
                return new RiDictionaryError(file, message);
            }

            public static RiDictionaryError IsFamesContents(string file) {
                const string message = "If you use the FAMEs RI, you have to decide the retention times as minute for \r\n"
                    + "C8, C9, C10, C12, C14, C16, C18, C20, C22, C24, C26, C28, C30.";
                return new RiDictionaryError(file, message);
            }

            public static RiDictionaryError IsSequencialOrder(string file) {
                const string message = "Invalid carbon-rt sequence: incorrect ordering of retention times.\r\n"
                    + "Carbon number\tRT(min)\r\n"
                    + "10\t4.72\r\n"
                    + "11\t5.63\r\n"
                    + "12\t6.81\r\n"
                    + "13\t8.08\r\n"
                    + "14\t9.12\r\n"
                    + "15\t10.33\r\n"
                    + "16\t11.91\r\n"
                    + "18\t14.01\r\n"
                    + "20\t16.15\r\n"
                    + "22\t18.28\r\n"
                    + "24\t20.33\r\n"
                    + "26\t22.17\r\n"
                    + "\r\nThis information should be required for RI calculation.";
                return new RiDictionaryError(file, message);
            }
        }
    }

    public sealed class RiDictionarySettingModel : BindableBase {
        private readonly IMessageBroker _broker;

        public RiDictionarySettingModel(AnalysisFileBeanModelCollection files, Dictionary<int, RiDictionaryInfo> fileIdToRiInfo, RiCompoundType compountType, IMessageBroker broker) {
            var dictionaries = fileIdToRiInfo.Select(kvp => new RiDictionaryModel(files.FindByID(kvp.Key).AnalysisFilePath, kvp.Value));
            RetentionIndexFiles = new ObservableCollection<RiDictionaryModel>(dictionaries);
            CompoundType = compountType;
            _broker = broker;
            IsImported = CheckSetted(RetentionIndexFiles, compountType);
        }

        public ObservableCollection<RiDictionaryModel> RetentionIndexFiles { get; }

        public RiCompoundType CompoundType {
            get => _compoundType;
            set => SetProperty(ref _compoundType, value);
        }
        private RiCompoundType _compoundType;

        public bool IsImported {
            get => _isImported;
            private set => SetProperty(ref _isImported, value);
        }
        private bool _isImported;

        public void AutoFill(RiDictionaryModel model) {
            if (model is null || string.IsNullOrEmpty(model.DictionaryPath)) {
                return;
            }
            foreach (var file in RetentionIndexFiles) {
                file.DictionaryPath = model.DictionaryPath;
            }
        }

        public void AutoFill(RiDictionaryModel model, RiDictionaryModel[] models) {
            if (model is null || string.IsNullOrEmpty(model.DictionaryPath)) {
                return;
            }
            foreach (var file in models) {
                file.DictionaryPath = model.DictionaryPath;
            }
        }

        public bool TrySet() {
            var errors = RetentionIndexFiles.SelectMany(file => file.Validate(CompoundType)).ToList();
            errors = RiDictionaryModel.RiDictionaryError.MergeErrors(errors);
            if (errors.Any()) {
                foreach (var error in errors) {
                    error.PrintError(message => {
                        var request = new ErrorMessageBoxRequest
                        {
                            Content = message,
                        };
                        _broker?.Publish(request);
                    });
                }
                return false;
            }
            else {
                IsImported = true;
                return true;
            }
        }

        private static bool CheckSetted(ObservableCollection<RiDictionaryModel> retentionIndexFiles, RiCompoundType compoundType) {
            var errors = retentionIndexFiles.SelectMany(file => file.Validate(compoundType)).ToList();
            errors = RiDictionaryModel.RiDictionaryError.MergeErrors(errors);
            return !errors.Any();
        }

        public bool TryCommit(MsdialGcmsParameter parameter) {
            foreach (var file in RetentionIndexFiles) {
                file.Commit();
            }
            parameter.RiCompoundType = CompoundType;
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
            RiDictionarySettingModel = new RiDictionarySettingModel(files, parameter.RefSpecMatchBaseParam.FileIdRiInfoDictionary, parameter.RiCompoundType, broker);
            RetentionIndexFiles = RiDictionarySettingModel.RetentionIndexFiles;
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

        public string? MspFilePath {
            get => _mspFilePath;
            set => SetProperty(ref _mspFilePath, value);
        }
        private string? _mspFilePath;

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

        public DataBaseStorage Create(IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer) {
            var result = DataBaseStorage.CreateEmpty();
            if (IsReadOnly) {
                return result;
            }

            RiDictionarySettingModel.TryCommit(_parameter);
            _parameter.RetentionType = RetentionType;
            _parameter.ReferenceFileParam.MspFilePath = MspFilePath;
            _parameter.RefSpecMatchBaseParam.MspSearchParam = SearchParameter;
            _parameter.IsReplaceQuantmassByUserDefinedValue = UseQuantmassDefinedInLibrary;
            _parameter.RefSpecMatchBaseParam.OnlyReportTopHitInMspSearch = OnlyReportTopHit;

            if (MspFilePath is { } && File.Exists(MspFilePath)) {
                var database = new MoleculeDataBase(MspFileParser.MspFileReader(MspFilePath), "0", DataBaseSource.Msp, SourceType.MspDB);
                var annotator = new MassAnnotator(database, _parameter.RefSpecMatchBaseParam.MspSearchParam, TargetOmics.Metabolomics, SourceType.MspDB, "annotator_0", 0);
                var pair = new MetabolomicsAnnotatorParameterPair(annotator.Save(), new AnnotationQueryFactory(annotator, _parameter.PeakPickBaseParam, _parameter.RefSpecMatchBaseParam.MspSearchParam));
                result.AddMoleculeDataBase(database, new List<IAnnotatorParameterPair<MoleculeDataBase>> { pair });
            }
            return result;
        }

        public void LoadParameter(ParameterBase parameter) {
            if (IsReadOnly) {
                return;
            }
            if (parameter is MsdialGcmsParameter gcmsParameter) {
                RetentionType = gcmsParameter.RetentionType;
                RiDictionarySettingModel.CompoundType = gcmsParameter.RiCompoundType;
                UseQuantmassDefinedInLibrary = gcmsParameter.IsReplaceQuantmassByUserDefinedValue;
            }
            MspFilePath = parameter.ReferenceFileParam.MspFilePath;
            SearchParameter.RiTolerance = parameter.RefSpecMatchBaseParam.MspSearchParam.RiTolerance;
            SearchParameter.RtTolerance = parameter.RefSpecMatchBaseParam.MspSearchParam.RtTolerance;
            SearchParameter.Ms1Tolerance = parameter.RefSpecMatchBaseParam.MspSearchParam.Ms1Tolerance;
            SearchParameter.WeightedDotProductCutOff = parameter.RefSpecMatchBaseParam.MspSearchParam.WeightedDotProductCutOff;
            SearchParameter.TotalScoreCutoff = parameter.RefSpecMatchBaseParam.MspSearchParam.TotalScoreCutoff;
            SearchParameter.IsUseTimeForAnnotationScoring = parameter.RefSpecMatchBaseParam.MspSearchParam.IsUseTimeForAnnotationScoring;
            SearchParameter.IsUseTimeForAnnotationFiltering = parameter.RefSpecMatchBaseParam.MspSearchParam.IsUseTimeForAnnotationFiltering;
            OnlyReportTopHit = parameter.RefSpecMatchBaseParam.OnlyReportTopHitInMspSearch;
        }
    }
}
