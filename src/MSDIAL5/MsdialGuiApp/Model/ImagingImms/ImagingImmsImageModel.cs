using CompMs.App.Msdial.Common;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Imaging;
using CompMs.App.Msdial.Model.Information;
using CompMs.CommonMVVM;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.ImagingImms
{
    internal sealed class ImagingImmsImageModel : DisposableModelBase
    {
        private readonly SemaphoreSlim _semaphoreSlim;
        private int _roiId = 0;

        public ImagingImmsImageModel(AnalysisFileBeanModel file)
        {
            File = file ?? throw new ArgumentNullException(nameof(file));
            var maldiFrames = new MaldiFrames(file.File.GetMaldiFrames());
            var wholeRoi = new RoiModel(file, _roiId, maldiFrames, ChartBrushes.GetChartBrush(_roiId).Color);
            ++_roiId;
            ImageResult = new WholeImageResultModel(file, maldiFrames, wholeRoi).AddTo(Disposables);

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
            SaveImagesModel = new SaveImagesModel(ImageResult, ImagingRoiModels);
            _semaphoreSlim = new SemaphoreSlim(1, 1).AddTo(Disposables);
        }

        public WholeImageResultModel ImageResult { get; }
        public ObservableCollection<ImagingRoiModel> ImagingRoiModels { get; }
        public RoiEditModel RoiEditModel { get; }
        public SaveImagesModel SaveImagesModel { get; }
        public AnalysisFileBeanModel File { get; }
        public PeakInformationAnalysisModel PeakInformationModel { get; }
        public MoleculeStructureModel MoleculeStructureModel { get; }

        public async Task AddRoiAsync()
        {
            await _semaphoreSlim.WaitAsync();
            try
            {
                var roi = RoiEditModel.CreateRoi(_roiId, ChartBrushes.GetChartBrush(_roiId).Color);
                ++_roiId;
                if (roi is null)
                {
                    return;
                }
                var imagingRoi = await ImageResult.CreateImagingRoiModelAsync(roi);
                if (imagingRoi is null)
                {
                    return;
                }
                ImagingRoiModels.Add(imagingRoi.AddTo(Disposables));
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }
    }
}
