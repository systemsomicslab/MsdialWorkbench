using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CompMs.Graphics.Command
{
    public class SavePngCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;
        public FrameworkElement Element { get; set; }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            if (Element == null) return;

            var bmp = new RenderTargetBitmap(
                (int)(Element.ActualWidth * 300d / 96d), (int)(Element.ActualHeight * 300d / 96d),
                300d, 300d, PixelFormats.Pbgra32
                );
            bmp.Render(Element);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bmp));
            var dialog = new SaveFileDialog();
            dialog.Filter = "Image file|*.png";
            if (dialog.ShowDialog() == true)
            //if (dialog.ShowDialog().Hasvlue)
            {
                using (FileStream stream = File.Create(dialog.FileName))
                {
                    encoder.Save(stream);
                }
            }
        }
    }
}
