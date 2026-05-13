using CompMs.App.Msdial.Common;
using CompMs.App.Msdial.Model.Chart;
using CompMs.Common.DataStructure;
using CompMs.CommonMVVM;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Imaging;

internal sealed class IntensityImagePlaceholderModel : BindableBase
{
    private readonly ConcurrentLruCache<int, BitmapImageModel> _cache;
    private readonly MaldiFrames _frameInfos;
    private readonly RawIntensityOnPixelsLoader _intensitiesLoader;

    public IntensityImagePlaceholderModel(MaldiFrames frameInfos, RawIntensityOnPixelsLoader intensitiesLoader) {
        _cache = new ConcurrentLruCache<int, BitmapImageModel>(32);
        _frameInfos = frameInfos;
        _intensitiesLoader = intensitiesLoader;
    }

    public BitmapImageModel? CurrentImage {
        get => _currentImage;
        private set => SetProperty(ref _currentImage, value);
    }
    private BitmapImageModel? _currentImage = null;

    public void ResetImage() {
        CurrentImage = null;
    }

    public async Task EnsureImageAsync(int index, string title, CancellationToken token = default) {
        if (_cache.TryGet(index, out var imageModel)) {
            CurrentImage = imageModel;
            return;
        }

        var xmin = BitmapImageModel.WithMarginToPoint(_frameInfos.XIndexPosMin);
        var ymin = BitmapImageModel.WithMarginToPoint(_frameInfos.YIndexPosMin);
        var width = BitmapImageModel.WithMarginToLength(_frameInfos.XIndexWidth);
        var height = BitmapImageModel.WithMarginToLength(_frameInfos.YIndexHeight);
        var pf = PixelFormats.Indexed8;
        var stride = (pf.BitsPerPixel + 7) / 8;

        token.ThrowIfCancellationRequested();

        var image = new byte[width * stride * height];
        var pixels = await _intensitiesLoader.LoadAsync(index, token).ConfigureAwait(false);

        token.ThrowIfCancellationRequested();

        var features = pixels.PixelPeakFeaturesList[0];
        var sorted = features.IntensityArray.OrderBy(x => x).ToArray();
        var length = sorted.Length;
        var zmin = sorted.DefaultIfEmpty(0d).First();
        var zmax = sorted.DefaultIfEmpty(255).ElementAt((int)(length * .99));
        if (zmin == zmax) {
            zmax = zmin + 1;
        }
        foreach (var (intensity, info) in features.IntensityArray.Zip(_frameInfos.Infos, (x, y) => (x, y))) {
            image[width * stride * (info.YIndexPos - ymin) + (info.XIndexPos - xmin) * stride] = (byte)Math.Max(1, Math.Min((intensity - zmin) / (zmax - zmin), 1d) * 255);
        }

        token.ThrowIfCancellationRequested();

        imageModel = BitmapImageModel.Create(image, width, height, pf, Colormaps.Viridis, title);
        _cache.Put(index, imageModel);
        CurrentImage = imageModel;
    }
}
