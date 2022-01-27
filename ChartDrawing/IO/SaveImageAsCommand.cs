using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace CompMs.Graphics.IO
{
    public class SaveImageAsCommand : ICommand
    {
        public static SaveImageAsCommand PngInstance { get; } = new SaveImageAsCommand();

        public SaveImageAsCommand() {
            encoder = new FrameworkElementEncoder();
        }

        private readonly FrameworkElementEncoder encoder;

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) {
            return true;
        }

        public void Execute(object parameter) {
            if (parameter is FrameworkElement element) {
                var sfd = new SaveFileDialog
                {
                    Title = "Save image dialog.",
                    Filter = "PNG image(*.png)|*.png",
                    RestoreDirectory = true,
                };
                if (sfd.ShowDialog() == true) {

                    using (var fs = File.Open(sfd.FileName, FileMode.Create)) {
                        encoder.SaveAsPng(element, fs);
                    }
                }
            }
        }
    }
}
