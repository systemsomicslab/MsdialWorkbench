namespace CompMs.App.Msdial.Model.Service
{
    public interface IUndoCommand {
        void Undo();
        void Redo();
    }
}
