using CompMs.CommonMVVM;
using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CompMs.App.Msdial.Model.Chart
{
    internal sealed class BitmapImageModel : BindableBase
    {
        public static readonly int ImageMargin = 1;

        private Func<BitmapSource>? _bitmapSourceFactory;
        private BitmapSource? _bitmapSource;

        public BitmapImageModel(BitmapSource bitmapSource, string title) {
            _bitmapSource = bitmapSource;
            Title = title;
        }

        public BitmapImageModel(Func<BitmapSource> bitmapSourceFactory, string title) {
            _bitmapSourceFactory = bitmapSourceFactory;
            Title = title;
        }

        public string Title { get; }
        public BitmapSource BitmapSource => _bitmapSource ?? _bitmapSourceFactory!.Invoke();

        public BitmapImageModel WithPalette(BitmapPalette palette) {
            if (_bitmapSource is null) {
                var factory = () => {
                    var bs = _bitmapSourceFactory!.Invoke();
                    var newbs = new FormatConvertedBitmap(bs, bs.Format, palette, 0d);
                    newbs.Freeze();
                    return newbs;
                };
                return new BitmapImageModel(factory, Title);
            }
            var bs = new FormatConvertedBitmap(_bitmapSource, _bitmapSource.Format, palette, 0d);
            bs.Freeze();
            return new BitmapImageModel(bs, Title);
        }

        public static BitmapImageModel Create(byte[] image, int width, int height, PixelFormat pf, BitmapPalette palette, string title) {
            var bs = BitmapSource.Create(width, height, 96, 96, pf, palette, image, image.Length / height);
            bs.Freeze();
            return new BitmapImageModel(bs, title);
        }

        public static BitmapImageModel Create(Func<byte[]> factory, int width, int height, PixelFormat pf, BitmapPalette palette, string title) {
            var f = () => {
                var image = factory.Invoke();
                var bs = BitmapSource.Create(width, height, 96, 96, pf, palette, image, image.Length / height);
                bs.Freeze();
                return bs;
            };
            return new BitmapImageModel(f, title);
        }

        public static int WithMarginToLength(int length) {
            return length + ImageMargin * 2;
        }

        public static int WithMarginToPoint(int point) {
            return point - ImageMargin;
        }
    }
}
