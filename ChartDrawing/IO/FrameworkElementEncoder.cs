using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CompMs.Graphics.IO
{
    public class FrameworkElementEncoder
    {
        public void SaveAsPng(FrameworkElement element, Stream stream) {
            var encoder = new PngBitmapEncoder();
            SaveUsingEncoder(element, stream, encoder);
        }

        private void SaveUsingEncoder(FrameworkElement element, Stream stream, BitmapEncoder encoder) {
            var bitmap = new RenderTargetBitmap((int)element.ActualWidth, (int)element.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(element);
            var frame = BitmapFrame.Create(bitmap);
            encoder.Frames.Add(frame);

            encoder.Save(stream);
        }
    }
}
