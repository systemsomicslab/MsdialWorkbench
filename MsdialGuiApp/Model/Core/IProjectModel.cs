using CompMs.Common.Enum;

namespace CompMs.App.Msdial.Model.Core
{
    public interface IProjectModel
    {
        void Add();
        void Change(IDatasetModel dataset);
        void Reprocess(ProcessOption processOption);
        bool Start();
    }
}