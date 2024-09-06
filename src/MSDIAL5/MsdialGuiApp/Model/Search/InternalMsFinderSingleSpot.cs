using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Ion;
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
using CompMs.MsdialCore.Export;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Search {
    internal class InternalMsFinderSingleSpot : DisposableModelBase {
        private readonly AnalysisParamOfMsfinder _parameter;
        private string folderPath;
        private readonly RawData? rawData;
        public List<FragmenterResult> _structureData;

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

        public List<FormulaResult>? formulaList { get; set; }
        public List<FragmenterResult>? structureList {
            get => _structureData;
            set {
                if (_structureData != value) {
                    _structureData = value;
                    OnPropertyChanged(nameof(structureList));
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
            if (_selectedStructure != null) {
                var molecule = new MoleculeProperty();
                molecule.SMILES = _selectedStructure.Smiles;
                MoleculeStructureModel.UpdateMolecule(molecule);
            }
        }

        public MsSpectrum ms1Spectrum { get; }
        public MsSpectrum ms2Spectrum { get; }

        public InternalMsFinderSingleSpot(string tempDir, string filePath) {
            try {
                _parameter = new AnalysisParamOfMsfinder();
                folderPath = tempDir;

                rawData = RawDataParcer.RawDataFileReader(filePath, _parameter);
                ms1Spectrum = new MsSpectrum(rawData.Ms1Spectrum);
                ms2Spectrum = new MsSpectrum(rawData.Ms2Spectrum);
                var internalMsFinderMs1 = new ObservableMsSpectrum(this.ObserveProperty(m => m.ms1Spectrum), null, Observable.Return<ISpectraExporter?>(null));
                var internalMsFinderMs2 = new ObservableMsSpectrum(this.ObserveProperty(m => m.ms2Spectrum), null, Observable.Return<ISpectraExporter?>(null));
                var ms1HorizontalAxis = internalMsFinderMs1.CreateAxisPropertySelectors(new PropertySelector<SpectrumPeak, double>(p => p.Mass), "m/z", "m/z");
                var ms1VerticalAxis = internalMsFinderMs2.CreateAxisPropertySelectors2(new PropertySelector<SpectrumPeak, double>(p => p.Intensity), "Intensity");
                var ms2HorizontalAxis = internalMsFinderMs1.CreateAxisPropertySelectors(new PropertySelector<SpectrumPeak, double>(p => p.Mass), "m/z", "m/z");
                var ms2VerticalAxis = internalMsFinderMs2.CreateAxisPropertySelectors2(new PropertySelector<SpectrumPeak, double>(p => p.Intensity), "Intensity");
                var msGraphLabels = new GraphLabels(string.Empty, "m/z", "Abundance", nameof(SpectrumPeak.Mass), nameof(SpectrumPeak.Intensity));
                spectrumModelMs1 = new SingleSpectrumModel(internalMsFinderMs1, ms1HorizontalAxis, ms1VerticalAxis, new ChartHueItem(string.Empty, new ConstantBrushMapper(Brushes.Black)), msGraphLabels).AddTo(Disposables);
                spectrumModelMs2 = new SingleSpectrumModel(internalMsFinderMs2, ms2HorizontalAxis, ms2VerticalAxis, new ChartHueItem(string.Empty, new ConstantBrushMapper(Brushes.Black)), msGraphLabels).AddTo(Disposables);
                
                FindFormula();
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message);
            }
        }

        public SingleSpectrumModel spectrumModelMs1 { get; }
        public SingleSpectrumModel spectrumModelMs2 { get; }

        private void FindFormula() {
            try { 
                var error = string.Empty;
                var formulaResults = MolecularFormulaFinder.GetMolecularFormulaList(productIonDB, neutralLossDB, existFormulaDB, rawData, _parameter);
                formulaList = formulaResults;
                foreach (var formulaResult in formulaResults) { 
                    var formulaFileName = Path.Combine(folderPath, formulaResult.Formula.FormulaString);
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
                process.DirectSingleSearchOfStructureFinder(rawData, formulaList, _parameter, folderPath, existStructureDB, userDefinedStructureDB, mineStructureDB, fragmentOntologyDB, mspDB, eiFragmentDB);
                var structureFilePath = Directory.GetFiles(folderPath, "*.sfd");
                var updatedStructureList = new List<FragmenterResult>();
                foreach (var file in structureFilePath) {
                    var fragmenterResults = FragmenterResultParser.FragmenterResultReader(file);
                    if (fragmenterResults != null) {
                        updatedStructureList.AddRange(fragmenterResults);
                    }
                }
                structureList = updatedStructureList;
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message);
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