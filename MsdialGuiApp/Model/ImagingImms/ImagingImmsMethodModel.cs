using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Imaging;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialImmsCore.Parameter;
using CompMs.MsdialImmsCore.Process;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.ImagingImms
{
    internal sealed class ImagingImmsMethodModel : MethodModelBase, IMethodModel
    {
        private readonly IMsdialDataStorage<MsdialImmsParameter> _storage;
        private readonly FacadeMatchResultEvaluator _evaluator;
        private readonly IDataProviderFactory<AnalysisFileBeanModel> _providerFactory;

        public ImagingImmsMethodModel(AnalysisFileBeanModelCollection analysisFileBeanModelCollection, IMsdialDataStorage<MsdialImmsParameter> storage, ProjectBaseParameterModel projectBaseParameter)
            : base(analysisFileBeanModelCollection, Enumerable.Empty<AlignmentFileBean>(), projectBaseParameter) {
            _storage = storage;
            _evaluator = FacadeMatchResultEvaluator.FromDataBases(storage.DataBases);
            _providerFactory = storage.Parameter.ProviderFactoryParameter.Create().ContraMap((AnalysisFileBeanModel file) => file.File.LoadRawMeasurement(true, true, 5, 5000));
            ImageModels = new ObservableCollection<ImagingImageModel>();
            Image = ImageModels.FirstOrDefault();
        }

        public ObservableCollection<ImagingImageModel> ImageModels { get; }

        public ImagingImageModel Image {
            get => _image;
            set => SetProperty(ref _image, value);
        }
        private ImagingImageModel _image;

        public override async Task RunAsync(ProcessOption option, CancellationToken token) {
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

        public override Task LoadAsync(CancellationToken token) {
            foreach (var file in AnalysisFileModelCollection.AnalysisFiles) {
                ImageModels.Add(new ImagingImageModel(file));
            }
            var analysisFile = AnalysisFileModelCollection.IncludedAnalysisFiles.FirstOrDefault();
            if (!(analysisFile is null)) {
                Image = ImageModels.FirstOrDefault(image => image.File == analysisFile);
            }
            return Task.CompletedTask;
        }

        protected override IAnalysisModel LoadAnalysisFileCore(AnalysisFileBeanModel analysisFile) {
            Image = ImageModels.FirstOrDefault(image => image.File == analysisFile);
            return null;
        }

        protected override IAlignmentModel LoadAlignmentFileCore(AlignmentFileBean alignmentFile) {
            return null;
        }
    }
}
