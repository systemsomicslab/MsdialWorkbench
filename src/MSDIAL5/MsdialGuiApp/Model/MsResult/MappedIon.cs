namespace CompMs.App.Msdial.Model.MsResult;

internal sealed class MappedIon(string id, int experimentID, double intensity, double time, double mz)
{
    public string ID { get; } = id;
    public int ExperimentID { get; } = experimentID;
    public double Intensity { get; } = intensity;
    public double Time { get; } = time;
    public double Mz { get; } = mz;
}
