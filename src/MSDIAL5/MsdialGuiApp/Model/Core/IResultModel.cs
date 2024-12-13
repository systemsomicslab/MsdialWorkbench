using CompMs.App.Msdial.Model.Visualization;

namespace CompMs.App.Msdial.Model.Core
{
    internal interface IResultModel
    {
        void SearchFragment();
        void InvokeMsfinder();
        void ExportMoleculerNetworkingData(MolecularNetworkingParameter parameter);
        void InvokeMoleculerNetworking(MolecularNetworkingParameter parameter);
        void InvokeMoleculerNetworkingForTargetSpot();
    }
}
