﻿using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Information;
using CompMs.App.Msdial.View.Search;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Ion;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
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
using CompMs.Graphics.Core.Base;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows;
using System.Windows.Input;

namespace CompMs.App.Msdial.Model.Search {
    internal sealed class MsfinderObservedMetabolite : BindableBase {
        private readonly MsfinderQueryFile _queryFile;
        private readonly AnalysisParamOfMsfinder _parameter;
        public readonly List<ExistStructureQuery> userDefinedDB;
        public RawData _spotData;
        public ReactivePropertySlim<MsSpectrum?> _refSpectrum;
        public BehaviorSubject<AxisRange?> _spectrumRange;
        public BehaviorSubject<MsSpectrum> _ms1SpectrumSubject;
        public BehaviorSubject<MsSpectrum> _ms2SpectrumSubject;
        public MsScanMatchResultContainerModel _msScanMatchResultContainer;
        public ChromatogramPeakFeatureModel _chromatogram;

        private static readonly List<ProductIon> productIonDB = CompMs.Common.FormulaGenerator.Parser.FragmentDbParser.GetProductIonDB(
                @"Resources\msfinderLibrary\ProductIonLib_vs1.pid", out string _);
        private static readonly List<NeutralLoss> neutralLossDB = CompMs.Common.FormulaGenerator.Parser.FragmentDbParser.GetNeutralLossDB(
            @"Resources\msfinderLibrary\NeutralLossDB_vs2.ndb", out string _);
        private static readonly List<ExistFormulaQuery> existFormulaDB = ExistFormulaDbParcer.ReadExistFormulaDB(
            @"Resources\msfinderLibrary\MsfinderFormulaDB-VS13.efd", out string _);

