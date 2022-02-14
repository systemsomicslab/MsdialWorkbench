using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;

namespace CompMs.App.Msdial.Model.Core
{
    public class DatasetModel : BindableBase, IDatasetModel
    {
        public MethodModelBase Method { get; }

        private readonly IMsdialDataStorage<ParameterBase> storage;

        public IProcessCommand CreateProcessQuery(ProcessOption processOption) {
            return new ProcessCommand();
        }
    }
}
