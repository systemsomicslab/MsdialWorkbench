﻿using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Information;
using CompMs.App.Msdial.View.Search;
using CompMs.App.Msdial.ViewModel.Search;
using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Ion;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Extension;
using CompMs.Common.FormulaGenerator;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.Common.FormulaGenerator.Parser;
using CompMs.Common.StructureFinder.DataObj;
using CompMs.Common.StructureFinder.Parser;
using CompMs.Common.Utility;
using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using CompMs.Graphics.Design;
using CompMs.Graphics.UI.Message;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Search
{
    internal class InternalMsFinderSingleSpot : DisposableModelBase {
        private readonly MsfinderParameterSetting? _parameter;
        private readonly string _folderPath;
        private readonly RawData? _rawData;
        private readonly BehaviorSubject<MsSpectrum> _ms1SpectrumSubject;
        private readonly BehaviorSubject<MsSpectrum> _ms2SpectrumSubject;
        private readonly ReactivePropertySlim<MsSpectrum?> _refSpectrum;
        private readonly BehaviorSubject<AxisRange?> _spectrumRange;
        private readonly MoleculeDataBase _molecules;
        private readonly string _filePath;
        private readonly SetAnnotationUsecase _setAnnotationUsecase;
        private readonly AdductIon _adduct;

        private static readonly List<ProductIon> productIonDB = FileStorageUtility.GetProductIonDB();
        private static readonly List<NeutralLoss> neutralLossDB = FileStorageUtility.GetNeutralLossDB();
        private static readonly List<ExistFormulaQuery> existFormulaDB = FileStorageUtility.GetExistFormulaDB();
        private static readonly List<ExistStructureQuery> mineStructureDB = FileStorageUtility.GetMinesStructureDB();
        private static readonly List<FragmentOntology> fragmentOntologyDB = FileStorageUtility.GetUniqueFragmentDB();
        private readonly List<MoleculeMsReference> mspDB = [];
        private readonly List<ExistStructureQuery> userDefinedStructureDB = [];
        private static readonly List<FragmentLibrary> eiFragmentDB = FileStorageUtility.GetEiFragmentDB();
        private static readonly List<ExistStructureQuery> existStructureDB = FileStorageUtility.GetExistStructureDB();
        private static readonly List<ChemicalOntology> chemicalOntologies = FileStorageUtility.GetChemicalOntologyDB();

        public List<FormulaResult>? FormulaList { get; private set; }
        private FormulaResult? _selectedFormula;
        public FormulaResult? SelectedFormula {
            get => _selectedFormula;
            set {
                if (SetProperty(ref _selectedFormula, value)) {
                    OnSelectedFormulaChanged();
                }
            }
        }

        private void OnSelectedFormulaChanged() {
            if (SelectedFormula is not null && StructureList?.Count > 0) {
                FilteredStructureList = StructureList.Where(s => s.Formula == SelectedFormula.Formula.FormulaString).ToList();
            } else {
                FilteredStructureList = [];
            }
        }

        private List<FragmenterResultVM> _filteredStructureList = [];
        public List<FragmenterResultVM> FilteredStructureList {
            get => _filteredStructureList;
            set => SetProperty(ref _filteredStructureList, value);
        }

        private List<FragmenterResultVM> _structureList = [];
        public List<FragmenterResultVM> StructureList {
            get => _structureList;
            set => SetProperty(ref _structureList, value);
        }
        private FragmenterResultVM? _selectedStructure;
        public FragmenterResultVM? SelectedStructure {
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
                
                if (SelectedStructure.FragmenterResult.FragmentPics is not null) {
                    foreach (var frag in SelectedStructure.FragmenterResult.FragmentPics) {
                        frag.Peak.FragmentationScore = frag.MatchedFragmentInfo.TotalLikelihood;
                        var label = MsfinderUtility.GetLabelForInsilicoSpectrum(frag.MatchedFragmentInfo.Formula, frag.MatchedFragmentInfo.RearrangedHydrogen, _adduct.IonMode, frag.MatchedFragmentInfo.AssignedAdductString);
                        frag.Peak.Comment = frag.MatchedFragmentInfo.Smiles;
                    }
                    var msSpectrum = new MsSpectrum(SelectedStructure.FragmenterResult.FragmentPics.Select(p => p.Peak).ToList());
                    _refSpectrum.Value = msSpectrum;
                    var (min, max) = msSpectrum.GetSpectrumRange(p => p.Mass);
                    _spectrumRange.OnNext(new AxisRange(min, max));
                }
                else if (SelectedStructure.FragmenterResult.ReferenceSpectrum is not null) {
                    var msSpectrum = new MsSpectrum(SelectedStructure.FragmenterResult.ReferenceSpectrum);
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

        public InternalMsFinderSingleSpot(string tempDir, string filePath, MoleculeDataBase molecules, MsfinderParameterSetting parameter, AdductIon adductType, SetAnnotationUsecase setAnnotationUsecase) {
            try {
                _parameter = parameter;
                _folderPath = tempDir;
                _adduct = adductType;
                _molecules = molecules;
                _filePath = filePath;
                _setAnnotationUsecase = setAnnotationUsecase;

                if (fragmentOntologyDB is not null && productIonDB is not null)
                    ChemOntologyDbParser.ConvertInChIKeyToChemicalOntology(productIonDB, fragmentOntologyDB);
                if (fragmentOntologyDB is not null && neutralLossDB is not null)
                    ChemOntologyDbParser.ConvertInChIKeyToChemicalOntology(neutralLossDB, fragmentOntologyDB);
                if (fragmentOntologyDB is not null && chemicalOntologies is not null)
                    ChemOntologyDbParser.ConvertInChIKeyToChemicalOntology(chemicalOntologies, fragmentOntologyDB);

                _rawData = RawDataParcer.RawDataFileReader(filePath, parameter.AnalysisParameter);

                string error = string.Empty;
                mspDB = FileStorageUtility.GetMspDB(parameter.AnalysisParameter, _rawData.IonMode, out error);
                if (error != string.Empty) {
                    MessageBox.Show(error, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                _ms1SpectrumSubject = new BehaviorSubject<MsSpectrum>(new MsSpectrum(_rawData.Ms1Spectrum)).AddTo(Disposables);
                _ms2SpectrumSubject = new BehaviorSubject<MsSpectrum>(new MsSpectrum(_rawData.Ms2Spectrum)).AddTo(Disposables);

                var internalMsFinderMs1 = new ObservableMsSpectrum(_ms1SpectrumSubject, null, Observable.Return<ISpectraExporter?>(null));
                var ms1HorizontalAxis = internalMsFinderMs1.CreateAxisPropertySelectors(new PropertySelector<SpectrumPeak, double>(p => p.Mass), "m/z", "m/z");
                var ms1VerticalAxis = internalMsFinderMs1.CreateAxisPropertySelectors2(new PropertySelector<SpectrumPeak, double>(p => p.Intensity), "abundance");

                var internalMsFinderMs2 = new ObservableMsSpectrum(_ms2SpectrumSubject, null, Observable.Return<ISpectraExporter?>(null));
                var ms2HorizontalAxis = internalMsFinderMs2.CreateAxisPropertySelectors(new PropertySelector<SpectrumPeak, double>(p => p.Mass), "m/z", "m/z");
                var ms2VerticalAxis = internalMsFinderMs2.CreateAxisPropertySelectors2(new PropertySelector<SpectrumPeak, double>(p => p.Intensity), "abundance");                             

                var ms2Spectrum = new ObservableMsSpectrum(Observable.Return(new MsSpectrum(_rawData.Ms2Spectrum)), null, Observable.Return<ISpectraExporter?>(null));
                var ms2VerticalAxis2 = ms2Spectrum.CreateAxisPropertySelectors2(new PropertySelector<SpectrumPeak, double>(p => p.Intensity), "abundance");

                var rawMs2Range = _rawData.Ms2Spectrum.IsEmptyOrNull()
                    ? null
                    : new AxisRange(_rawData.Ms2Spectrum.Min(p => p.Mass), _rawData.Ms2Spectrum.Max(p => p.Mass));

                _refSpectrum = new ReactivePropertySlim<MsSpectrum?>(null).AddTo(Disposables);
                var observableRefSpectrum = new ObservableMsSpectrum(_refSpectrum, null, Observable.Return<ISpectraExporter?>(null));
                var refVerticalAxis = observableRefSpectrum.CreateAxisPropertySelectors2(new PropertySelector<SpectrumPeak, double>(p => p.FragmentationScore), "fragmentation score");

                _spectrumRange = new BehaviorSubject<AxisRange?>(new AxisRange(0d, 1d)).AddTo(Disposables);
                var horizontalAxis = _spectrumRange.Select(range => AxisRange.Union(range, rawMs2Range) ?? new AxisRange(0d, 1d)).ToReactiveContinuousAxisManager<double>(new ConstantMargin(40d)).AddTo(Disposables);
                var itemSelector = new AxisItemSelector<double>(new AxisItemModel<double>("m/z", horizontalAxis, "m/z")).AddTo(Disposables);
                var propertySelectors = new AxisPropertySelectors<double>(itemSelector);
                propertySelectors.Register(new PropertySelector<SpectrumPeak, double>(p => p.Mass));
                var refMs2HorizontalAxis = propertySelectors;

                FindFormula();
                SelectedFormula = FormulaList.FirstOrDefault();
                MoleculeStructureModel = new MoleculeStructureModel().AddTo(Disposables);

                var _msGraphLabels = new GraphLabels(string.Empty, "m/z", "Abundance", nameof(SpectrumPeak.Mass), nameof(SpectrumPeak.Intensity));
                var _msGraphLabel2 = new GraphLabels(string.Empty, "m/z", "Fragment score", nameof(SpectrumPeak.Mass), nameof(SpectrumPeak.FragmentationScore));
                SpectrumModelMs1 = new SingleSpectrumModel(internalMsFinderMs1, ms1HorizontalAxis, ms1VerticalAxis, new ChartHueItem(string.Empty, new ConstantBrushMapper(Brushes.Black)), _msGraphLabels).AddTo(Disposables);
                SpectrumModelMs2 = new SingleSpectrumModel(internalMsFinderMs2, ms2HorizontalAxis, ms2VerticalAxis, new ChartHueItem(string.Empty, new ConstantBrushMapper(Brushes.Black)), _msGraphLabels).AddTo(Disposables);
                var ms2SpectrumModel = new SingleSpectrumModel(ms2Spectrum, refMs2HorizontalAxis, ms2VerticalAxis2, new ChartHueItem(string.Empty, new ConstantBrushMapper(Brushes.Blue)), _msGraphLabels).AddTo(Disposables);
                var refSpectrumModel = new SingleSpectrumModel(observableRefSpectrum, refMs2HorizontalAxis, refVerticalAxis, new ChartHueItem(string.Empty, new ConstantBrushMapper(Brushes.Red)), _msGraphLabel2).AddTo(Disposables);
                RefMs2SpectrumModel = new MsSpectrumModel(ms2SpectrumModel, refSpectrumModel, Observable.Return<Ms2ScanMatching?>(null)).AddTo(Disposables);
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message);
                throw;
            }
        }

        public InternalMsFinderSingleSpot(string tempDir, string filePath, MoleculeDataBase molecules, MsfinderParameterSetting parameter, SetAnnotationUsecase setAnnotationUsecase) {
            try {
                _parameter = parameter;
                _folderPath = tempDir;
                _filePath = filePath;
                _molecules = molecules;
                _setAnnotationUsecase = setAnnotationUsecase;

                if (fragmentOntologyDB is not null && productIonDB is not null)
                    ChemOntologyDbParser.ConvertInChIKeyToChemicalOntology(productIonDB, fragmentOntologyDB);
                if (fragmentOntologyDB is not null && neutralLossDB is not null)
                    ChemOntologyDbParser.ConvertInChIKeyToChemicalOntology(neutralLossDB, fragmentOntologyDB);
                if (fragmentOntologyDB is not null && chemicalOntologies is not null)
                    ChemOntologyDbParser.ConvertInChIKeyToChemicalOntology(chemicalOntologies, fragmentOntologyDB);

                _rawData = RawDataParcer.RawDataFileReader(filePath, parameter.AnalysisParameter);

                string error = string.Empty;
                mspDB = FileStorageUtility.GetMspDB(parameter.AnalysisParameter, _rawData.IonMode, out error); 
                if (error != string.Empty) {
                    MessageBox.Show(error, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                _adduct = AdductIon.GetAdductIon(_rawData.PrecursorType);
                _ms1SpectrumSubject = new BehaviorSubject<MsSpectrum>(new MsSpectrum(_rawData.Ms1Spectrum)).AddTo(Disposables);
                _ms2SpectrumSubject = new BehaviorSubject<MsSpectrum>(new MsSpectrum(_rawData.Ms2Spectrum)).AddTo(Disposables);

                var internalMsFinderMs1 = new ObservableMsSpectrum(_ms1SpectrumSubject, null, Observable.Return<ISpectraExporter?>(null));
                var ms1HorizontalAxis = internalMsFinderMs1.CreateAxisPropertySelectors(new PropertySelector<SpectrumPeak, double>(p => p.Mass), "m/z", "m/z");
                var ms1VerticalAxis = internalMsFinderMs1.CreateAxisPropertySelectors2(new PropertySelector<SpectrumPeak, double>(p => p.Intensity), "abundance");

                var internalMsFinderMs2 = new ObservableMsSpectrum(_ms2SpectrumSubject, null, Observable.Return<ISpectraExporter?>(null));
                var ms2HorizontalAxis = internalMsFinderMs2.CreateAxisPropertySelectors(new PropertySelector<SpectrumPeak, double>(p => p.Mass), "m/z", "m/z");
                var ms2VerticalAxis = internalMsFinderMs2.CreateAxisPropertySelectors2(new PropertySelector<SpectrumPeak, double>(p => p.Intensity), "abundance");

                var ms2Spectrum = new ObservableMsSpectrum(Observable.Return(new MsSpectrum(_rawData.Ms2Spectrum)), null, Observable.Return<ISpectraExporter?>(null));
                var ms2VerticalAxis2 = ms2Spectrum.CreateAxisPropertySelectors2(new PropertySelector<SpectrumPeak, double>(p => p.Intensity), "abundance");

                var rawMs2Range = _rawData.Ms2Spectrum.IsEmptyOrNull()
                    ? null
                    : new AxisRange(_rawData.Ms2Spectrum.Min(p => p.Mass), _rawData.Ms2Spectrum.Max(p => p.Mass));

                _refSpectrum = new ReactivePropertySlim<MsSpectrum?>(null).AddTo(Disposables);
                var observableRefSpectrum = new ObservableMsSpectrum(_refSpectrum, null, Observable.Return<ISpectraExporter?>(null));
                var refVerticalAxis = observableRefSpectrum.CreateAxisPropertySelectors2(new PropertySelector<SpectrumPeak, double>(p => p.FragmentationScore), "fragmentation score");

                _spectrumRange = new BehaviorSubject<AxisRange?>(new AxisRange(0d, 1d)).AddTo(Disposables);
                var horizontalAxis = _spectrumRange.Select(range => AxisRange.Union(range, rawMs2Range) ?? new AxisRange(0d, 1d)).ToReactiveContinuousAxisManager<double>(new ConstantMargin(40d)).AddTo(Disposables);
                var itemSelector = new AxisItemSelector<double>(new AxisItemModel<double>("m/z", horizontalAxis, "m/z")).AddTo(Disposables);
                var propertySelectors = new AxisPropertySelectors<double>(itemSelector);
                propertySelectors.Register(new PropertySelector<SpectrumPeak, double>(p => p.Mass));
                var refMs2HorizontalAxis = propertySelectors;

                FindFormula();
                SelectedFormula = FormulaList.FirstOrDefault();
                MoleculeStructureModel = new MoleculeStructureModel().AddTo(Disposables);

                var _msGraphLabels = new GraphLabels(string.Empty, "m/z", "Abundance", nameof(SpectrumPeak.Mass), nameof(SpectrumPeak.Intensity));
                var _msGraphLabel2 = new GraphLabels(string.Empty, "m/z", "Fragment score", nameof(SpectrumPeak.Mass), nameof(SpectrumPeak.FragmentationScore));
                SpectrumModelMs1 = new SingleSpectrumModel(internalMsFinderMs1, ms1HorizontalAxis, ms1VerticalAxis, new ChartHueItem(string.Empty, new ConstantBrushMapper(Brushes.Black)), _msGraphLabels).AddTo(Disposables);
                SpectrumModelMs2 = new SingleSpectrumModel(internalMsFinderMs2, ms2HorizontalAxis, ms2VerticalAxis, new ChartHueItem(string.Empty, new ConstantBrushMapper(Brushes.Black)), _msGraphLabels).AddTo(Disposables);
                var ms2SpectrumModel = new SingleSpectrumModel(ms2Spectrum, refMs2HorizontalAxis, ms2VerticalAxis2, new ChartHueItem(string.Empty, new ConstantBrushMapper(Brushes.Blue)), _msGraphLabels).AddTo(Disposables);
                var refSpectrumModel = new SingleSpectrumModel(observableRefSpectrum, refMs2HorizontalAxis, refVerticalAxis, new ChartHueItem(string.Empty, new ConstantBrushMapper(Brushes.Red)), _msGraphLabel2).AddTo(Disposables);
                RefMs2SpectrumModel = new MsSpectrumModel(ms2SpectrumModel, refSpectrumModel, Observable.Return<Ms2ScanMatching?>(null)).AddTo(Disposables);
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message);
                throw;
            }
        }
        public SingleSpectrumModel SpectrumModelMs1 { get; }
        public SingleSpectrumModel SpectrumModelMs2 { get; }
        public MsSpectrumModel RefMs2SpectrumModel { get; }
        public MoleculeStructureModel MoleculeStructureModel { get; }

        private void FindFormula() {
            Mouse.OverrideCursor = Cursors.Wait;
            if (_rawData is null || _parameter is null) return;
            var formulaResults = MolecularFormulaFinder.GetMolecularFormulaList(productIonDB, neutralLossDB, existFormulaDB, _rawData, _parameter.AnalysisParameter);
            ChemicalOntologyAnnotation.ProcessByOverRepresentationAnalysis(formulaResults, chemicalOntologies, _rawData.IonMode, _parameter.AnalysisParameter, _adduct, productIonDB, neutralLossDB);
            FormulaList = formulaResults;
            foreach (var formulaResult in formulaResults) {
                var formulaFileName = Path.Combine(_folderPath, formulaResult.Formula.FormulaString);
                var formulaFilePath = Path.ChangeExtension(formulaFileName, ".fgt");
                FormulaResultParcer.FormulaResultsWriter(formulaFilePath, formulaResults);
            }
            Mouse.OverrideCursor = null;
            if (FormulaList.Count == 0) {
                MessageBox.Show("No formula found");
            }
        }

        public DelegateCommand RunFindStructure => _runFindStructure ??= new DelegateCommand(FindStructure);
        private DelegateCommand? _runFindStructure;

        private void FindStructure() {
            Mouse.OverrideCursor = Cursors.Wait;
            if (_rawData is null || _parameter is null || FormulaList is null) return;
            var existingFilePaths = Directory.GetFiles(_folderPath, "*.sfd");
            foreach (var file in existingFilePaths) {
                File.Delete(file);
            }
            var process = new StructureFinderBatchProcess();
            process.DirectSingleSearchOfStructureFinder(_rawData, FormulaList, _parameter.AnalysisParameter, _folderPath, existStructureDB, userDefinedStructureDB, mineStructureDB, fragmentOntologyDB, mspDB, eiFragmentDB);
            var structureFilePaths = Directory.GetFiles(_folderPath, "*.sfd");
            var updatedStructureList = new List<FragmenterResultVM>();
            foreach (var file in structureFilePaths) {
                var formula = Path.GetFileNameWithoutExtension(file);
                var fragmenterResults = FragmenterResultParser.FragmenterResultReader(file);
                foreach (var result in fragmenterResults.Where(r => !string.IsNullOrEmpty(r.Title))) {
                    result.Formula = formula;
                    var resultVM = new FragmenterResultVM(false, result);
                    updatedStructureList.Add(resultVM);
                }
            }
            StructureList = updatedStructureList;
            FilteredStructureList = [.. StructureList.Where(s => s.Formula == StructureList.FirstOrDefault().Formula)];
            SelectedStructure = FilteredStructureList.FirstOrDefault();
            Mouse.OverrideCursor = null;
            if (StructureList.Count == 0) {
                MessageBox.Show("No structure found");
            }
        }
        public DelegateCommand ShowRawMs1SpectrumCommand => _showRawMs1SpectrumCommand ??= new DelegateCommand(ShowRawMs1Spectrum);
        private DelegateCommand? _showRawMs1SpectrumCommand;
        public void ShowRawMs1Spectrum() {
            if (_rawData?.Ms1Spectrum is null) {  return; }
            _ms1SpectrumSubject.OnNext(new MsSpectrum(_rawData.Ms1Spectrum));
        }

        public DelegateCommand ShowIsotopeSpectrumCommand => _showIsotopeSpectrumCommand ??= new DelegateCommand(ShowIsotopeSpectrum);
        private DelegateCommand? _showIsotopeSpectrumCommand;
        public void ShowIsotopeSpectrum() {
            if (SelectedFormula is null|| _rawData is null) { return; }
            MsfinderUtility.GetExperimentalIsotopicIons(_rawData.PrecursorMz, _rawData.Ms1Spectrum, out var precursorIntensity);
            var isotopicIons = MsfinderUtility.GetTheoreticalIsotopicIons(SelectedFormula, _rawData.PrecursorType, precursorIntensity);
            if (isotopicIons is not null) {
                _ms1SpectrumSubject.OnNext(new MsSpectrum(isotopicIons));
            }
        }

        public DelegateCommand ShowRawMs2SpectrumCommand => _showRawMs2SpectrumCommand ??= new DelegateCommand(ShowRawMs2Spectrum);
        private DelegateCommand? _showRawMs2SpectrumCommand;
        public void ShowRawMs2Spectrum() {
            if (_rawData?.Ms2Spectrum is null) { return; }
            _ms2SpectrumSubject.OnNext(new MsSpectrum(_rawData.Ms2Spectrum));
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
            if (_rawData is null || FormulaList is null) return;
            foreach (var formula in FormulaList) {
                if (formula.ChemicalOntologyDescriptions is null || formula.ChemicalOntologyDescriptions.Count == 0) {
                    MessageBox.Show("No chemical ontology description found.");
                    Mouse.OverrideCursor = null;
                    return;
                }
            }
            var vm = new FseaResultViewModel(FormulaList, chemicalOntologies, fragmentOntologyDB, _rawData.IonMode);
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
            var message = new ShortMessageWindow() {
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Title = "Preparing the substructure view...",
                Width = 400,
                Height = 100
            };
            message.Show();
            if (_rawData is null || FormulaList is null) return;
            var vm = new InternalMsfinderSubstructure(FormulaList, fragmentOntologyDB);
            var substructure = new SubstructureView() {
                DataContext = vm
            };
            substructure.Closed += (s, e) => vm.Dispose();
            substructure.Show();
            message.Close();
            Mouse.OverrideCursor = null;
        }

        public DelegateCommand ReflectToMsdialCommand => _reflectToMsdialCommand ??= new DelegateCommand(ReflectToMsdial);
        private DelegateCommand? _reflectToMsdialCommand;
        public void ReflectToMsdial() {
            if (SelectedStructure is not null) {
                var moleculeMsReference = new MoleculeMsReference() {
                    ScanID = _molecules.Database.Count + 1,
                    ChromXs = new ChromXs() { RT = new RetentionTime(SelectedStructure.FragmenterResult.RetentionTime) },
                    Spectrum = _refSpectrum.Value?.Spectrum ?? [],
                    Formula = new Formula() { FormulaString = SelectedStructure.FragmenterResult.Formula },
                    AdductType = _adduct,
                    PrecursorMz = SelectedStructure.FragmenterResult.PrecursorMz,
                    Name = SelectedStructure.FragmenterResult.Title,
                    InChIKey = SelectedStructure.FragmenterResult.Inchikey,
                    SMILES = SelectedStructure.Smiles,
                    Ontology = SelectedStructure.Ontology,
                };
                _molecules.Database.Add(moleculeMsReference);
                var matchResult = new MsScanMatchResult {
                    AnnotatorID = _molecules.Id,
                    Source = _molecules.SourceType | SourceType.Manual,
                    Name = SelectedStructure.FragmenterResult.Title,
                    InChIKey = SelectedStructure.FragmenterResult.Inchikey,
                    TotalScore = ((float)SelectedStructure.TotalScore),
                    RtSimilarity = ((float)SelectedStructure.FragmenterResult.RtSimilarityScore),
                    RiSimilarity = ((float)SelectedStructure.FragmenterResult.RiSimilarityScore),
                    LibraryID = moleculeMsReference.ScanID,
                };
                _setAnnotationUsecase.SetConfidence(moleculeMsReference, matchResult);
            } else {
                MessageBox.Show("Please select structure to reflect to the MS-DIAL.");
            }
        }

        protected override void Dispose(bool disposing) {
            if (!disposedValue) {
                if (File.Exists(_filePath)) {
                    File.Delete(_filePath);
                }
            }
            base.Dispose(disposing);
        }
    }
}