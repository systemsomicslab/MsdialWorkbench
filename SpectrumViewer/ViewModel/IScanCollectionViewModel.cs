using CompMs.App.SpectrumViewer.Model;
using CompMs.Common.Interfaces;
using Reactive.Bindings;
using System;

namespace CompMs.App.SpectrumViewer.ViewModel
{
    public interface IScanCollectionViewModel
    {
        IScanCollection Model { get; }

        IObservable<IMSScanProperty> ScanSource { get; }

        ReactiveCommand CloseCommand { get; }
    }
}
