using System.IO;
using System.Windows;

namespace CompMs.Graphics.IO
{
    public interface IElementEncoder
    {
        void Save(FrameworkElement element, Stream stream);

        object Get(FrameworkElement element);
    }
}
