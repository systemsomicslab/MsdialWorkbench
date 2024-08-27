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
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Search {
    internal class InternalMsFinderSingleSpot : DisposableModelBase {
        public AnalysisFileBeanModel _analysisFile;
        private readonly AnalysisParamOfMsfinder _parameter;
        private RawData? _rawData;
        public List<FormulaResult> _formulaData;
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
        private static readonly List<FragmentLibrary> eiFragmentDB = FileStorageUtility.GetEiFragmentDB();
        private static readonly List<ExistStructureQuery> existStructureDB = FileStorageUtility.GetExistStructureDB();

        public List<FormulaResult>? formulaList {
            get => _formulaData;
            set {
                if (_formulaData != value) {
                    _formulaData = value;
                    OnPropertyChanged(nameof(formulaList));
                }
            }
        }

        public List<FragmenterResult>? structureList {
            get => _structureData;
            set {
                if (_structureData != value) {
                    _structureData = value;
                    OnPropertyChanged(nameof(structureList));
                }
            }
        }

        public MsSpectrum ms1Spectrum { get; private set; }
        public MsSpectrum ms2Spectrum { get; private set; }

        public InternalMsFinderSingleSpot(string filePath) {
            try { 
                _parameter = new AnalysisParamOfMsfinder();
                _rawData = RawDataParcer.RawDataFileReader(filePath, _parameter);
                ms1Spectrum = new MsSpectrum(_rawData.Ms1Spectrum);
                ms2Spectrum = new MsSpectrum(_rawData.Ms2Spectrum);
                var ms1 = this.ObserveProperty(m => m.ms1Spectrum);
                var ms2 = this.ObserveProperty(m => m.ms2Spectrum);
                internalMsFinderMs1 = new ObservableMsSpectrum(ms1, null, Observable.Return<ISpectraExporter?>(null)).AddTo(Disposables);
                internalMsFinderMs2 = new ObservableMsSpectrum(ms2, null, Observable.Return<ISpectraExporter?>(null)).AddTo(Disposables);
                var ms1HorizontalAxis = internalMsFinderMs1.CreateAxisPropertySelectors(new PropertySelector<SpectrumPeak, double>(p => p.Mass), "m/z", "m/z");
                var ms1VerticalAxis = internalMsFinderMs2.CreateAxisPropertySelectors2(new PropertySelector<SpectrumPeak, double>(p => p.Intensity), "Intensity");
                var ms2HorizontalAxis = internalMsFinderMs1.CreateAxisPropertySelectors(new PropertySelector<SpectrumPeak, double>(p => p.Mass), "m/z", "m/z");
                var ms2VerticalAxis = internalMsFinderMs2.CreateAxisPropertySelectors2(new PropertySelector<SpectrumPeak, double>(p => p.Intensity), "Intensity");
                GraphLabels msGraphLabels = new GraphLabels(string.Empty, "m/z", "Abundance", nameof(SpectrumPeak.Mass), nameof(SpectrumPeak.Intensity));
            
                spectrumModelMs1 = new SingleSpectrumModel(internalMsFinderMs1, ms1HorizontalAxis, ms1VerticalAxis, new ChartHueItem(string.Empty, new ConstantBrushMapper(Brushes.Black)), msGraphLabels).AddTo(Disposables);
                spectrumModelMs2 = new SingleSpectrumModel(internalMsFinderMs2, ms2HorizontalAxis, ms2VerticalAxis, new ChartHueItem(string.Empty, new ConstantBrushMapper(Brushes.Black)), msGraphLabels).AddTo(Disposables);
                _formulaData = MolecularFormulaFinder.GetMolecularFormulaList(productIonDB, neutralLossDB, existFormulaDB, _rawData, _parameter);
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message);
            }
        }
        public ObservableMsSpectrum internalMsFinderMs1 { get; }
        public ObservableMsSpectrum internalMsFinderMs2 { get; }

        public SingleSpectrumModel spectrumModelMs1 { get; }
        public SingleSpectrumModel spectrumModelMs2 { get; }

        public DelegateCommand RunFindStructure => _runFindStructure ??= new DelegateCommand(FindStructure);
        private DelegateCommand? _runFindStructure;

        private void FindStructure() {
            var process = new StructureFinderBatchProcess();
            var parameter = new AnalysisParamOfMsfinder();
            var exportPath = string.Empty;
            process.DirectSingleSearchOfStructureFinder(_rawData, _formulaData, parameter, exportPath, existStructureDB, existStructureDB, mineStructureDB, fragmentOntologyDB, mspDB, eiFragmentDB);
            var structureFilePath = System.IO.Directory.GetFiles("*.sfd");
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