        private static readonly List<ExistStructureQuery> mineStructureDB = FileStorageUtility.GetMinesStructureDB();
        private static readonly List<FragmentOntology> fragmentOntologyDB = FileStorageUtility.GetUniqueFragmentDB();
        private static readonly List<MoleculeMsReference> mspDB = [];
        private static readonly List<FragmentLibrary> eiFragmentDB = FileStorageUtility.GetEiFragmentDB();
        private static readonly List<ExistStructureQuery> existStructureDB = FileStorageUtility.GetExistStructureDB();
        public static readonly List<ChemicalOntology> chemicalOntologies = FileStorageUtility.GetChemicalOntologyDB();

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
                }
                if (IonMode == IonMode.Negative) {
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

        private List<FormulaResult> _formulaList;
        public List<FormulaResult> FormulaList {
            get => _formulaList;
            set => SetProperty(ref _formulaList, value);
        }

        private List<FragmenterResult> _structureList;
        public List<FragmenterResult> StructureList {
            get => _structureList;
            set => SetProperty(ref _structureList, value);
        }

        private FragmenterResult? _selectedStructure;
        public FragmenterResult? SelectedStructure {
            get => _selectedStructure;
            set {
                if (SetProperty(ref _selectedStructure, value)) {
                    OnSelectedStructureChanged();
                }
            }
        }

        private void OnSelectedStructureChanged() {
            if (SelectedStructure is not null) {
                var molecule = new MoleculeProperty {
                    SMILES = SelectedStructure.Smiles
                };
                MoleculeStructureModel.UpdateMolecule(molecule);

                if (SelectedStructure.FragmentPics is not null) {
                    var msSpectrum = new MsSpectrum(SelectedStructure.FragmentPics.Select(p => p.Peak).ToList());
                    _refSpectrum.Value = msSpectrum;
                    var (min, max) = msSpectrum.GetSpectrumRange(p => p.Mass);
                    _spectrumRange.OnNext(new AxisRange(min, max));
                }
                else if (SelectedStructure.ReferenceSpectrum is not null) {
                    var msSpectrum = new MsSpectrum(SelectedStructure.ReferenceSpectrum);
                    _refSpectrum.Value = msSpectrum;
                    var (min, max) = msSpectrum.GetSpectrumRange(p => p.Mass);
                    _spectrumRange.OnNext(new AxisRange(min, max));
                } else {
                    System.Diagnostics.Debug.Fail("Should not reach here.");
                    _refSpectrum.Value = null;
                    _spectrumRange.OnNext(null);
                }
            } else {
                _refSpectrum.Value = null;
                _spectrumRange.OnNext(null);
            }
        }

        public MsfinderObservedMetabolite(MsfinderQueryFile queryFile, AnalysisParamOfMsfinder parameter, List<ExistStructureQuery> existStructureQueries) {
            _queryFile = queryFile;
            _parameter = parameter;
            userDefinedDB = existStructureQueries;
            _formulaList = [];
            _structureList = [];

            MoleculeStructureModel = new MoleculeStructureModel();

            _spotData = RawDataParcer.RawDataFileReader(_queryFile.RawDataFilePath, _parameter);

            _ms1SpectrumSubject = new BehaviorSubject<MsSpectrum>(new MsSpectrum(_spotData.Ms1Spectrum));
            _ms2SpectrumSubject = new BehaviorSubject<MsSpectrum>(new MsSpectrum(_spotData.Ms2Spectrum));

            _refSpectrum = new ReactivePropertySlim<MsSpectrum?>(null);
            _spectrumRange = new BehaviorSubject<AxisRange?>(new AxisRange(0d, 1d));

            var error = string.Empty;
            if (_queryFile.FormulaFileExists) {
                _formulaList = FormulaResultParcer.FormulaResultReader(_queryFile.FormulaFilePath, out error);
            }
            if (_queryFile.StructureFileExists) {
                var structureFilePath = Directory.GetFiles(_queryFile.StructureFolderPath, "*.sfd");
                foreach (var file in structureFilePath) {
                    FragmenterResultParser.FragmenterResultReader(file);
                }
            }
        }

        public MoleculeStructureModel MoleculeStructureModel { get; }

        public DelegateCommand RunFindFormula => _runFindFormula ??= new DelegateCommand(FindFormula);
        private DelegateCommand? _runFindFormula;
        public void FindFormula() {
            Mouse.OverrideCursor = Cursors.Wait;
            if (_spotData is null || _parameter is null) return;
            var formulaResults = MolecularFormulaFinder.GetMolecularFormulaList(productIonDB, neutralLossDB, existFormulaDB, _spotData, _parameter);
            FormulaList = formulaResults;
            FormulaResultParcer.FormulaResultsWriter(_queryFile.FormulaFilePath, formulaResults);
            Mouse.OverrideCursor = null;
            if (FormulaList.Count == 0) {
                MessageBox.Show("No formula found");
            }
        }

        public DelegateCommand RunFindStructure => _runFindStructure ??= new DelegateCommand(FindStructure);
        private DelegateCommand? _runFindStructure;
        private void FindStructure() {
            Mouse.OverrideCursor = Cursors.Wait;
            if (_spotData is null || _parameter is null || FormulaList is null) return;
            var existingFilePaths = Directory.GetFiles(_queryFile.StructureFolderPath, "*.sfd");
            foreach (var file in existingFilePaths) {
                File.Delete(file);
            }
            var process = new StructureFinderBatchProcess();
            process.DirectSingleSearchOfStructureFinder(_spotData, FormulaList, _parameter, _queryFile.StructureFolderPath, existStructureDB, userDefinedDB, mineStructureDB, fragmentOntologyDB, mspDB, eiFragmentDB);
            var structureFilePaths = Directory.GetFiles(_queryFile.StructureFolderPath, "*.sfd");
            var updatedStructureList = new List<FragmenterResult>();
            foreach (var file in structureFilePaths) {
                var formula = Path.GetFileNameWithoutExtension(file);
                var fragmenterResults = FragmenterResultParser.FragmenterResultReader(file);
                foreach (var result in fragmenterResults.Where(r => !string.IsNullOrEmpty(r.Title))) {
                    result.Formula = formula;
                    updatedStructureList.Add(result);
                }
            }
            StructureList = updatedStructureList;
            Mouse.OverrideCursor = null;
            if (StructureList.Count == 0) {
                MessageBox.Show("No structure found");
            }
        }

        public DelegateCommand ShowRawMs1SpectrumCommand => _showRawMs1SpectrumCommand ??= new DelegateCommand(ShowRawMs1Spectrum);
        private DelegateCommand? _showRawMs1SpectrumCommand;
        public void ShowRawMs1Spectrum() {
            if (_spotData.Ms1Spectrum is null) { return; }
            _ms1SpectrumSubject.OnNext(new MsSpectrum(_spotData.Ms1Spectrum));
        }

        public DelegateCommand ShowIsotopeSpectrumCommand => _showIsotopeSpectrumCommand ??= new DelegateCommand(ShowIsotopeSpectrum);
        private DelegateCommand? _showIsotopeSpectrumCommand;
        public void ShowIsotopeSpectrum() {
            if (_spotData?.NominalIsotopicPeakList is null) { return; }
            var isotopeList = _spotData.NominalIsotopicPeakList;
            var peakList = new List<SpectrumPeak>();
            foreach (var isotope in isotopeList) {
                var spec = new SpectrumPeak() {
                    Mass = isotope.Mass,
                    Intensity = isotope.RelativeAbundance,
                    Comment = isotope.Comment,
                };
                peakList.Add(spec);
            }
            _ms1SpectrumSubject.OnNext(new MsSpectrum(peakList));
        }

        public DelegateCommand ShowRawMs2SpectrumCommand => _showRawMs2SpectrumCommand ??= new DelegateCommand(ShowRawMs2Spectrum);
        private DelegateCommand? _showRawMs2SpectrumCommand;
        public void ShowRawMs2Spectrum() {
            if (_spotData?.Ms2Spectrum is null) { return; }
            _ms2SpectrumSubject.OnNext(new MsSpectrum(_spotData.Ms2Spectrum));
        }

        public DelegateCommand ShowProductIonSpectrumCommand => _showProductIonSpectrumCommand ??= new DelegateCommand(ShowProductIonSpectrum);
        private DelegateCommand? _showProductIonSpectrumCommand;
        public void ShowProductIonSpectrum() {
            if (FormulaList is null) { return; }
            foreach (var formula in FormulaList) {
                var productIonSpectrum = new List<SpectrumPeak>();
                var productIonList = formula.ProductIonResult;
                foreach (var ion in productIonList) {
                    var spec = new SpectrumPeak() {
                        Mass = ion.Mass,
                        Intensity = ion.Intensity,
                        Comment = ion.Comment,
                    };
                    productIonSpectrum.Add(spec);
                }
                _ms2SpectrumSubject.OnNext(new MsSpectrum(productIonSpectrum));
            }
        }

        public DelegateCommand ShowNeutralLossSpectrumCommand => _showNeutralLossSpectrumCommand ??= new DelegateCommand(ShowNeutralLossSpectrum);
        private DelegateCommand? _showNeutralLossSpectrumCommand;
        public void ShowNeutralLossSpectrum() {
            if (FormulaList is null) { return; }
            foreach (var formula in FormulaList) {
                var neutralLossSpectrum = new List<SpectrumPeak>();
                var neutralLossList = formula.NeutralLossResult;
                foreach (var ion in neutralLossList) {
                    for (var i = 0; i < neutralLossList.Count; i++) {
                        SpectrumPeak spectrumPeak = new() {
                            Mass = ion.PrecursorMz,
                            Intensity = ion.PrecursorIntensity,
                        };
                        neutralLossSpectrum.Add(spectrumPeak);
                    }
                }
                _ms2SpectrumSubject.OnNext(new MsSpectrum(neutralLossSpectrum));
            }
        }

        public DelegateCommand ShowFseaResultViewerCommand => _showFseaResultViewerCommand ??= new DelegateCommand(ShowFseaResultViewer);
        private DelegateCommand? _showFseaResultViewerCommand;
        public void ShowFseaResultViewer() {
            Mouse.OverrideCursor = Cursors.Wait;
            if (_spotData is null || FormulaList is null) return;
            var vm = new FseaResultViewModel(FormulaList, chemicalOntologies, fragmentOntologyDB, _spotData.IonMode);
            var substructure = new FseaResultView() {
                DataContext = vm
            };
            substructure.Closed += (s, e) => vm.Dispose();
            substructure.Show();
            Mouse.OverrideCursor = null;
        }

        public DelegateCommand ShowSubstructureCommand => _showSubstructureCommand ??= new DelegateCommand(ShowSubstructure);
        private DelegateCommand? _showSubstructureCommand;
        public void ShowSubstructure() {
            Mouse.OverrideCursor = Cursors.Wait;
            if (_spotData is null || FormulaList is null) return;
            var vm = new InternalMsfinderSubstructure(FormulaList, fragmentOntologyDB);
            var substructure = new SubstructureView() {
                DataContext = vm
            };
            substructure.Closed += (s, e) => vm.Dispose();
            substructure.Show();
            Mouse.OverrideCursor = null;
        }

        public DelegateCommand ReflectToMsdial => _reflectToMsdial ??= new DelegateCommand(ReflectToMsdialAsync);
        private DelegateCommand? _reflectToMsdial;
        public void ReflectToMsdialAsync() {
            if (SelectedStructure is not null) {
                var moleculeMsReference = new MoleculeMsReference() {
                    ChromXs = new ChromXs() { RT = new RetentionTime(SelectedStructure.RetentionTime) },
                    Spectrum = _refSpectrum.Value?.Spectrum ?? [],
                    Formula = new Formula() { FormulaString = SelectedStructure.Formula },
                    AdductType = _chromatogram.AdductType,
                    PrecursorMz = SelectedStructure.PrecursorMz,
                    Name = SelectedStructure.Title,
                    InChIKey = SelectedStructure.Inchikey,
                    SMILES = SelectedStructure.Smiles,
                    Ontology = SelectedStructure.Ontology,
                };
                mspDB.Add(moleculeMsReference);
                var matchResult = new MsScanMatchResult {
                    AnnotatorID = "MS-FINDER",
                    Source = SourceType.Manual,
                    Name = SelectedStructure.Title,
                    InChIKey = SelectedStructure.Inchikey,
                    TotalScore = ((float)SelectedStructure.TotalScore),
                    AcurateMassSimilarity = ((float)SelectedStructure.TotalMaLikelihood),
                    RtSimilarity = ((float)SelectedStructure.RtSimilarityScore),
                    RiSimilarity = ((float)SelectedStructure.RiSimilarityScore),
                    LibraryID = moleculeMsReference.ScanID,
                };
                _msScanMatchResultContainer.AddResult(matchResult);
            }
            else
            {
                MessageBox.Show("Please select structure to reflect to the MS-DIAL.");
            }
        }
    }
}