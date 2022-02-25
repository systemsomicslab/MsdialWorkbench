using BCDev.XamlToys;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;

namespace CompMs.Graphics.IO
{
    public class EmfEncoder : IElementEncoder
    {
        public void Save(FrameworkElement element, Stream stream) {
            var drawing = Utility.GetDrawingFromXaml(element);

            using (var graphics = Utility.CreateEmf(stream, drawing.Bounds))
            {
                Utility.RenderDrawingToGraphics(drawing, graphics);
            }
        }

        public object Get(FrameworkElement element) {
            using (var memory = new MemoryStream()) {
                var drawing = Utility.GetDrawingFromXaml(element);

                using (var graphics = Utility.CreateEmf(memory, drawing.Bounds))
                {
                    Utility.RenderDrawingToGraphics(drawing, graphics);
                }

                memory.Seek(0, SeekOrigin.Begin);
                return new Metafile(memory);
            }
        }
    }
}
