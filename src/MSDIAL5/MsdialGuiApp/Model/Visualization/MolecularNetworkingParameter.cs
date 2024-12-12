using CompMs.App.Msdial.Model.Setting;
using CompMs.MsdialCore.Parameter;

namespace CompMs.App.Msdial.Model.Visualization;

public sealed class MolecularNetworkingParameter(MolecularSpectrumNetworkingBaseParameter parameter)
{
    public MolecularSpectrumNetworkingBaseParameter BaseParameter { get; } = parameter;
    public bool UseCurrentFiltering { get; set; }
    public bool CutByExcelLimit { get; set; }
    public NetworkVisualizationType NetworkPresentationType { get; set; }
    public string CyRestApiUrl { get; set; } = string.Empty;
}
