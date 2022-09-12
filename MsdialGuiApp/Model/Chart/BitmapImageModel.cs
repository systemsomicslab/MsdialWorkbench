using CompMs.CommonMVVM;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CompMs.App.Msdial.Model.Chart
{
    internal sealed class BitmapImageModel : BindableBase
    {
        public BitmapImageModel(byte[] image, int width, int height, PixelFormat pf, BitmapPalette palette, string title) {
            var bs = BitmapSource.Create(width, height, 96, 96, pf, palette, image, image.Length / height);
            bs.Freeze();
            BitmapSource = bs;
            Title = title;
        }

        public string Title { get; }
        public BitmapSource BitmapSource { get; }
    }
}
