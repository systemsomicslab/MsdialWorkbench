using CompMs.App.SpectrumViewer.Model;
using CompMs.App.SpectrumViewer.ViewModel;
using Microsoft.Win32;
using Reactive.Bindings.Notifiers;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;

namespace CompMs.App.SpectrumViewer.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow() {
            InitializeComponent();

            broker = new MessageBroker();
            DataContext = new MainViewModel(broker);

            CommandBindings.Add(new CommandBinding(ApplicationCommands.Open, FileSelectAction));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Close, (s, e) => Close()));

            var drop = Observable.FromEvent<DragEventHandler, DragEventArgs>(
                h => (s, e) => h(e),
                h => Drop += h,
                h => Drop -= h);
            drop.Select(e => e.Data.GetData(DataFormats.FileDrop))
                .OfType<string[]>()
                .SelectMany(files => files.ToObservable())
                .Where(file => file != null)
                .Subscribe(file => broker.Publish(new FileOpenRequest(file)));

            if (Environment.GetCommandLineArgs().Length >= 2) {
                foreach (var file in Environment.GetCommandLineArgs().Skip(1)) {
                    broker.Publish(new FileOpenRequest(file));
                }
            }
        }

        private readonly MessageBroker broker;

        private void FileSelectAction(object sender, RoutedEventArgs e) {
            var ofd = new OpenFileDialog
            {
                Title = "Please select spectrum file",
                RestoreDirectory = true,
            };
            if (ofd.ShowDialog() != true) {
                return;
            }

            broker.Publish(new FileOpenRequest(ofd.FileName));
        }
    }
}
