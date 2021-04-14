using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace CompMs.Graphics.AxisManager.Generic
{
    public class AutoContinuousAxisManager<T> : ContinuousAxisManager<T> where T: IConvertible
    {
        public AutoContinuousAxisManager(ObservableCollection<T> source) : base(source) {
            this.source = source;
            this.source.CollectionChanged += Source_CollectionChanged;
        }

        private static readonly double eps = 1e-10;
        private void Source_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            switch (e.Action) {
                case NotifyCollectionChangedAction.Reset:
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Replace:
                    break;
            }
        }

        private ObservableCollection<T> source;
    }
}
