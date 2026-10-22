using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;

namespace CompMs.App.RawDataViewer.ViewModel
{
    /// <summary>
    /// What a panel of the summary window shows once the model task behind it has ended: its view model, or why there is none.
    /// </summary>
    public sealed class PanelLoadResult<T> where T : class
    {
        private PanelLoadResult(T value, string errorMessage) {
            Value = value;
            ErrorMessage = errorMessage;
        }

        public T Value { get; }

        public string ErrorMessage { get; }

        public bool IsFailed => !(ErrorMessage is null);

        public static PanelLoadResult<T> Loaded(T value) {
            return new PanelLoadResult<T>(value ?? throw new ArgumentNullException(nameof(value)), null);
        }

        public static PanelLoadResult<T> Failed(Exception exception) {
            return new PanelLoadResult<T>(null, PanelLoadResult.Describe(exception));
        }
    }

    public static class PanelLoadResult
    {
        /// <summary>
        /// Emits one result when <paramref name="task"/> ends: the view model built from its model, or the failure
        /// if the task faults, is canceled, or the view model cannot be built. It never ends, neither in OnError nor
        /// in OnCompleted.
        /// </summary>
        public static IObservable<PanelLoadResult<TViewModel>> FromTask<TModel, TViewModel>(Task<TModel> task, Func<TModel, TViewModel> createViewModel)
            where TViewModel : class {
            // ReadOnlyReactivePropertySlim rethrows any OnError it receives. Behind Task.ToObservable the rethrow happens in
            // a task continuation that nobody waits on, so it was lost as an unobserved task exception and the panel showed
            // "Loading..." for ever. Caught here, the failure becomes a value and that continuation completes normally.
            // The continuation reads the faulted task only while the panel is subscribed; SummarizedDataModel observes the
            // fault of each of its tasks whether or not a panel is left to read it.
            // ReadOnlyReactivePropertySlim also disposes itself when its source completes, and then gives anyone who
            // subscribes later OnCompleted without its value: for a task that had already ended, which Task.ToObservable
            // emits during the subscription, every property derived from the panel would stay null. Followed by Never, the
            // source does not complete, and the panel keeps its value for later subscribers until it is disposed.
            return task.ToObservable()
                .Select(model => PanelLoadResult<TViewModel>.Loaded(createViewModel(model)))
                .Catch((Exception ex) => Observable.Return(PanelLoadResult<TViewModel>.Failed(ex)))
                .Concat(Observable.Never<PanelLoadResult<TViewModel>>());
        }

        /// <summary>
        /// The type and message of an exception and of each exception inside it, one per line, innermost cause first.
        /// The failures an AggregateException collects are described one after another, in their order.
        /// </summary>
        public static string Describe(Exception exception) {
            // Not Flatten: it moves the failures of a nested AggregateException behind those of the outer one.
            if (exception is AggregateException aggregate && aggregate.InnerExceptions.Count > 0) {
                return string.Join(Environment.NewLine + Environment.NewLine, aggregate.InnerExceptions.Select(Describe));
            }
            var chain = new List<Exception>();
            for (var e = exception; !(e is null); e = e.InnerException) {
                chain.Add(e);
            }
            chain.Reverse();
            return string.Join(Environment.NewLine, chain.Select(e => $"{e.GetType().FullName}: {e.Message}"));
        }
    }
}
