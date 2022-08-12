using CompMs.CommonMVVM;
using System;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Core
{
    internal interface IResultViewModel : IDisposable
    {
        ViewModelBase[] PeakDetailViewModels { get; }

        ICommand ShowIonTableCommand { get; }
    }
}
