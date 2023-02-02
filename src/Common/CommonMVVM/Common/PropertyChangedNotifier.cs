using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace CompMs.CommonMVVM.Common
{
    public class PropertyChangedNotifier : IDisposable
    {
        public PropertyChangedNotifier(INotifyPropertyChanged source) {
            this.source = source;
            this.source.PropertyChanged += OnPropertyChanged;
        }

        public PropertyChangedNotifier SubscribeTo(string name, Action handler) {
            if (!disposedValue) {
                if (!actionMap.ContainsKey(name)) {
                    actionMap[name] = new List<Action>();
                }
                actionMap[name].Add(handler);
            }
            return this;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (actionMap.TryGetValue(e.PropertyName, out var actions)) {
                actions.ForEach(action => action());
            }
        }

        private INotifyPropertyChanged source;
        private Dictionary<string, List<Action>> actionMap = new Dictionary<string, List<Action>>();

        private bool disposedValue;

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    source.PropertyChanged -= OnPropertyChanged;
                    source = null;
                    actionMap = null;
                }

                disposedValue = true;
            }
        }

        public void Dispose() {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
