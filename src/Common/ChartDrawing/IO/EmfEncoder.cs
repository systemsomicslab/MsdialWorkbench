using BCDev.XamlToys; // This comes from http://xamltoys.codeplex.com/. Unfortunately the repo no longer exists. We gratefully use+modify it here.
using System.Drawing.Imaging;
using System.IO;
using System.Windows;

namespace CompMs.Graphics.IO
{
    public class EmfEncoder : IElementEncoder
    {
        public void Save(FrameworkElement element, Stream stream) {
            SetElementAsImageToStream(element, stream);
        }

        public object Get(FrameworkElement element) {
            using (var memory = new MemoryStream()) {
                SetElementAsImageToStream(element, memory);

                memory.Seek(0, SeekOrigin.Begin);
                return new Metafile(memory);
            }
        }

        private void SetElementAsImageToStream(FrameworkElement element, Stream stream) {
            var drawing = Utility.GetDrawingFromXaml(element);

            using (var graphics = Utility.CreateEmf(stream, drawing.Bounds))
            {
                Utility.RenderDrawingToGraphics(drawing, graphics);
            }
        }
    }
}
