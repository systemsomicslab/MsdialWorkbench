using CompMs.MsdialCore.Parameter;
using CompMs.MsdialImmsCore.Parameter;

namespace CompMs.App.Msdial.ViewModel
{
    class MsdialProjectParameterVM<T> : ParameterBaseVM where T : ParameterBase
    {
        private readonly T innerModel;
        public MsdialProjectParameterVM(T innerModel) : base(innerModel) {
            this.innerModel = innerModel;
        }
    }
}
