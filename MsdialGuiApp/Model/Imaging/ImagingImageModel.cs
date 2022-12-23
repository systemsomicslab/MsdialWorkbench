using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Information;
using CompMs.CommonMVVM;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Imaging
{
    internal sealed class ImagingImageModel : DisposableModelBase
    {
        private readonly SemaphoreSlim _semaphoreSlim;

        public ImagingImageModel(AnalysisFileBeanModel file) {
            File = file ?? throw new ArgumentNullException(nameof(file));
            var maldiFrames = new MaldiFrames(file.File.GetMaldiFrames());
            ImageResult = new WholeImageResultModel(file, maldiFrames).AddTo(Disposables);

            ImagingRoiModels = new ObservableCollection<ImagingRoiModel>
            {
                ImageResult.ImagingRoiModel
            };
            RoiEditModel = new RoiEditModel(file, maldiFrames);

            PeakInformationModel = new PeakInformationAnalysisModel(ImageResult.Target).AddTo(Disposables);
            PeakInformationModel.Add(
                t => new MzPoint(t?.Mass ?? 0d),
                t => new DriftPoint(t?.InnerModel.ChromXs.Drift.Value ?? 0d),
                t => new CcsPoint(t?.InnerModel.CollisionCrossSection ?? 0d));
            PeakInformationModel.Add(
                t => new HeightAmount(t?.Intensity ?? 0d),
                t => new AreaAmount(t?.PeakArea ?? 0d));
            var moleculeStructureModel = new MoleculeStructureModel().AddTo(Disposables);
            MoleculeStructureModel = moleculeStructureModel;
            ImageResult.Target.Subscribe(t => moleculeStructureModel.UpdateMolecule(t?.InnerModel)).AddTo(Disposables);
            _semaphoreSlim = new SemaphoreSlim(1, 1).AddTo(Disposables);
        }

        public WholeImageResultModel ImageResult { get; }
        public ObservableCollection<ImagingRoiModel> ImagingRoiModels { get; }
        public RoiEditModel RoiEditModel { get; }
        public AnalysisFileBeanModel File { get; }
        public PeakInformationAnalysisModel PeakInformationModel { get; }
        public MoleculeStructureModel MoleculeStructureModel { get; }

        public async Task AddRoiAsync() {
            await _semaphoreSlim.WaitAsync();
            try {
                var roi = RoiEditModel.CreateRoi(Colors.Purple);
                if (roi is null) {
                    return;
                }
                var imagingRoi = await ImageResult.CreateImagingRoiModelAsync(roi);
                if (imagingRoi is null) {
                    return;
                }
                ImagingRoiModels.Add(imagingRoi.AddTo(Disposables));
            }
            finally {
                _semaphoreSlim.Release();
            }
        }
    }
}
