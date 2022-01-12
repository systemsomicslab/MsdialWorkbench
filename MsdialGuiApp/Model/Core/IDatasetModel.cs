using CompMs.Common.Enum;

namespace CompMs.App.Msdial.Model.Core
{
    public interface IDatasetModel
    {
        IProcessCommand CreateProcessQuery(ProcessOption processOption);
    }
}