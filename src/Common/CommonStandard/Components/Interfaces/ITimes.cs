using CompMs.Common.Components;

namespace CompMs.Common.Interfaces
{
    public interface IChromXs
    {
        RetentionTime RT { get; set; }
        RetentionIndex RI { get; set; }
        DriftTime Drift { get; set; }
        MzValue Mz { get; set; }
        ChromXType MainType { get; set; }
    }
}
