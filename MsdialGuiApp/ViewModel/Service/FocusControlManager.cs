using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace CompMs.App.Msdial.ViewModel.Service
{
    internal sealed class FocusControlManager : IDisposable
    {
        private readonly object _syncObject = new object();
        private readonly Subject<object> _subject = new Subject<object>();
        private readonly List<object> _identifiers = new List<object>();

        public (Action, IObservable<bool>) Request() {
            var identifier = new object();
            _identifiers.Add(identifier);

            var observable = _subject.Select(id => id == identifier);
            void action() {
                _subject.OnNext(identifier);
            }
            return (action, observable);
        }

        public void Dispose() {
            lock (_syncObject) {
                _subject.Dispose();
                _identifiers.Clear();
            }
        }
    }
}
