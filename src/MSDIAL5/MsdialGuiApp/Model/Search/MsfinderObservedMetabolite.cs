using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Ion;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.FormulaGenerator.Parser;
using CompMs.Common.Parameter;
using CompMs.Common.StructureFinder.DataObj;
using CompMs.Common.StructureFinder.Parser;
using CompMs.Common.StructureFinder.Result;
using CompMs.Common.Utility;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Search
{
    internal sealed class MsfinderObservedMetabolite : BindableBase {
        private readonly MsfinderQueryFile _queryFile;
        private readonly AnalysisParamOfMsfinder _parameter;
        private RawData _spotData;
        public List<FormulaResult> _formulaList;
        public List<FragmenterResult?> _structureList;

        private static readonly List<ProductIon> productIonDB = CompMs.Common.FormulaGenerator.Parser.FragmentDbParser.GetProductIonDB(
                @"Resources\msfinderLibrary\ProductIonLib_vs1.pid", out string _);
        private static readonly List<NeutralLoss> neutralLossDB = CompMs.Common.FormulaGenerator.Parser.FragmentDbParser.GetNeutralLossDB(
            @"Resources\msfinderLibrary\NeutralLossDB_vs2.ndb", out string _);
        private static readonly List<ExistFormulaQuery> existFormulaDB = ExistFormulaDbParcer.ReadExistFormulaDB(
            @"Resources\msfinderLibrary\MsfinderFormulaDB-VS13.efd", out string _);

        private static readonly List<ExistStructureQuery> mineStructureDB = FileStorageUtility.GetMinesStructureDB();
        private static readonly List<FragmentOntology> fragmentOntologyDB = FileStorageUtility.GetUniqueFragmentDB();
        private static readonly List<MoleculeMsReference> mspDB = [];
        public List<ExistStructureQuery> _userDefinedDB = [];
        private static readonly List<FragmentLibrary> eiFragmentDB = FileStorageUtility.GetEiFragmentDB();
        private static readonly List<ExistStructureQuery> existStructureDB = FileStorageUtility.GetExistStructureDB();

        public string MetaboliteName {
            get => _spotData.Name;
            set {
                if (_spotData.Name != value) {
                    _spotData.Name = value;
                    OnPropertyChanged(nameof(MetaboliteName));
                }
            }
        }
        public int AlignmentID {
            get => _spotData.ScanNumber;
            set {
                if (_spotData.ScanNumber != value) {
                    _spotData.ScanNumber = value;
                    OnPropertyChanged(nameof(AlignmentID));
                }
            }
        }
        public double RetentionTime {
            get => _spotData.RetentionTime;
            set {
                if (_spotData.RetentionTime != value) {
                    _spotData.RetentionTime = value;
                    OnPropertyChanged(nameof(RetentionTime));
                }
            }
        }
        public double CentralCcs {
            get => _spotData.Ccs;
            set {
                if (_spotData.Ccs != value) {
                    _spotData.Ccs = value;
                    OnPropertyChanged(nameof(CentralCcs));
                }
            }
        }
        public double Mass {
            get => _spotData.PrecursorMz;
            set {
                if (_spotData.PrecursorMz != value) {
                    _spotData.PrecursorMz = value;
                    OnPropertyChanged(nameof(Mass));
                }
            }
        }
        public string Adduct {
            get => _spotData.PrecursorType;
            set {
                if (_spotData.PrecursorType != value) {
                    _spotData.PrecursorType = value;
                    OnPropertyChanged(nameof(Adduct));
                }
            }
        }
        public string Formula {
            get => _spotData.Formula;
            set {
                if (_spotData.Formula != value) {
                    _spotData.Formula = value;
                    OnPropertyChanged(nameof(Formula));
                }
            }
        }
        public string Ontology {
            get => _spotData.Ontology;
            set {
                if (_spotData.Ontology != value) {
                    _spotData.Ontology = value;
                    OnPropertyChanged(nameof(Ontology));
                }
            }
        }
        public string Smiles {
            get => _spotData.Smiles;
            set {
                if (_spotData.Smiles != value) {
                    _spotData.Smiles = value;
                    OnPropertyChanged(nameof(Smiles));
                }
            }
        }
        public string Inchikey {
            get => _spotData.InchiKey;
            set {
                if (_spotData.InchiKey != value) {
                    _spotData.InchiKey = value;
                    OnPropertyChanged(nameof(Inchikey));
                }
            }
        }
        public string Comment {
            get => _spotData.Comment;
            set {
                if (_spotData.Comment != value) {
                    _spotData.Comment = value;
                    OnPropertyChanged(nameof(Comment));
                }
            }
        }
        public IonMode IonMode {
            get => _spotData.IonMode;
            set {
                if (_spotData.IonMode != value) {
                    _spotData.IonMode = value;
                    OnPropertyChanged(nameof(IonMode));
                    OnPropertyChanged(nameof(AdductIons));
                }
            }
        }
        public List<AdductIon> AdductIons {
            get {
                if (IonMode == IonMode.Positive) {
                    return _parameter.MS1PositiveAdductIonList;
                } if (IonMode == IonMode.Negative) {
                    return _parameter.MS1NegativeAdductIonList;
                } else {
                    return [.. _parameter.MS1PositiveAdductIonList, .. _parameter.MS1NegativeAdductIonList];
                }
            }
        }
        public MSDataType SpectrumType {
            get => _spotData.SpectrumType;
            set {
                if (_spotData.SpectrumType != value) {
                    _spotData.SpectrumType = value;
                    OnPropertyChanged(nameof(SpectrumType));
                }
            }
        }

        public double CollisionEnergy {
            get => _spotData.CollisionEnergy;
            set {
                if (_spotData.CollisionEnergy != value) {
                    _spotData.CollisionEnergy = value;
                    OnPropertyChanged(nameof(CollisionEnergy));
                }
            }
        }

        public int Ms1Num {
            get => _spotData.Ms1PeakNumber;
        }
        public int Ms2Num {
            get => _spotData.Ms2PeakNumber;
        }

        public List<FormulaResult> FormulaList {
            get => _formulaList;
            set {
                if (_formulaList != value) {
                    _formulaList = value;
                    OnPropertyChanged(nameof(FormulaList));
                }
            }
        }

        public List<FragmenterResult?> StructureList {
            get => _structureList;
            set {
                if (_structureList != value) {
                    _structureList = value;
                    OnPropertyChanged(nameof(StructureList));
                }
            }
        }

        public MsSpectrum? Ms1Spectrum { get; private set; }
        public MsSpectrum? Ms2Spectrum { get; private set; }

        public MsfinderObservedMetabolite(MsfinderQueryFile queryFile, AnalysisParamOfMsfinder parameter, List<ExistStructureQuery> existStructureQueries) {
            _queryFile = queryFile;
            _parameter = parameter;
            _userDefinedDB = existStructureQueries;
            Load();
            LoadExistingFiles();
        }

        public DelegateCommand RunFindFormula => _runFindFormula ??= new DelegateCommand(FindFormula);
        private DelegateCommand? _runFindFormula;

        public DelegateCommand RunFindStructure => _runFindStructure ??= new DelegateCommand(FindStructure);
        private DelegateCommand? _runFindStructure;

        private void Load() {
            if (_queryFile is not null) {
                _spotData = RawDataParcer.RawDataFileReader(_queryFile.RawDataFilePath, _parameter);
                Ms1Spectrum = new MsSpectrum(_spotData.Ms1Spectrum);
                Ms2Spectrum = new MsSpectrum(_spotData.Ms2Spectrum);
            }
        }

        public void LoadExistingFiles() {
            var error = string.Empty;
            if (_queryFile.FormulaFileExists) {
                _formulaList =  FormulaResultParcer.FormulaResultReader(_queryFile.FormulaFilePath, out error);
            }
            if (_queryFile.StructureFileExists) {
                var structureFilePath = System.IO.Directory.GetFiles(_queryFile.StructureFolderPath, "*.sfd");
                foreach (var file in structureFilePath) {
                    FragmenterResultParser.FragmenterResultReader(file);
                }
            }
        }

        public void FindFormula() {
            var error = string.Empty;
            var rawData = RawDataParcer.RawDataFileReader(_queryFile.RawDataFilePath, _parameter);
            var formulaResults = MolecularFormulaFinder.GetMolecularFormulaList(productIonDB, neutralLossDB, existFormulaDB, rawData, _parameter);
            FormulaResultParcer.FormulaResultsWriter(_queryFile.FormulaFilePath, formulaResults);
            _formulaList = FormulaResultParcer.FormulaResultReader(_queryFile.FormulaFilePath, out error);
        }

        private void FindStructure() {
            var rawData = RawDataParcer.RawDataFileReader(_queryFile.RawDataFilePath, _parameter);
            var process = new StructureFinderBatchProcess();
            process.SingleSearchOfStructureFinder(_queryFile, rawData, _parameter, existStructureDB, _userDefinedDB, mineStructureDB, fragmentOntologyDB, mspDB, eiFragmentDB);
            var structureFilePath = System.IO.Directory.GetFiles(_queryFile.StructureFolderPath, "*.sfd");
            foreach (var file in structureFilePath) { 
                FragmenterResultParser.FragmenterResultReader(file);
            }
        }

        public Task ClearAsync(CancellationToken token = default) {
            throw new NotImplementedException();
        }

        public Task ReflectToMsdialAsync(CancellationToken token = default) {
            throw new NotImplementedException();
        }
    }
}
