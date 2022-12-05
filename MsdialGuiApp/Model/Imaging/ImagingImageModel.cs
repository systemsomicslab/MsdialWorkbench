using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Information;
using CompMs.CommonMVVM;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;

namespace CompMs.App.Msdial.Model.Imaging
{
    internal sealed class ImagingImageModel : DisposableModelBase
    {
        public ImagingImageModel(AnalysisFileBeanModel file) {
            File = file ?? throw new ArgumentNullException(nameof(file));
            ImageResult = new WholeImageResultModel(file).AddTo(Disposables);

            ImagingRoiModels = new ObservableCollection<ImagingRoiModel>
            {
                ImageResult.ImagingRoiModel
            };

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
        }

        public WholeImageResultModel ImageResult { get; }
        public ObservableCollection<ImagingRoiModel> ImagingRoiModels { get; }
        public AnalysisFileBeanModel File { get; }
        public PeakInformationAnalysisModel PeakInformationModel { get; }
        public MoleculeStructureModel MoleculeStructureModel { get; }
    }
}
