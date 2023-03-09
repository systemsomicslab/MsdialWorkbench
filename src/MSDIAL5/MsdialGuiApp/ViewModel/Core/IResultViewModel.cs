using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.CommonMVVM;
using System;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Core
{
    internal interface IResultViewModel : IDisposable
    {
        IResultModel Model { get; }

        // FocusNavigatorViewModel FocusNavigatorViewModel { get; }
        ViewModelBase[] PeakDetailViewModels { get; }

        ICommand ShowIonTableCommand { get; }
        ICommand SetUnknownCommand { get; }
        UndoManagerViewModel UndoManagerViewModel { get; }
    }
}
