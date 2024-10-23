using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Information;
using CompMs.App.Msdial.Utility;
using CompMs.App.Msdial.View.Search;
using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Ion;
using CompMs.Common.DataObj.Result;
using CompMs.Common.FormulaGenerator;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.FormulaGenerator.Parser;
using CompMs.Common.Parameter;
using CompMs.Common.StructureFinder.DataObj;
using CompMs.Common.StructureFinder.Parser;
using CompMs.Common.StructureFinder.Result;
using CompMs.Common.Utility;
using CompMs.CommonMVVM;
using CompMs.Graphics.Design;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using MessagePack;
using Reactive.Bindings.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Search {
    internal class InternalMsFinderSingleSpot : DisposableModelBase {
        private readonly AnalysisParamOfMsfinder? _parameter;
        private string _folderPath;
        private readonly RawData? _rawData;
        public List<FragmenterResult?> _structureList;
        private GraphLabels _msGraphLabels;
        public MsScanMatchResultContainerModel _msScanMatchResultContainer;
        public ChromatogramPeakFeatureModel _chromatogram;
        private Subject<MsSpectrum> _ms1SpectrumSubject;
        private Subject<MsSpectrum> _ms2SpectrumSubject;

        private static readonly List<ProductIon> productIonDB = CompMs.Common.FormulaGenerator.Parser.FragmentDbParser.GetProductIonDB(
            @"Resources\msfinderLibrary\ProductIonLib_vs1.pid", out string _);
        private static readonly List<NeutralLoss> neutralLossDB = CompMs.Common.FormulaGenerator.Parser.FragmentDbParser.GetNeutralLossDB(
            @"Resources\msfinderLibrary\NeutralLossDB_vs2.ndb", out string _);
        private static readonly List<ExistFormulaQuery> existFormulaDB = ExistFormulaDbParcer.ReadExistFormulaDB(
            @"Resources\msfinderLibrary\MsfinderFormulaDB-VS13.efd", out string _);

        private static readonly List<ExistStructureQuery> mineStructureDB = FileStorageUtility.GetMinesStructureDB();
        private static readonly List<FragmentOntology> fragmentOntologyDB = FileStorageUtility.GetUniqueFragmentDB();
        private static List<MoleculeMsReference> mspDB = new List<MoleculeMsReference>();
        private List<ExistStructureQuery> userDefinedStructureDB;
        private static readonly List<FragmentLibrary> eiFragmentDB = FileStorageUtility.GetEiFragmentDB();
        private static readonly List<ExistStructureQuery> existStructureDB = FileStorageUtility.GetExistStructureDB();

        public List<FormulaResult>? FormulaList { get; private set; }
        public List<FragmenterResult>? StructureList {
            get => _structureList;
            set {
                if (_structureList != value) {
                    _structureList = value;
                    OnPropertyChanged(nameof(StructureList));
                }
            }
        }
        private FragmenterResult? _selectedStructure;
        public FragmenterResult? SelectedStructure {
            get => _selectedStructure;
            set {
                if (_selectedStructure != value) {
                    _selectedStructure = value;
                    OnPropertyChanged(nameof(SelectedStructure));
                    OnSelectedStructureChanged();
                }
            }
        }

        private List<SpectrumPeak> _refMs2;
        private List<SpectrumPeak> RefMs2 {
            get => _refMs2;
            set {
                if (_refMs2 != value) {
                    _refMs2 = value;
                    OnPropertyChanged(nameof(RefMs2));
                }
            }
        }

        private List<SpectrumPeak> _spectrumPeaks;
        private List<SpectrumPeak> SpectrumPeaks {
            get => _spectrumPeaks;
            set {
                if (_spectrumPeaks != value) {
                    _spectrumPeaks = value;
                    OnPropertyChanged(nameof(SpectrumPeaks));
                }
            }
        }

        private void OnSelectedStructureChanged() {
            if (SelectedStructure is not null) {
                var molecule = new MoleculeProperty();
                molecule.SMILES = SelectedStructure.Smiles;
                MoleculeStructureModel.UpdateMolecule(molecule);
                
                if (SelectedStructure.FragmentPics is not null) {
                    foreach (var fragment in SelectedStructure.FragmentPics) {
                        SpectrumPeaks.Add(fragment.Peak);
                    }
                }
                if (SelectedStructure.ReferenceSpectrum is not null) {
                    SpectrumPeaks = SelectedStructure.ReferenceSpectrum;
                }
                foreach (var peak in SpectrumPeaks) {
                    RefMs2.Add(peak);
                }
            }
        }

        public InternalMsFinderSingleSpot(string tempDir, string filePath, ChromatogramPeakFeatureModel chromatogram) {
            try {
                _parameter = new AnalysisParamOfMsfinder();
                _folderPath = tempDir;
                _msScanMatchResultContainer = chromatogram.MatchResultsModel;
                _chromatogram = chromatogram;

                _rawData = RawDataParcer.RawDataFileReader(filePath, _parameter);
                _ms1SpectrumSubject = new Subject<MsSpectrum>().AddTo(Disposables);
                _ms2SpectrumSubject = new Subject<MsSpectrum>().AddTo(Disposables);

                var internalMsFinderMs1 = new ObservableMsSpectrum(_ms1SpectrumSubject, null, Observable.Return<ISpectraExporter?>(null));
                var ms1HorizontalAxis = internalMsFinderMs1.CreateAxisPropertySelectors(new PropertySelector<SpectrumPeak, double>(p => p.Mass), "m/z", "m/z");
                var ms1VerticalAxis = internalMsFinderMs1.CreateAxisPropertySelectors2(new PropertySelector<SpectrumPeak, double>(p => p.Intensity), "Intensity");

                var internalMsFinderMs2 = new ObservableMsSpectrum(_ms2SpectrumSubject, null, Observable.Return<ISpectraExporter?>(null));
                var ms2HorizontalAxis = internalMsFinderMs2.CreateAxisPropertySelectors(new PropertySelector<SpectrumPeak, double>(p => p.Mass), "m/z", "m/z");
                var ms2VerticalAxis = internalMsFinderMs2.CreateAxisPropertySelectors2(new PropertySelector<SpectrumPeak, double>(p => p.Intensity), "Intensity");                             

                var ms2Spectrum = new ObservableMsSpectrum(Observable.Return(new MsSpectrum(_rawData.Ms2Spectrum)), null, Observable.Return<ISpectraExporter?>(null));
                var ms2VerticalAxis2 = ms2Spectrum.CreateAxisPropertySelectors2(new PropertySelector<SpectrumPeak, double>(p => p.Intensity), "Intensity");

                RefMs2 = new List<SpectrumPeak>();
                SpectrumPeaks = new List<SpectrumPeak>();
                foreach (var peak in _rawData.Ms2Spectrum) {
                    RefMs2.Add(peak);
                }

                var observableRefSpectrum = new ObservableMsSpectrum(Observable.Return(new MsSpectrum(SpectrumPeaks)), null, Observable.Return<ISpectraExporter?>(null));
                var refVerticalAxis = observableRefSpectrum.CreateAxisPropertySelectors2(new PropertySelector<SpectrumPeak, double>(p => p.Intensity), "Intensity");

                var refMs2Spectrum = new ObservableMsSpectrum(Observable.Return(new MsSpectrum(RefMs2)), null, Observable.Return<ISpectraExporter?>(null));
                var refMs2HorizontalAxis = refMs2Spectrum.CreateAxisPropertySelectors(new PropertySelector<SpectrumPeak, double>(p => p.Mass), "m/z", "m/z");

                FindFormula();
                MoleculeStructureModel = new MoleculeStructureModel().AddTo(Disposables);

                _msGraphLabels = new GraphLabels(string.Empty, "m/z", "Abundance", nameof(SpectrumPeak.Mass), nameof(SpectrumPeak.Intensity));
                SpectrumModelMs1 = new SingleSpectrumModel(internalMsFinderMs1, ms1HorizontalAxis, ms1VerticalAxis, new ChartHueItem(string.Empty, new ConstantBrushMapper(Brushes.Black)), _msGraphLabels).AddTo(Disposables);
                SpectrumModelMs2 = new SingleSpectrumModel(internalMsFinderMs2, ms2HorizontalAxis, ms2VerticalAxis, new ChartHueItem(string.Empty, new ConstantBrushMapper(Brushes.Black)), _msGraphLabels).AddTo(Disposables);
                var ms2SpectrumModel = new SingleSpectrumModel(ms2Spectrum, refMs2HorizontalAxis, ms2VerticalAxis2, new ChartHueItem(string.Empty, new ConstantBrushMapper(Brushes.Black)), _msGraphLabels).AddTo(Disposables);
                var refSpectrumModel = new SingleSpectrumModel(observableRefSpectrum, refMs2HorizontalAxis, refVerticalAxis, new ChartHueItem(string.Empty, new ConstantBrushMapper(Brushes.Black)), _msGraphLabels).AddTo(Disposables);
                RefMs2SpectrumModel = new MsSpectrumModel(ms2SpectrumModel, refSpectrumModel, Observable.Return<Ms2ScanMatching?>(null)).AddTo(Disposables);

                _ms1SpectrumSubject.OnNext(new MsSpectrum(_rawData.Ms1Spectrum));
                _ms2SpectrumSubject.OnNext(new MsSpectrum(_rawData.Ms2Spectrum));
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message);
            }
        }

        public SingleSpectrumModel SpectrumModelMs1 { get; }
        public SingleSpectrumModel SpectrumModelMs2 { get; }
        public MsSpectrumModel RefMs2SpectrumModel { get; }
        public MoleculeStructureModel MoleculeStructureModel { get; }

        private void FindFormula() {
            try {
                var error = string.Empty;
                var formulaResults = MolecularFormulaFinder.GetMolecularFormulaList(productIonDB, neutralLossDB, existFormulaDB, _rawData, _parameter);
                FormulaList = formulaResults;
                foreach (var formulaResult in formulaResults) {
                    var formulaFileName = Path.Combine(_folderPath, formulaResult.Formula.FormulaString);
                    var formulaFilePath = Path.ChangeExtension(formulaFileName, ".fgt");
                    FormulaResultParcer.FormulaResultsWriter(formulaFilePath, formulaResults);
                }
            } catch (Exception ex) {
                MessageBox.Show($"Error occurred in formula finder:{ex.Message}");
            }
        }

        public DelegateCommand RunFindStructure => _runFindStructure ??= new DelegateCommand(FindStructure);
        private DelegateCommand? _runFindStructure;

        private void FindStructure() {
            try {
                var process = new StructureFinderBatchProcess();
                process.DirectSingleSearchOfStructureFinder(_rawData, FormulaList, _parameter, _folderPath, existStructureDB, userDefinedStructureDB, mineStructureDB, fragmentOntologyDB, mspDB, eiFragmentDB);
                var structureFilePath = Directory.GetFiles(_folderPath, "*.sfd");
                var updatedStructureList = new List<FragmenterResult>();
                foreach (var file in structureFilePath) {
                    var fragmenterResults = FragmenterResultParser.FragmenterResultReader(file);
                    if (fragmenterResults != null) {
                        updatedStructureList.AddRange(fragmenterResults);
                    }
                }
                StructureList = updatedStructureList;
            } catch (Exception ex) {
                MessageBox.Show($"Error occurred in structure finder:{ex.Message}");
            }
        }
        public DelegateCommand ShowRawMs1SpectrumCommand => _showRawMs1SpectrumCommand ??= new DelegateCommand(ShowRawMs1Spectrum);
        private DelegateCommand? _showRawMs1SpectrumCommand;
        public void ShowRawMs1Spectrum() {
            if (_rawData.Ms1Spectrum is null) {  return; }
            _ms1SpectrumSubject.OnNext(new MsSpectrum(_rawData.Ms1Spectrum));
        }

        public DelegateCommand ShowIsotopeSpectrumCommand => _showIsotopeSpectrumCommand ??= new DelegateCommand(ShowIsotopeSpectrum);
        private DelegateCommand? _showIsotopeSpectrumCommand;
        public void ShowIsotopeSpectrum() {
            if (_rawData.NominalIsotopicPeakList is null) { return; }
            var isotopeList = _rawData.NominalIsotopicPeakList;
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
            if (_rawData.Ms2Spectrum is null) { return; }
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
                    var spec = new SpectrumPeak() {
                        Mass = ion.PrecursorMz,
                        Intensity = ion.PrecursorIntensity,
                    };
                    var spec2 = new SpectrumPeak() {
                        Mass = ion.ProductMz,
                        Intensity = ion.ProductIntensity,
                    };
                    neutralLossSpectrum.Add(spec);
                    neutralLossSpectrum.Add(spec2);
                }
                _ms2SpectrumSubject.OnNext(new MsSpectrum(neutralLossSpectrum));
            }
        }

        public DelegateCommand ShowFseaResultViewerCommand => _showFseaResultViewerCommand ??= new DelegateCommand(ShowFseaResultViewer);
        private DelegateCommand? _showFseaResultViewerCommand;
        public void ShowFseaResultViewer() {
            var ms2Spectrum = new MsSpectrum(_rawData.Ms2Spectrum);
            _ms2SpectrumSubject.OnNext(ms2Spectrum);
        }

        public DelegateCommand ShowSubstructureCommand => _showSubstructureCommand ??= new DelegateCommand(ShowSubstructure);
        private DelegateCommand? _showSubstructureCommand;
        public void ShowSubstructure() {
            Mouse.OverrideCursor = Cursors.Wait;
            foreach(var formula in FormulaList) {
                if (formula.IsSelected) {
                    var window = new InternalMsfinderSubstructureView(_rawData, formula, fragmentOntologyDB);
                    window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
                    window.Show();
                }
            }
            Mouse.OverrideCursor = null;
        }

        public Task ClearAsync(CancellationToken token = default) {
            throw new NotImplementedException();
        }
        public DelegateCommand ReflectToMsdial => _reflectToMsdial ??= new DelegateCommand(ReflectToMsdialAsync);
        private DelegateCommand? _reflectToMsdial;
        public void ReflectToMsdialAsync() {
            if (SelectedStructure is not null) {
                _msScanMatchResultContainer.AddResult(new MsScanMatchResult { AnnotatorID = "MS-FINDER", Source = SourceType.Manual, Priority = 1, Name = SelectedStructure.Title, InChIKey = SelectedStructure.Inchikey, 
                    TotalScore = ((float)SelectedStructure.TotalScore), AcurateMassSimilarity = ((float)SelectedStructure.TotalMaLikelihood), RtSimilarity = ((float)SelectedStructure.RtSimilarityScore), RiSimilarity = ((float)SelectedStructure.RiSimilarityScore)});
                var moleculeMsList = new List<MoleculeMsReference>();
                var moleculeMs = new MoleculeMsReference() {
                    ChromXs = _chromatogram.ChromXs, 
                    Spectrum = SpectrumPeaks, 
                    Formula = new CompMs.Common.DataObj.Property.Formula() { FormulaString = SelectedStructure.Formula }, 
                    AdductType = _chromatogram.AdductType
                };
                moleculeMsList.Add(moleculeMs);
                var moleculeMsCollection = new ObservableCollection<MoleculeMsReference>(moleculeMsList);
                var msfinderRefer = new MsfinderRefer(moleculeMsCollection, SelectedStructure.ID);
                var dataBaseMapper = new DataBaseMapper();
                dataBaseMapper.Add("MS-FINDER", msfinderRefer);
            } else {
                MessageBox.Show("Please select structure to reflect to the MS-DIAL.");
            }
        }
    }
    class MsfinderRefer : IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> {

        private bool _needSerialize;
        public MsfinderRefer(IEnumerable<MoleculeMsReference> source, string id) {
            Database = new MoleculeMsReferenceCollection(source.ToList());
            Id = id;
            _needSerialize = true;
        }

        [IgnoreMember]
        public MoleculeMsReferenceCollection Database { get; private set; }

        [Key(0)]
        public string Id { get; }

        string IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>.Key => Id;

        MoleculeMsReference IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>.Refer(MsScanMatchResult result) {
            if (result.LibraryID >= Database.Count || Database[result.LibraryID].ScanID != result.LibraryID) {
                return Database.FirstOrDefault(reference => reference.ScanID == result.LibraryID);
            }
            return Database[result.LibraryID];
        }
    }
}