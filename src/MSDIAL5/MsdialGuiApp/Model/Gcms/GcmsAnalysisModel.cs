using CompMs.App.Msdial.Model.Core;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Parameter;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Gcms
{
    internal sealed class GcmsAnalysisModel : BindableBase, IAnalysisModel
    {
        // IAnalysisModel interface
        Task IAnalysisModel.SaveAsync(CancellationToken token) {
            throw new NotImplementedException();
        }

        // IResultModel interface
        void IResultModel.InvokeMsfinder() {
            throw new NotImplementedException();
        }

        void IResultModel.SearchFragment() {
            throw new NotImplementedException();
        }

        void IResultModel.ExportMoleculerNetworkingData(MolecularSpectrumNetworkingBaseParameter parameter) {
            throw new NotImplementedException();
        }

        void IResultModel.InvokeMoleculerNetworking(MolecularSpectrumNetworkingBaseParameter parameter) {
            throw new NotImplementedException();
        }

        void IResultModel.InvokeMoleculerNetworkingForTargetSpot() {
            throw new NotImplementedException();
        }
    }
}
