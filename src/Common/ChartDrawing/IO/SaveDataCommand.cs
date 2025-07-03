using Microsoft.Win32;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CompMs.Graphics.IO
{
    public class SaveDataCommand(Func<Stream, Task> saveFunc) : ICommand
    {
        private readonly Func<Stream, Task> _saveFunc = saveFunc;

        public string Filter { get; set; } = string.Empty;

#pragma warning disable 67
        public event EventHandler CanExecuteChanged;
#pragma warning restore 67

        public bool CanExecute(object parameter) {
            return !string.IsNullOrEmpty(Filter);
        }

        public async void Execute(object parameter) {
            if (TryGetSaveFilePath(out var path)) {
                using var fs = File.Open(path, FileMode.Create);
                await _saveFunc.Invoke(fs).ConfigureAwait(false);
            }
        }

        private bool TryGetSaveFilePath(out string path) {
            var sfd = new SaveFileDialog
            {
                Title = "Save file dialog.",
                Filter = Filter,
                RestoreDirectory = true,
            };

            var result = sfd.ShowDialog() ?? false;
            path = sfd.FileName ?? string.Empty;
            return result;
        } 
    }
}
