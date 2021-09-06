using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialDimsCore.Algorithm;
using MessagePack;
using System;

namespace CompMs.MsdialDimsCore.Parameter
{
    [Union(0, typeof(DimsBpiDataProviderFactoryParameter))]
    [Union(1, typeof(DimsTicDataProviderFactoryParameter))]
    [Union(2, typeof(DimsAverageDataProviderFactoryParameter))]
    public interface IDimsDataProviderFactoryParameter
    {
        IDataProviderFactory<AnalysisFileBean> Create(int retry, bool isGuiProcess);
    }

    [MessagePackObject]
    public class DimsBpiDataProviderFactoryParameter : IDimsDataProviderFactoryParameter
    {
        public DimsBpiDataProviderFactoryParameter(double timeBegin, double timeEnd) {
            TimeBegin = timeBegin;
            TimeEnd = timeEnd;
        }

        [Key(nameof(TimeBegin))]
        public double TimeBegin { get; }
        [Key(nameof(TimeEnd))]
        public double TimeEnd { get; }

        public IDataProviderFactory<AnalysisFileBean> Create(int retry, bool isGuiProcess) {
            return new DimsBpiDataProviderFactory(TimeBegin, TimeEnd, retry, isGuiProcess);
        }
    }

    [MessagePackObject]
    public class DimsTicDataProviderFactoryParameter: IDimsDataProviderFactoryParameter
    {
        public DimsTicDataProviderFactoryParameter(double timeBegin, double timeEnd) {
            TimeBegin = timeBegin;
            TimeEnd = timeEnd;
        }

        [Key(nameof(TimeBegin))]
        public double TimeBegin { get; }
        [Key(nameof(TimeEnd))]
        public double TimeEnd { get; }

        public IDataProviderFactory<AnalysisFileBean> Create(int retry, bool isGuiProcess) {
            return new DimsTicDataProviderFactory(TimeBegin, TimeEnd, retry, isGuiProcess);
        }
    }

    [MessagePackObject]
    public class DimsAverageDataProviderFactoryParameter: IDimsDataProviderFactoryParameter
    {
        public DimsAverageDataProviderFactoryParameter(double timeBegin, double timeEnd, double massTolerance) {
            TimeBegin = timeBegin;
            TimeEnd = timeEnd;
            MassTolerance = massTolerance;
        }

        [Key(nameof(TimeBegin))]
        public double TimeBegin { get; }
        [Key(nameof(TimeEnd))]
        public double TimeEnd { get; }
        [Key(nameof(MassTolerance))]
        public double MassTolerance { get; }

        public IDataProviderFactory<AnalysisFileBean> Create(int retry, bool isGuiProcess) {
            return new DimsAverageDataProviderFactory(MassTolerance, TimeBegin, TimeEnd, retry, isGuiProcess);
        }
    }
}
