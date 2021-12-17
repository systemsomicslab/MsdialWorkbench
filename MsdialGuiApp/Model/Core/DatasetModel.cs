using CompMs.Common.Enum;
using CompMs.CommonMVVM;

namespace CompMs.App.Msdial.Model.Core
{
    public class DatasetModel : BindableBase, IDatasetModel
    {
        public MethodModelBase Method { get; }

        public IProcessCommand CreateProcessQuery(ProcessOption processOption) {
            return new ProcessCommand();
        }
    }
}
