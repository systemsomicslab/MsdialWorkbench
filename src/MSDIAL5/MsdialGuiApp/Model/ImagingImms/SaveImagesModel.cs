using CompMs.App.Msdial.Model.Imaging;
using CompMs.CommonMVVM;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace CompMs.App.Msdial.Model.ImagingImms
{
    internal sealed class SaveImagesModel : BindableBase
    {
        private readonly WholeImageResultModel _imageResult;
        private readonly IReadOnlyList<ImagingRoiModel> _roiModels;

        public SaveImagesModel(WholeImageResultModel imageResult, IReadOnlyList<ImagingRoiModel> roiModels)
        {
            _imageResult = imageResult ?? throw new ArgumentNullException(nameof(imageResult));
            _roiModels = roiModels ?? throw new ArgumentNullException(nameof(roiModels));
        }

        public string Path
        {
            get => _path;
            set => SetProperty(ref _path, value);
        }
        private string _path = string.Empty;

        public Task SaveAsync()
        {
            var image = _imageResult.SelectedPeakIntensities?.BitmapImageModel;
            if (image is null) {
                return Task.CompletedTask;
            }
            var rois = _roiModels.Where(roi => roi.IsSelected).Select(roi => roi.Roi.RoiImage).ToArray();

            return Task.Run(() =>
            {
                if (string.IsNullOrEmpty(Path)) {
                    return;
                }
                BitmapEncoder? encoder = null;
                if (Path.EndsWith("png"))
                {
                    encoder = new PngBitmapEncoder();
                }
                else if (Path.EndsWith("gif"))
                {
                    encoder = new GifBitmapEncoder();
                }
                else
                {
                    return;
                }
                encoder.Frames.Add(BitmapFrame.Create(image.BitmapSource));
                foreach (var roi in rois)
                {
                    encoder.Frames.Add(BitmapFrame.Create(roi.BitmapSource));
                }
                using (var stream = File.Open(Path, FileMode.Create))
                {
                    encoder.Save(stream);
                }
            });
        }
    }
}
