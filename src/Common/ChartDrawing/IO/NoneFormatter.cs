using System;
using System.Threading.Tasks;

namespace CompMs.Graphics.IO
{
    public class NoneFormatter : IElementFormatter
    {
        public Task<IDisposable> Format(object element) {
            return Task.FromResult((IDisposable)null);
        }
    }
}
