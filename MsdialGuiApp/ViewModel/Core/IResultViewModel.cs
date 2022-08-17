using CompMs.App.Msdial.ViewModel.Search;
using CompMs.CommonMVVM;
using System;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Core
{
    internal interface IResultViewModel : IDisposable
    {
        // FocusNavigatorViewModel FocusNavigatorViewModel { get; }
        ViewModelBase[] PeakDetailViewModels { get; }

        ICommand ShowIonTableCommand { get; }
    }
}
