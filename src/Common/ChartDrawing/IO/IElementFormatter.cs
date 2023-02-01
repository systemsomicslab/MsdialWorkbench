using System;
using System.Threading.Tasks;

namespace CompMs.Graphics.IO
{
    public interface IElementFormatter
    {
        Task<IDisposable> Format(object element);
    }
}
