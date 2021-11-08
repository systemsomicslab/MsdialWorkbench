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
            LipidQueries = new LipidQueryBean{
                SolventType = SolventType.CH3COONH4,
                LbmQueries = LbmQueryParcer.GetLbmQueries(false),
            };

            scanCollections = new ObservableCollection<IScanCollection>();
            ScanCollections = new ReadOnlyObservableCollection<IScanCollection>(scanCollections);

            spectrumModels = new ObservableCollection<SpectrumModel>();
            SpectrumModels = new ReadOnlyObservableCollection<SpectrumModel>(spectrumModels);
            spectrumModels.Add(new SpectrumModel(spectrumModelSerialNumber++));
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

        public void AddSpectrumModel() {
            spectrumModels.Add(new SpectrumModel(spectrumModelSerialNumber++));
        }

        public void RemoveSpectrumModel(SpectrumModel spectrumModel) {
            spectrumModels.Remove(spectrumModel);
        }
    }
}
