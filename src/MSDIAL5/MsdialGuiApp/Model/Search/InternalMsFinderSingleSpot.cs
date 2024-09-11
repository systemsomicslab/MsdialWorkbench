using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.Information;
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
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Search {
    internal class InternalMsFinderSingleSpot : DisposableModelBase {
        private readonly AnalysisParamOfMsfinder? _parameter;
        private string _folderPath;
        private readonly RawData? _rawData;
        public List<FragmenterResult?> _structureData;
        private GraphLabels _msGraphLabels;
        public MsScanMatchResultContainer _msScanMatchResultContainer;
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
            get => _structureData;
            set {
                if (_structureData != value) {
                    _structureData = value;
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

        private void OnSelectedStructureChanged() {
            if (SelectedStructure != null) {
                var molecule = new MoleculeProperty();
                molecule.SMILES = SelectedStructure.Smiles;
                MoleculeStructureModel.UpdateMolecule(molecule);
            }
        }

        public InternalMsFinderSingleSpot(string tempDir, string filePath) {
            try {
                _parameter = new AnalysisParamOfMsfinder();
                _folderPath = tempDir;

                _rawData = RawDataParcer.RawDataFileReader(filePath, _parameter);
                _ms1SpectrumSubject = new Subject<MsSpectrum>().AddTo(Disposables);
                _ms2SpectrumSubject = new Subject<MsSpectrum>().AddTo(Disposables);
                var internalMsFinderMs1 = new ObservableMsSpectrum(_ms1SpectrumSubject, null, Observable.Return<ISpectraExporter?>(null));
                var internalMsFinderMs2 = new ObservableMsSpectrum(_ms2SpectrumSubject, null, Observable.Return<ISpectraExporter?>(null));
                var ms1HorizontalAxis = internalMsFinderMs1.CreateAxisPropertySelectors(new PropertySelector<SpectrumPeak, double>(p => p.Mass), "m/z", "m/z");
                var ms1VerticalAxis = internalMsFinderMs1.CreateAxisPropertySelectors2(new PropertySelector<SpectrumPeak, double>(p => p.Intensity), "Intensity");
                var ms2HorizontalAxis = internalMsFinderMs2.CreateAxisPropertySelectors(new PropertySelector<SpectrumPeak, double>(p => p.Mass), "m/z", "m/z");
                var ms2VerticalAxis = internalMsFinderMs2.CreateAxisPropertySelectors2(new PropertySelector<SpectrumPeak, double>(p => p.Intensity), "Intensity");
                _msGraphLabels = new GraphLabels(string.Empty, "m/z", "Abundance", nameof(SpectrumPeak.Mass), nameof(SpectrumPeak.Intensity));
                SpectrumModelMs1 = new SingleSpectrumModel(internalMsFinderMs1, ms1HorizontalAxis, ms1VerticalAxis, new ChartHueItem(string.Empty, new ConstantBrushMapper(Brushes.Black)), _msGraphLabels).AddTo(Disposables);
                SpectrumModelMs2 = new SingleSpectrumModel(internalMsFinderMs2, ms2HorizontalAxis, ms2VerticalAxis, new ChartHueItem(string.Empty, new ConstantBrushMapper(Brushes.Black)), _msGraphLabels).AddTo(Disposables);
                
                FindFormula();
                MoleculeStructureModel = new MoleculeStructureModel().AddTo(Disposables);
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message);
            }
        }

        public SingleSpectrumModel spectrumModelMs1 { get; set; }
        public SingleSpectrumModel spectrumModelMs2 { get; set; }
        public SingleSpectrumModel experimentSpectrum { get; }
        public SingleSpectrumModel referenceSpectrum {
            get => _referenceSpectrum;

        public SingleSpectrumModel SpectrumModelMs1 { get; }
        public SingleSpectrumModel SpectrumModelMs2 { get; }
        public MsSpectrumModel RefMs2SpectrumModel {
            get => _refMs2SpectrumModel;
            set {
                if (_refMs2SpectrumModel != value) {
                    _refMs2SpectrumModel = value;
                    OnPropertyChanged(nameof(RefMs2SpectrumModel));
                }
            }
        }
        private MsSpectrumModel? _refMs2SpectrumModel;
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
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message);
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
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message);
            }
        }

        public Task ClearAsync(CancellationToken token = default) {
            throw new NotImplementedException();
        }
        public DelegateCommand ReflectToMsdial => _reflectToMsdial ??= new DelegateCommand(ReflectToMsdialAsync);
        private DelegateCommand? _reflectToMsdial;
        public void ReflectToMsdialAsync() {
            if (SelectedStructure is not null) {
                var msScanMatchResult = new MsScanMatchResult();
                msScanMatchResult.Name = SelectedStructure.Title;
                msScanMatchResult.InChIKey = SelectedStructure.Inchikey;
                msScanMatchResult.TotalScore = ((float)SelectedStructure.TotalScore);
                _msScanMatchResultContainer = new MsScanMatchResultContainer();
                _msScanMatchResultContainer.AddResult(msScanMatchResult);
            }
        }
    }
}