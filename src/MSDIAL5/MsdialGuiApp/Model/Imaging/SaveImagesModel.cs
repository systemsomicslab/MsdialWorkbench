using CompMs.CommonMVVM;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace CompMs.App.Msdial.Model.Imaging
{
    internal sealed class SaveImagesModel : BindableBase
    {
        private readonly IWholeImageResultModel _imageResult;
        private readonly IReadOnlyList<ImagingRoiModel> _roiModels;

        public SaveImagesModel(IWholeImageResultModel imageResult, IReadOnlyList<ImagingRoiModel> roiModels)
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

        public Task SaveAsync(CancellationToken token = default)
        {
            string? filePath = Path;
            if (string.IsNullOrEmpty(filePath)) {
                var dialog = new SaveFileDialog
                {
                    Filter = "PNG Image|*.png|GIF Image|*.gif",
                    DefaultExt = "png",
                    FileName = "image.png",
                };
                if (dialog.ShowDialog() == true) {
                    filePath = dialog.FileName;
                }
            }
            if (string.IsNullOrEmpty(filePath)) {
                return Task.CompletedTask;
            }

            var image = _imageResult.IntensityImagePlaceholder.CurrentImage;
            if (image is null) {
                return Task.CompletedTask;
            }
            var rois = _roiModels.Where(roi => roi.IsSelected).Select(roi => roi.Roi.RoiImage).ToArray();

            return Task.Run(async () =>
            {
                BitmapEncoder? encoder = null;
                if (filePath.EndsWith("png"))
                {
                    encoder = new PngBitmapEncoder();
                }
                else if (filePath.EndsWith("gif"))
                {
                    encoder = new GifBitmapEncoder();
                }
                else
                {
                    return;
                }

                await Task.WhenAll([image.EnsureBitmapSourceAsync(), .. rois.Select(roi => roi.EnsureBitmapSourceAsync())]).ConfigureAwait(false);

                encoder.Frames.Add(BitmapFrame.Create(image.BitmapSource));
                foreach (var roi in rois)
                {
                    encoder.Frames.Add(BitmapFrame.Create(roi.BitmapSource));
                }
                using (var stream = File.Open(filePath, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    encoder.Save(stream);
                }
            }, token);
        }
    }
}
