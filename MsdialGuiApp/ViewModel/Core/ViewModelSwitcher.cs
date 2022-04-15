using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Core
{
    public class ViewModelSwitcher : ViewModelBase
    {
        public ViewModelSwitcher(
            IObservable<ViewModelBase> whenAnalysis,
            IObservable<ViewModelBase> whenAlignment,
            params IObservable<ViewModelBase>[] viewmodels) {

            SelectedViewModel = new ReactivePropertySlim<ViewModelBase>().AddTo(Disposables);

            ViewModels = viewmodels
                .CombineLatest()
                .StartWith(new ViewModelBase[0])
                .Select(xs => new ReadOnlyCollection<ViewModelBase>(xs))
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            whenAnalysisFileSelecting = Array.IndexOf(viewmodels, whenAnalysis);
            whenAlignmentFileSelecting = Array.IndexOf(viewmodels, whenAlignment);
        }

        public ViewModelSwitcher(
            IObservable<ViewModelBase> whenAnalysis,
            IObservable<ViewModelBase> whenAlignment,
            IReadOnlyList<IObservable<ViewModelBase>> viewmodels)
            : this(whenAnalysis, whenAlignment, viewmodels.ToArray()) {

        }

        public ReactivePropertySlim<ViewModelBase> SelectedViewModel { get; }

        public ReadOnlyReactivePropertySlim<ReadOnlyCollection<ViewModelBase>> ViewModels { get; }

        public void SelectAnalysisFile() {
            if (whenAnalysisFileSelecting >= 0 && whenAnalysisFileSelecting < ViewModels.Value.Count) {
                SelectedViewModel.Value = ViewModels.Value[whenAnalysisFileSelecting];
            }
        }
        private readonly int whenAnalysisFileSelecting;

        public void SelectAlignmentFile() {
            if (whenAlignmentFileSelecting >= 0 && whenAlignmentFileSelecting < ViewModels.Value.Count) {
                SelectedViewModel.Value = ViewModels.Value[whenAlignmentFileSelecting];
            }
        }
        private readonly int whenAlignmentFileSelecting;
    }
}
