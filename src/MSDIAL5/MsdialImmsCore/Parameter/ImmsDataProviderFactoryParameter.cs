using CompMs.MsdialImmsCore.Algorithm;
using CompMs.Raw.Abstractions;
using MessagePack;

namespace CompMs.MsdialImmsCore.Parameter
{
    [Union(0, typeof(ImmsAverageDataProviderFactoryParameter))]
    [Union(1, typeof(ImmsTicDataProviderFactoryParameter))]
    public interface IImmsDataProviderFactoryParameter
    {
        IDataProviderFactory<T> Create<T>(IDataProviderFactory<T> factory);
    }

    [MessagePackObject]
    public class ImmsTicDataProviderFactoryParameter: IImmsDataProviderFactoryParameter
    {
        public ImmsTicDataProviderFactoryParameter(double timeBegin, double timeEnd) {
            TimeBegin = timeBegin;
            TimeEnd = timeEnd;
        }

        [Key(nameof(TimeBegin))]
        public double TimeBegin { get; }
        [Key(nameof(TimeEnd))]
        public double TimeEnd { get; }

        public IDataProviderFactory<T> Create<T>(IDataProviderFactory<T> factory) {
            return new ImmsRepresentativeDataProviderFactory<T>(factory, TimeBegin, TimeEnd);
        }
    }

    [MessagePackObject]
    public class ImmsAverageDataProviderFactoryParameter : IImmsDataProviderFactoryParameter
    {
        public ImmsAverageDataProviderFactoryParameter(double massTolerance, double driftTolerance, double timeBegin, double timeEnd) {
            MassTolerance = massTolerance;
            DriftTolerance = driftTolerance;
            TimeBegin = timeBegin;
            TimeEnd = timeEnd;
        }

        [Key(nameof(MassTolerance))]
        public double MassTolerance { get; }
        [Key(nameof(DriftTolerance))]
        public double DriftTolerance { get; }
        [Key(nameof(TimeBegin))]
        public double TimeBegin { get; }
        [Key(nameof(TimeEnd))]
        public double TimeEnd { get; }

        public IDataProviderFactory<T> Create<T>(IDataProviderFactory<T> providerFactory) {
            return new ImmsAverageDataProviderFactory<T>(providerFactory, MassTolerance, DriftTolerance, TimeBegin, TimeEnd);
        }
    }
}
