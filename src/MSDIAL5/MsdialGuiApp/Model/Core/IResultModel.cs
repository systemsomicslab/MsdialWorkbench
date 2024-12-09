using CompMs.App.Msdial.Model.Setting;
using CompMs.MsdialCore.Parameter;

namespace CompMs.App.Msdial.Model.Core
{
    internal interface IResultModel
    {
        void SearchFragment();
        void InvokeMsfinder();
        void ExportMoleculerNetworkingData(MolecularSpectrumNetworkingBaseParameter parameter, bool useCurrentFiltering, bool cutByExcelLimit);
        void InvokeMoleculerNetworking(MolecularSpectrumNetworkingBaseParameter parameter, bool useCurrentFiltering, NetworkPresentationType networkPresentationType, string cytoscapeUrl);
        void InvokeMoleculerNetworkingForTargetSpot();
    }
}
