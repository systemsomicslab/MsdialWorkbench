using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using CompMs.Common.Parser;
using CompMs.Common.Query;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Utility;
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
        }

        public LipidQueryBean LipidQueries { get; }

        public ObservableCollection<IMSScanProperty> Scans {
            get => scans;
            private set => SetProperty(ref scans, value);
        }
        private ObservableCollection<IMSScanProperty> scans;

        public void FileOpen(FileOpenRequest request) {
            switch (Path.GetExtension(request.FileName)) {
                case ".lbm":
                case ".lbm2":
                    Scans = new ObservableCollection<IMSScanProperty>(LibraryHandler.ReadLipidMsLibrary(request.FileName, LipidQueries, LipidQueries.IonMode));
                    break;
                case ".msp":
                case ".msp2":
                    Scans = new ObservableCollection<IMSScanProperty>(LibraryHandler.ReadMspLibrary(request.FileName));
                    break;
            }
        }
    }
}
