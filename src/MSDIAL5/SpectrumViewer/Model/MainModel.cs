using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using CompMs.Common.Parser;
using CompMs.Common.Query;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Utility;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace CompMs.App.SpectrumViewer.Model
{
    public class MainModel : BindableBase {
        public MainModel() {
            LipidQueries = new LipidQueryBean {
                SolventType = SolventType.CH3COONH4,
                LbmQueries = LbmQueryParcer.GetLbmQueries(isLabUseOnly: true),
            };

            scanCollections = new ObservableCollection<IScanCollection>();
            ScanCollections = new ReadOnlyObservableCollection<IScanCollection>(scanCollections);

            spectrumModels = new ObservableCollection<SpectrumModel>();
            SpectrumModels = new ReadOnlyObservableCollection<SpectrumModel>(spectrumModels);
            spectrumModels.Add(new SpectrumModel(spectrumModelSerialNumber));

            splitSpectrumModels = new ObservableCollection<SplitSpectrumsModel>();
            SplitSpectrumModels = new ReadOnlyObservableCollection<SplitSpectrumsModel>(splitSpectrumModels);
            splitSpectrumModels.Add(new SplitSpectrumsModel(spectrumModelSerialNumber++));

            generatorEditorModels = new ObservableCollection<SpectrumGeneratorEditorModel>();
            GeneratorEditorModels = new ReadOnlyObservableCollection<SpectrumGeneratorEditorModel>(generatorEditorModels);
        }

        public LipidQueryBean LipidQueries { get; }

        public ReadOnlyObservableCollection<IScanCollection> ScanCollections { get; }

        private readonly ObservableCollection<IScanCollection> scanCollections;

        public void AddScanCollection(string name, IReadOnlyList<IMSScanProperty> collection) {
            scanCollections.Add(new ScanCollection(name, collection));
        }

        public void AddLipidReferenceGeneration() {
            scanCollections.Add(new LipidReferenceCollection());
        }

        public void RemoveScanCollection(IScanCollection collection) {
            scanCollections.Remove(collection);
        }

        public void FileOpen(FileOpenRequest request) {
            switch (Path.GetExtension(request.FileName)) {
                case ".lbm":
                case ".lbm2":
                    AddScanCollection(
                        Path.GetFileName(request.FileName),
                        LibraryHandler.ReadLipidMsLibrary(request.FileName, LipidQueries, LipidQueries.IonMode));
                    break;
                case ".msp":
                case ".msp2":
                    AddScanCollection(
                        Path.GetFileName(request.FileName),
                        LibraryHandler.ReadMspLibrary(request.FileName));
                    break;
            }
        }

        private int spectrumModelSerialNumber = 1;
        public ReadOnlyObservableCollection<SpectrumModel> SpectrumModels { get; }
        private readonly ObservableCollection<SpectrumModel> spectrumModels;
        public ReadOnlyObservableCollection<SplitSpectrumsModel> SplitSpectrumModels { get; }
        private readonly ObservableCollection<SplitSpectrumsModel> splitSpectrumModels;

        public void AddSpectrumModel() {
            spectrumModels.Add(new SpectrumModel(spectrumModelSerialNumber));
            splitSpectrumModels.Add(new SplitSpectrumsModel(spectrumModelSerialNumber++));
        }

        public void RemoveSpectrumModel(SpectrumModel spectrumModel) {
            spectrumModels.Remove(spectrumModel);
        }

        public void RemoveSpectrumModel(SplitSpectrumsModel splitSpectrumModel) {
            splitSpectrumModels.Remove(splitSpectrumModel);
        }

        public ReadOnlyObservableCollection<SpectrumGeneratorEditorModel> GeneratorEditorModels { get; }
        private readonly ObservableCollection<SpectrumGeneratorEditorModel> generatorEditorModels;

        public void AddSpectrumGeneratorEditorModel() {
            generatorEditorModels.Add(new SpectrumGeneratorEditorModel());
        }

        public void RemoveSpectrumGeneratorEditorModel(SpectrumGeneratorEditorModel editorModel) {
            generatorEditorModels.Remove(editorModel);
        }
    }
}
