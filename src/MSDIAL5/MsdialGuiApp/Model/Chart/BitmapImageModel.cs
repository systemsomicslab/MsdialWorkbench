using CompMs.CommonMVVM;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CompMs.App.Msdial.Model.Chart
{
    internal sealed class BitmapImageModel : BindableBase
    {
        public static readonly int ImageMargin = 1;

        public BitmapImageModel(BitmapSource bitmapSource, string title) {
            BitmapSource = bitmapSource;
            Title = title;
        }

        public string Title { get; }
        public BitmapSource BitmapSource { get; }

        public BitmapImageModel WithPalette(BitmapPalette palette) {
            var bs = new FormatConvertedBitmap(BitmapSource, BitmapSource.Format, palette, 0d);
            bs.Freeze();
            return new BitmapImageModel(bs, Title);
        }

        public static BitmapImageModel Create(byte[] image, int width, int height, PixelFormat pf, BitmapPalette palette, string title) {
            var bs = BitmapSource.Create(width, height, 96, 96, pf, palette, image, image.Length / height);
            bs.Freeze();
            return new BitmapImageModel(bs, title);
        }

        public static int WithMarginToLength(int length) {
            return length + ImageMargin * 2;
        }

        public static int WithMarginToPoint(int point) {
            return point - ImageMargin;
        }
    }
}
