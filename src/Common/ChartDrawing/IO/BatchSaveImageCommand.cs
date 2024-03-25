using CompMs.Graphics.Window;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace CompMs.Graphics.IO;

public sealed class BatchSaveImageCommand : ICommand
{
    public static BatchSaveImageCommand Instance { get; } = new BatchSaveImageCommand();

    public bool CanExecute(object parameter) {
        return true;
        if (parameter is IReadOnlyList<object> parameters) {
            for (int i = 0; i < parameters.Count; i+=2) {
                if (i + 1 < parameters.Count && (parameters[i] is not FrameworkElement || parameters[i + 1] is not string)) {
                    return false;
                }
            }
            return true;
        }
        return false;
    }

#pragma warning disable 67
    public event EventHandler CanExecuteChanged;
#pragma warning restore 67

    public async void Execute(object parameter) {
        var sfd = new SelectFolderDialog
        {
            Title = "Select folder to save images",
        };
        if (sfd.ShowDialog() != DialogResult.OK) {
            return;
        }
        var folderPath = sfd.SelectedPath;
        var encoder = new PngEncoder();
        var formatter = new NoneFormatter();
        if (parameter is IReadOnlyList<object> parameters) {
            for (int i = 0; i < parameters.Count; i+=2) {
                if (i + 1 >= parameters.Count) {
                    continue;
                }
                if (parameters[i] is FrameworkElement element && parameters[i + 1] is string name) {
                    using (await formatter.Format(parameters[i]))
                    using (var fs = File.Open(Path.Combine(folderPath, name + ".png"), FileMode.Create)) {
                        encoder.Save(element, fs);
                    }
                }
            }
        }
    }
}
