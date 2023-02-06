using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CompMs.Graphics.IO
{
    public class PngEncoder : IElementEncoder {
        public double HorizontalDpi { get; }
        public double VerticalDpi { get; }
        public PngEncoder() : this(96d, 96d) {

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
            var height = element.ActualHeight;
            var width = element.ActualWidth;
            if (HorizontalDpi < VerticalDpi) {
                height *= VerticalDpi / HorizontalDpi;
            }
            else {
                width *= HorizontalDpi / VerticalDpi;
            }
            var bitmap = new RenderTargetBitmap((int)Math.Ceiling(width), (int)Math.Ceiling(height), HorizontalDpi, VerticalDpi, PixelFormats.Pbgra32);
            bitmap.Render(element);
            var frame = BitmapFrame.Create(bitmap);
            encoder.Frames.Add(frame);

            encoder.Save(stream);
        }
    }
}
