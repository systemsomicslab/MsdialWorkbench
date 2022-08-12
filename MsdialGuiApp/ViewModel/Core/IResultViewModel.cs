using CompMs.CommonMVVM;
using System;
using System.ComponentModel;

namespace CompMs.App.Msdial.ViewModel.Core
{
    internal interface IResultViewModel : IDisposable
    {
        object Model { get; }
        ICollectionView PeakSpotsView { get; }
        // ViewModelBase[] PeakDetailViewModels { get; }
    }
}
