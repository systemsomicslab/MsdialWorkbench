using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CompMs.Graphics.IO
{
    public class PngEncoder : IElementEncoder {
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
            var bitmap = new RenderTargetBitmap((int)element.ActualWidth, (int)element.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(element);
            var frame = BitmapFrame.Create(bitmap);
            encoder.Frames.Add(frame);

            encoder.Save(stream);
        }
    }
}
