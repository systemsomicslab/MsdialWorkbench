namespace CompMs.App.Msdial.Model.MsResult;

internal sealed class MappedIon(string id, int experimentID, double intensity)
{
    public string ID { get; } = id;
    public int ExperimentID { get; } = experimentID;
    public double Intensity { get; } = intensity;
}
