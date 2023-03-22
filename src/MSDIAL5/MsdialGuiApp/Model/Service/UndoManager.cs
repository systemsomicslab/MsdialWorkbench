using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.App.Msdial.Model.Service
{
    public sealed class UndoManager : IDisposable {
        private static readonly int UNDO_LIMIT = 10;
        private static readonly int REDO_LIMIT = 10;
        private readonly LinkedList<IDoCommand> _undos = new LinkedList<IDoCommand>();
        private readonly LinkedList<IDoCommand> _redos = new LinkedList<IDoCommand>();
        private bool _disposedValue;

        public void Add(IDoCommand command) {
            _undos.AddFirst(command);
            Shrink(_undos, UNDO_LIMIT);
            _redos.Clear();
        }
    
        public void Undo() {
            if (!_undos.Any()) {
                return;
            }
            var node = _undos.First;
            _undos.RemoveFirst();
            node.Value.Undo();
            _redos.AddFirst(node);
            Shrink(_redos, REDO_LIMIT);
        }

        public void Redo() {
            if (!_redos.Any()) {
                return;
            }
            var node = _redos.First;
            _redos.RemoveFirst();
            node.Value.Do();
            _undos.AddFirst(node);
            Shrink(_undos, UNDO_LIMIT);
        }

        private void Shrink(LinkedList<IDoCommand> list, int limit) {
            while (list.Count > limit) {
                list.RemoveLast();
            }
        }

        private void Dispose(bool disposing) {
            if (!_disposedValue) {
                if (disposing) {
                    _undos.Clear();
                    _redos.Clear();
                }

                _disposedValue = true;
            }
        }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
