using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CompMs.Graphics.IO
{
    public class PngEncoder : IElementEncoder {
        public double HorizontalDpi { get; }
        public double VerticalDpi { get; }
        public PngEncoder() {
            HorizontalDpi = 96; VerticalDpi = 96;
        }
        public PngEncoder(double horizontalDpi, double verticalDpi) {
            HorizontalDpi = horizontalDpi;
            VerticalDpi = verticalDpi;
        }

        public void Save(FrameworkElement element, Stream stream) {
            var encoder = new PngBitmapEncoder();
            SaveUsingEncoder(element, stream, encoder);
        }

        public object Get(FrameworkElement element){
            var encoder = new PngBitmapEncoder();
            using (var memory = new MemoryStream()) {
                SaveUsingEncoder(element, memory, encoder);
                return new Bitmap(memory);
            }
        }

        private void SaveUsingEncoder(FrameworkElement element, Stream stream, BitmapEncoder encoder) {
            var bitmap = new RenderTargetBitmap((int)element.ActualWidth, (int)element.ActualHeight, HorizontalDpi, VerticalDpi, PixelFormats.Pbgra32);
            bitmap.Render(element);
            var frame = BitmapFrame.Create(bitmap);
            encoder.Frames.Add(frame);

            encoder.Save(stream);
        }
    }
}
