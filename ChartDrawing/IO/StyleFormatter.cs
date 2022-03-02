using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace CompMs.Graphics.IO
{
    public class StyleFormatter : DependencyObject, IElementFormatter {
        public Style Style { get; set; }

        public async Task<IDisposable> Format(object element) {
            if (element is Control control) {
                var restorer = new ControlStyleRestore(control);
                await Dispatcher.InvokeAsync(() =>
                {
                    control.Style = Style;
                });
                return restorer;
            }
            return null;
        }

        class ControlStyleRestore : IDisposable
        {
            private readonly Control control;
            private readonly Style style;

            public ControlStyleRestore(Control control) {
                this.control = control;
                style = control.Style;
            }

            public void Dispose() {
                control.Style = style;
            }
        }
    }
}
