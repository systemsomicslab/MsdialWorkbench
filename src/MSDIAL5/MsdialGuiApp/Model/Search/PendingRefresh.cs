using System;
using System.ComponentModel;
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
    /// </remarks>
    internal sealed class PendingRefresh : IDisposable
    {
        private readonly ICollectionView _view;
        private readonly Dispatcher _dispatcher;
        private bool _pending;
        private bool _posted;
        private bool _disposed;

        public PendingRefresh(ICollectionView view) {
            _view = view ?? throw new ArgumentNullException(nameof(view));
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
            if (_disposed) {
                return;
            }
            // While a refresh is posted for an edit that just ended, the table may still be ending that edit's row:
            // the posted refresh does this one too, instead of refreshing inside the row commit.
            if (IsInEdit || _posted) {
                _pending = true;
                return;
            }
            _pending = false;
            _view.Refresh();
        }

        private bool IsInEdit => _view is IEditableCollectionView editable && (editable.IsEditingItem || editable.IsAddingNew);

        private void OnViewPropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (!IsEditStateChange(e.PropertyName) || !_pending || IsInEdit || _posted) {
                return;
            }
            _posted = true;
            // Background is below DataBind on purpose. A table that closes the view's transaction itself
            // (DataGridPendingEditBehavior.CommitPendingEdit) then ends the DataGrid row, and DataGrid runs the queued
            // DataBind work while doing so: a refresh posted at DataBind or above would run inside that row commit.
            _dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(RefreshIfStillPending));
        }

        private static bool IsEditStateChange(string? propertyName) {
            return string.IsNullOrEmpty(propertyName)
                || propertyName == nameof(IEditableCollectionView.IsEditingItem)
                || propertyName == nameof(IEditableCollectionView.IsAddingNew);
        }

        private void RefreshIfStillPending() {
            _posted = false;
            // A new edit may have begun before this ran: the refresh then waits for that edit's end.
            if (_disposed || !_pending || IsInEdit) {
                return;
            }
            _pending = false;
            _view.Refresh();
        }

        public void Dispose() {
            if (_disposed) {
                return;
            }
            _disposed = true;
            _pending = false;
            if (_view is INotifyPropertyChanged notifier) {
                notifier.PropertyChanged -= OnViewPropertyChanged;
            }
        }
    }
}
