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
            IObservable<ViewModelBase?> whenAnalysis,
            IObservable<ViewModelBase?> whenAlignment,
            params IObservable<ViewModelBase?>[] viewmodels) {

            SelectedIndex = new ReactivePropertySlim<int>().AddTo(Disposables);

            whenAnalysisFileSelecting = Array.IndexOf(viewmodels, whenAnalysis);
            whenAlignmentFileSelecting = Array.IndexOf(viewmodels, whenAlignment);

            ViewModels = viewmodels
                .CombineLatest()
                .StartWith(new ViewModelBase[0])
                .Select(xs => new ObservableCollection<ViewModelBase?>(xs))
                .Select(xs => new ReadOnlyObservableCollection<ViewModelBase?>(xs))
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            SelectedIndex.Value = whenAnalysisFileSelecting;
        }

        public ViewModelSwitcher(
            IObservable<ViewModelBase?> whenAnalysis,
            IObservable<ViewModelBase?> whenAlignment,
            IReadOnlyList<IObservable<ViewModelBase?>> viewmodels)
            : this(whenAnalysis, whenAlignment, viewmodels.ToArray()) {

        }

        public ReactivePropertySlim<int> SelectedIndex { get; }

        public ReadOnlyReactivePropertySlim<ReadOnlyObservableCollection<ViewModelBase?>> ViewModels { get; }

        public void SelectAnalysisFile() {
            if (whenAnalysisFileSelecting >= 0 && whenAnalysisFileSelecting < ViewModels.Value.Count) {
                SelectedIndex.Value = whenAnalysisFileSelecting;
            }
        }
        private readonly int whenAnalysisFileSelecting;

        public void SelectAlignmentFile() {
            if (whenAlignmentFileSelecting >= 0 && whenAlignmentFileSelecting < ViewModels.Value.Count) {
                SelectedIndex.Value = whenAlignmentFileSelecting;
            }
        }
        private readonly int whenAlignmentFileSelecting;
    }
}
