using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
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
        public ImagingImmsMethodModel(AnalysisFileBeanModelCollection analysisFileBeanModelCollection, IMsdialDataStorage<ParameterBase> storage) {
            AnalysisFileModelCollection = analysisFileBeanModelCollection;
            ImageModels = new ObservableCollection<ImagingImageModel>(AnalysisFileModelCollection.AnalysisFiles.Select(file => new ImagingImageModel(file)));
            Image = ImageModels.FirstOrDefault();
        }

        public ObservableCollection<ImagingImageModel> ImageModels { get; }

        public ImagingImageModel Image {
            get => _image;
            set => SetProperty(ref _image, value);
        }
        private ImagingImageModel _image;

        public AnalysisFileBeanModelCollection AnalysisFileModelCollection { get; }

        public AnalysisFileBeanModel AnalysisFileModel {
            get => _analysisFileModel;
            set => SetProperty(ref _analysisFileModel, value);
        }
        private AnalysisFileBeanModel _analysisFileModel;

        public ObservableCollection<AlignmentFileBean> AlignmentFiles { get; } = new ObservableCollection<AlignmentFileBean>();
        public AlignmentFileBean AlignmentFile => null;

        public Task LoadAnalysisFileAsync(AnalysisFileBeanModel analysisFile, CancellationToken token) {
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
