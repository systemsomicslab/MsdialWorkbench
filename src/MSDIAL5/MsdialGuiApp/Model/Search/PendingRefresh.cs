using System;
using System.ComponentModel;
using System.Runtime.ExceptionServices;
using System.Windows.Threading;

namespace CompMs.App.Msdial.Model.Search
{
    /// <summary>
    /// Refreshes one collection view, or, while the view is in an edit transaction (refreshing it would throw),
    /// holds the refresh until the transaction ends and then refreshes it once.
    /// </summary>
    /// <remarks>
    /// The edit state is read from the view itself (<see cref="IEditableCollectionView"/>), never from a flag pushed by a table.
    /// The held refresh is posted to the dispatcher instead of run in the end-of-edit notification,
    /// because the view is still finishing the commit when it raises that notification.
    /// A refresh that fails is never thrown to the caller: it is thrown again from its own dispatcher operation,
    /// so the application's unhandled-exception handling reports it, whether the refresh ran at once or was held.
    /// </remarks>
    internal sealed class PendingRefresh : IDisposable
    {
        private enum State
        {
            // Nothing is waiting.
            Idle,
            // A refresh waits for the view's edit transaction to end.
            Held,
            // The edit ended and the refresh is posted to the dispatcher.
            Posted,
            Disposed,
        }

        private readonly ICollectionView _view;
        private readonly Dispatcher _dispatcher;
        private State _state = State.Idle;

        public PendingRefresh(ICollectionView view) {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            // Collection views from WPF are DispatcherObjects; another ICollectionView is used on the thread that creates this.
            _dispatcher = (view as DispatcherObject)?.Dispatcher ?? Dispatcher.CurrentDispatcher;
            if (_view is INotifyPropertyChanged notifier) {
                notifier.PropertyChanged += OnViewPropertyChanged;
            }
        }

        /// <summary>
        /// Refreshes the view now, or once its edit transaction ends. Requests made during one edit lead to one refresh.
        /// </summary>
        /// <remarks>A request made after the edit ended but before the held refresh ran is left to that refresh.</remarks>
        public void Request() {
            switch (_state) {
                case State.Disposed:
                // While a refresh is posted for an edit that just ended, the table may still be ending that edit's row:
                // the posted refresh does this one too, instead of refreshing inside the row commit.
                case State.Posted:
                    return;
            }
            if (IsInEdit) {
                _state = State.Held;
                return;
            }
            _state = State.Idle;
            Refresh();
        }

        // Refreshing throws during an EditItem or an AddNew transaction alike.
        private bool IsInEdit => _view is IEditableCollectionView editable && (editable.IsEditingItem || editable.IsAddingNew);

        private void OnViewPropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (_state != State.Held || !IsEditStateChange(e.PropertyName) || IsInEdit) {
                return;
            }
            _state = State.Posted;
            // Background is below DataBind on purpose. A table that closes the view's transaction itself
            // (DataGridCommitPendingEditBehavior.CommitPendingEdit) then ends the DataGrid row, and DataGrid runs the queued
            // DataBind work while doing so: a refresh posted at DataBind or above would run inside that row commit.
            _dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(RefreshIfStillPosted));
        }

        private static bool IsEditStateChange(string? propertyName) {
            return string.IsNullOrEmpty(propertyName)
                || propertyName == nameof(IEditableCollectionView.IsEditingItem)
                || propertyName == nameof(IEditableCollectionView.IsAddingNew);
        }

        private void RefreshIfStillPosted() {
            if (_state != State.Posted) {
                return;
            }
            // A new edit may have begun before this ran: the refresh then waits for that edit's end.
            if (IsInEdit) {
                _state = State.Held;
                return;
            }
            _state = State.Idle;
            Refresh();
        }

        private void Refresh() {
            try {
                _view.Refresh();
            }
            catch (Exception ex) {
                var error = ExceptionDispatchInfo.Capture(ex);
                _dispatcher.BeginInvoke(new Action(error.Throw));
            }
        }

        public void Dispose() {
            if (_state == State.Disposed) {
                return;
            }
            _state = State.Disposed;
            if (_view is INotifyPropertyChanged notifier) {
                notifier.PropertyChanged -= OnViewPropertyChanged;
            }
        }
    }
}
