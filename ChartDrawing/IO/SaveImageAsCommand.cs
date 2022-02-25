using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace CompMs.Graphics.IO
{
    public class SaveImageAsCommand : ICommand
    {
        public static SaveImageAsCommand PngInstance { get; } = new SaveImageAsCommand(ImageFormat.Png);

        public static SaveImageAsCommand EmfInstance { get; } = new SaveImageAsCommand(ImageFormat.Emf);

        public SaveImageAsCommand() : this(ImageFormat.Png) {

        }

        private SaveImageAsCommand(ImageFormat format) {
            switch (format) {
                case ImageFormat.Png:
                    encoder = new PngEncoder();
                    filter = "PNG image(*.png)|.png";
                    break;
                case ImageFormat.Emf:
                    encoder = new EmfEncoder();
                    filter = "Extended Metafile Format(*.emf)|.emf";
                    break;
            }
        }

        private readonly IElementEncoder encoder;
        private readonly string filter;

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) {
            return true;
        }

        public void Execute(object parameter) {
            if (parameter is FrameworkElement element) {
                var sfd = new SaveFileDialog
                {
                    Title = "Save image dialog.",
                    Filter = filter,
                    RestoreDirectory = true,
                };
                if (sfd.ShowDialog() == true) {

                    using (var fs = File.Open(sfd.FileName, FileMode.Create)) {
                        encoder.Save(element, fs);
                    }
                }
            }
        }
    }
}
