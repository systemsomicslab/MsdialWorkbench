using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace CompMs.Graphics.IO
{
    public class CopyAsStringCommand(Func<Stream, Task> saveFunc) : ICommand
    {
        private readonly Func<Stream, Task> _saveFunc = saveFunc;

        public string Format { get; set; } = DataFormats.Text;

#pragma warning disable 67
        public event EventHandler CanExecuteChanged;
#pragma warning restore 67

        public bool CanExecute(object parameter) => true;

        public async void Execute(object parameter) {
            using var memory = new MemoryStream();
            await _saveFunc.Invoke(memory).ConfigureAwait(false);
            await memory.FlushAsync();
            Application.Current?.Dispatcher.Invoke(
                () => Clipboard.SetData(Format, Encoding.UTF8.GetString(memory.ToArray()))
            );
        }
    }
}
