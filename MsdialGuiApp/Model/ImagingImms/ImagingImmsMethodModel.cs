using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Imaging;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialImmsCore.Parameter;
using CompMs.MsdialImmsCore.Process;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.ImagingImms
{
    internal sealed class ImagingImmsMethodModel : DisposableModelBase, IMethodModel
    {
        private readonly IMsdialDataStorage<MsdialImmsParameter> _storage;
        private readonly FacadeMatchResultEvaluator _evaluator;
        private readonly IDataProviderFactory<AnalysisFileBeanModel> _providerFactory;

        public ImagingImmsMethodModel(AnalysisFileBeanModelCollection analysisFileBeanModelCollection, IMsdialDataStorage<MsdialImmsParameter> storage) {
            AnalysisFileModelCollection = analysisFileBeanModelCollection;
            _storage = storage;
            _evaluator = FacadeMatchResultEvaluator.FromDataBases(storage.DataBases);
            _providerFactory = storage.Parameter.ProviderFactoryParameter.Create().ContraMap((AnalysisFileBeanModel file) => file.File.LoadRawMeasurement(true, true, 5, 5000));
            // _providerFactory = new StandardDataProviderFactory().ContraMap((AnalysisFileBeanModel file) => file.File.LoadRawMeasurement(true, true, 5, 5000));
            ImageModels = new ObservableCollection<ImagingImageModel>();
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

        public async Task RunAsync(ProcessOption option, CancellationToken token) {
            if (option.HasFlag(ProcessOption.Identification | ProcessOption.PeakSpotting)) {
                var files = AnalysisFileModelCollection.IncludedAnalysisFiles;
                var processor = new FileProcess(_storage, null, null, _evaluator);
                await processor.RunAllAsync(files.Select(file => file.File), files.Select(_providerFactory.Create), Enumerable.Repeat<Action<int>>(null, files.Count), 2, null).ConfigureAwait(false);
                foreach (var file in files) {
                    ImageModels.Add(new ImagingImageModel(file));
                }
            }
            else if (option.HasFlag(ProcessOption.Identification)) {
                var files = AnalysisFileModelCollection.IncludedAnalysisFiles;
                var processor = new FileProcess(_storage, null, null, _evaluator);
                await processor.AnnotateAllAsync(files.Select(file => file.File), files.Select(_providerFactory.Create), Enumerable.Repeat<Action<int>>(null, files.Count), 2, null).ConfigureAwait(false);
                foreach (var file in files) {
                    ImageModels.Add(new ImagingImageModel(file));
                }
            }
        }

        public Task SaveAsync() {
            return Task.CompletedTask;
        }
    }
}
