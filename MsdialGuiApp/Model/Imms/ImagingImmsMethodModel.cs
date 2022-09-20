using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.Imaging;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Imms
{
    internal sealed class ImagingImmsMethodModel : DisposableModelBase, IMethodModel {
        public ImagingImmsMethodModel(IMsdialDataStorage<ParameterBase> storage) {
            var files = storage.AnalysisFiles;
            AnalysisFiles = new ObservableCollection<AnalysisFileBean>(files);
            ImageModels = new ObservableCollection<ImagingImageModel>(files.Select(file => new ImagingImageModel(file)));
            Image = ImageModels.FirstOrDefault();
        }

        public ObservableCollection<ImagingImageModel> ImageModels { get; }

        public ImagingImageModel Image {
            get => _image;
            set => SetProperty(ref _image, value);
        }
        private ImagingImageModel _image;

        public ObservableCollection<AnalysisFileBean> AnalysisFiles { get; }
        public AnalysisFileBean AnalysisFile {
            get => _analysisFile;
            set => SetProperty(ref _analysisFile, value);
        }
        private AnalysisFileBean _analysisFile;
        public ObservableCollection<AlignmentFileBean> AlignmentFiles { get; } = new ObservableCollection<AlignmentFileBean>();
        public AlignmentFileBean AlignmentFile => null;

        public Task LoadAnalysisFileAsync(AnalysisFileBean analysisFile, CancellationToken token) {
            Image = ImageModels.FirstOrDefault(image => image.File == analysisFile);
            return Task.CompletedTask;
        }

        public Task RunAsync(ProcessOption option, CancellationToken token) {
            return Task.CompletedTask;
        }

        public Task SaveAsync() {
            return Task.CompletedTask;
        }
    }
}
