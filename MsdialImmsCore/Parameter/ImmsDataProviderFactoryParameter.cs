using CompMs.Common.DataObj;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialImmsCore.Algorithm;
using MessagePack;

namespace CompMs.MsdialImmsCore.Parameter
{
    [Union(0, typeof(ImmsAverageDataProviderFactoryParameter))]
    [Union(1, typeof(ImmsTicDataProviderFactoryParameter))]
    public interface IImmsDataProviderFactoryParameter
    {
        IDataProviderFactory<AnalysisFileBean> Create(int retry, bool isGuiProcess);
        IDataProviderFactory<RawMeasurement> Create();
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

        public IDataProviderFactory<AnalysisFileBean> Create(int retry, bool isGuiProcess) {
            return new ImmsRepresentativeDataProviderFactory(TimeBegin, TimeEnd, retry, isGuiProcess);
        }

        public IDataProviderFactory<RawMeasurement> Create() {
            return new ImmsRepresentativeDataProviderFactory(TimeBegin, TimeEnd);
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

        public IDataProviderFactory<AnalysisFileBean> Create(int retry, bool isGuiProcess) {
            return new ImmsAverageDataProviderFactory(MassTolerance, DriftTolerance, TimeBegin, TimeEnd, retry, isGuiProcess);
        }

        public IDataProviderFactory<RawMeasurement> Create() {
            return new ImmsRepresentativeDataProviderFactory(TimeBegin, TimeEnd);
        }
    }
}
