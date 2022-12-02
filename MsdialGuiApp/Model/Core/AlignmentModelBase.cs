using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using System.Windows;

namespace CompMs.App.Msdial.Model.Core
{
    public abstract class AlignmentModelBase : BindableBase, IAlignmentModel, IDisposable
    {
        public AlignmentModelBase(AlignmentFileBean file, string resultFile) {
            alignmentResultFile = resultFile;
            //Container = AlignmentResultContainer.Load(file);
            Container = AlignmentResultContainer.LoadLazy(file);
            if (Container == null) {
                MessageBox.Show("No aligned spot information."); // TODO: Move to view.
                Container = new AlignmentResultContainer
                {
                    AlignmentSpotProperties = new ObservableCollection<AlignmentSpotProperty>(),
                };
            }
        }

        public string AlignmentResultFile {
            get => alignmentResultFile;
            set => SetProperty(ref alignmentResultFile, value);
        }
        private string alignmentResultFile;

        public AlignmentResultContainer Container {
            get => container;
            private set => SetProperty(ref container, value);
        }
        private AlignmentResultContainer container;

        public virtual Task SaveAsync() {
            // return Task.Run(() => MessagePackHandler.SaveToFile(Container, AlignmentResultFile));
            return Task.Run(() => Container.Save(AlignmentResultFile));
        }

        public string DisplayLabel {
            get => displayLabel;
            set => SetProperty(ref displayLabel, value);
        }
        private string displayLabel = string.Empty;

        public abstract void SearchFragment();
        public abstract void InvokeMsfinder();

        protected readonly CompositeDisposable Disposables = new CompositeDisposable();

        private bool disposedValue;

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    // TODO: dispose managed state (managed objects)
                    Disposables.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
