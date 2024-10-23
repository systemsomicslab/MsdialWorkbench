using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Setting;
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
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Search
{
    internal sealed class MsfinderObservedMetabolite : BindableBase {
        private readonly MsfinderQueryFile _queryFile;
        private readonly AnalysisParamOfMsfinder _parameter;
        private RawData? _spotData;
        public List<FormulaResult> _formulaList;
        public List<FragmenterResult> _structureList;
        public List<ExistStructureQuery> _userDefinedDB;

        private static readonly List<ProductIon> productIonDB = CompMs.Common.FormulaGenerator.Parser.FragmentDbParser.GetProductIonDB(
                @"Resources\msfinderLibrary\ProductIonLib_vs1.pid", out string _);
        private static readonly List<NeutralLoss> neutralLossDB = CompMs.Common.FormulaGenerator.Parser.FragmentDbParser.GetNeutralLossDB(
            @"Resources\msfinderLibrary\NeutralLossDB_vs2.ndb", out string _);
        private static readonly List<ExistFormulaQuery> existFormulaDB = ExistFormulaDbParcer.ReadExistFormulaDB(
            @"Resources\msfinderLibrary\MsfinderFormulaDB-VS13.efd", out string _);

        private static readonly List<ExistStructureQuery> mineStructureDB = FileStorageUtility.GetMinesStructureDB();
        private static readonly List<FragmentOntology> fragmentOntologyDB = FileStorageUtility.GetUniqueFragmentDB();
        private static List<MoleculeMsReference> mspDB = new List<MoleculeMsReference>();
        private static readonly List<FragmentLibrary> eiFragmentDB = FileStorageUtility.GetEiFragmentDB();
        private static readonly List<ExistStructureQuery> existStructureDB = FileStorageUtility.GetExistStructureDB();

        public string metaboliteName {
            get => _spotData.Name;
            set {
                if (_spotData.Name != value) {
                    _spotData.Name = value;
                    OnPropertyChanged(nameof(metaboliteName));
                }
            }
        }
        public int alignmentID {
            get => _spotData.ScanNumber;
            set {
                if (_spotData?.ScanNumber != value) {
                    _spotData.ScanNumber = value;
                    OnPropertyChanged(nameof(alignmentID));
                }
            }
        }
        public double retentionTime {
            get => _spotData.RetentionTime;
            set {
                if (_spotData.RetentionTime != value) {
                    _spotData.RetentionTime = value;
                    OnPropertyChanged(nameof(retentionTime));
                }
            }
        }
        public double centralCcs {
            get => _spotData.Ccs;
            set {
                if (_spotData.Ccs != value) {
                    _spotData.Ccs = value;
                    OnPropertyChanged(nameof(centralCcs));
                }
            }
        }
        public double mass {
            get => _spotData.PrecursorMz;
            set {
                if (_spotData.PrecursorMz != value) {
                    _spotData.PrecursorMz = value;
                    OnPropertyChanged(nameof(mass));
                }
            }
        }
        public string adduct {
            get => _spotData.PrecursorType;
            set {
                if (_spotData.PrecursorType != value) {
                    _spotData.PrecursorType = value;
                    OnPropertyChanged(nameof(adduct));
                }
            }
        }
        public string formula {
            get => _spotData.Formula;
            set {
                if (_spotData?.Formula != value) {
                    _spotData.Formula = value;
                    OnPropertyChanged(nameof(formula));
                }
            }
        }
        public string ontology {
            get => _spotData.Ontology;
            set {
                if (_spotData.Ontology != value) {
                    _spotData.Ontology = value;
                    OnPropertyChanged(nameof(ontology));
                }
            }
        }
        public string smiles {
            get => _spotData.Smiles;
            set {
                if (_spotData.Smiles != value) {
                    _spotData.Smiles = value;
                    OnPropertyChanged(nameof(smiles));
                }
            }
        }
        public string inchikey {
            get => _spotData.InchiKey;
            set {
                if (_spotData.InchiKey != value) {
                    _spotData.InchiKey = value;
                    OnPropertyChanged(nameof(inchikey));
                }
            }
        }
        public string comment {
            get => _spotData.Comment;
            set {
                if (_spotData.Comment != value) {
                    _spotData.Comment = value;
                    OnPropertyChanged(nameof(comment));
                }
            }
        }
        public IonMode ionMode {
            get => _spotData.IonMode;
            set {
                if (_spotData.IonMode != value) {
                    _spotData.IonMode = value;
                    OnPropertyChanged(nameof(ionMode));
                    OnPropertyChanged(nameof(adductIons));
                }
            }
        }
        public List<AdductIon> adductIons {
            get {
                if (ionMode == IonMode.Positive) {
                    return _parameter.MS1PositiveAdductIonList;
                } if (ionMode == IonMode.Negative) {
                    return _parameter.MS1NegativeAdductIonList;
                } else {
                    return _parameter.MS1PositiveAdductIonList.Concat(_parameter.MS1NegativeAdductIonList).ToList();
                }
            }
        }
        public MSDataType spectrumType {
            get => _spotData.SpectrumType;
            set {
                if (_spotData.SpectrumType != value) {
                    _spotData.SpectrumType = value;
                    OnPropertyChanged(nameof(spectrumType));
                }
            }
        }

        public double collisionEnergy {
            get => _spotData.CollisionEnergy;
            set {
                if (_spotData.CollisionEnergy != value) {
                    _spotData.CollisionEnergy = value;
                    OnPropertyChanged(nameof(collisionEnergy));
                }
            }
        }

        public int ms1Num {
            get => _spotData.Ms1PeakNumber;
        }
        public int ms2Num {
            get => _spotData.Ms2PeakNumber;
        }

        public List<FormulaResult>? formulaList {
            get => _formulaList;
            set {
                if (_formulaList != value) {
                    _formulaList = value;
                    OnPropertyChanged(nameof(formulaList));
                }
            }
        }

        public List<FragmenterResult>? structureList {
            get => _structureList;
            set {
                if (_structureList != value) {
                    _structureList = value;
                    OnPropertyChanged(nameof(structureList));
                }
            }
        }

        public MsSpectrum ms1Spectrum { get; private set; }
        public MsSpectrum ms2Spectrum { get; private set; }

        public MsSpectrum experimentSpectrum { get; private set; }
        public MsSpectrum referenceSpectrum { get; private set; }

        public MsfinderObservedMetabolite(MsfinderQueryFile queryFile, AnalysisParamOfMsfinder parameter, List<ExistStructureQuery> existStructureQueries) {
            _queryFile = queryFile;
            _parameter = parameter;
            _userDefinedDB = existStructureQueries;
            Load();
            LoadExistingFiles();
        }

        public AlignmentSpotPropertyModel Spot { get; private set; }
        public StructureFinderBatchProcess StructureFinderBatchProcess { get; }

        public DelegateCommand RunFindFormula => _runFindFormula ??= new DelegateCommand(FindFormula);
        private DelegateCommand? _runFindFormula;

        public DelegateCommand RunFindStructure => _runFindStructure ??= new DelegateCommand(FindStructure);
        private DelegateCommand? _runFindStructure;

        private void Load() {
            _spotData = RawDataParcer.RawDataFileReader(_queryFile.RawDataFilePath, _parameter);
            ms1Spectrum = new MsSpectrum(_spotData.Ms1Spectrum);
            ms2Spectrum = new MsSpectrum(_spotData.Ms2Spectrum);
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
