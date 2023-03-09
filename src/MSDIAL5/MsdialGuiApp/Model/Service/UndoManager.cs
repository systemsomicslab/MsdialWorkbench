using System.Collections.Generic;
using System.Linq;

namespace CompMs.App.Msdial.Model.Service
{
    public sealed class UndoManager {
        private static readonly int UNDO_LIMIT = 10;
        private static readonly int REDO_LIMIT = 10;
        private readonly LinkedList<IUndoCommand> _undos = new LinkedList<IUndoCommand>();
        private readonly LinkedList<IUndoCommand> _redos = new LinkedList<IUndoCommand>();

        public void Add(IUndoCommand command) {
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
            node.Value.Redo();
            _undos.AddFirst(node);
            Shrink(_undos, UNDO_LIMIT);
        }

        private void Shrink(LinkedList<IUndoCommand> list, int limit) {
            while (list.Count > limit) {
                list.RemoveLast();
            }
        }
    }
}
