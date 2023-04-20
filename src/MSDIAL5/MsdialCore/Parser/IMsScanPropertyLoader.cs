using CompMs.Common.Interfaces;

namespace CompMs.MsdialCore.Parser
{
    public interface IMsScanPropertyLoader<in T>
    {
        IMSScanProperty Load(T source);
    }
}
