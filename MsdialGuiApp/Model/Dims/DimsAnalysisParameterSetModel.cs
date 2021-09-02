using CompMs.App.Msdial.Model.Core;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialDimsCore.Algorithm;
using CompMs.MsdialDimsCore.Parameter;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Model.Dims
{
    public class DimsAnalysisParameterSetModel : AnalysisParameterSetModelBase
    {
        public DimsAnalysisParameterSetModel(MsdialDimsParameter parameter, IEnumerable<AnalysisFileBean> files)
            : base(parameter, files) {
            Parameter = parameter;
        }

        public MsdialDimsParameter Parameter { get; }

        public bool UseMs1WithHighestTic {
            get => useMs1WithHighestTic;
            set => SetProperty(ref useMs1WithHighestTic, value);
        }
        private bool useMs1WithHighestTic;

        public bool UseMs1WithHighestBpi {
            get => useMs1WithHighestBpi;
            set => SetProperty(ref useMs1WithHighestBpi, value);
        }
        private bool useMs1WithHighestBpi;

        public bool UseAverageMs1 {
            get => useAverageMs1;
            set => SetProperty(ref useAverageMs1, value);
        }
        private bool useAverageMs1;

        public double TimeBegin {
            get => timeBegin;
            set => SetProperty(ref timeBegin, value);
        }
        private double timeBegin;

        public double TimeEnd {
            get => timeEnd;
            set => SetProperty(ref timeEnd, value);
        }
        private double timeEnd;

        public double MassTolerance {
            get => massTolerance;
            set => SetProperty(ref massTolerance, value);
        }
        private double massTolerance;

        public IDataProviderFactory<AnalysisFileBean> BuildDataProviderFactory() {
            if (UseMs1WithHighestBpi) {
                return new DimsBpiDataProviderFactory(TimeBegin, TimeEnd, retry: 5, isGuiProcess: true);
            }
            else if(UseMs1WithHighestTic){
                return new DimsTicDataProviderFactory(TimeBegin, TimeEnd, retry: 5, isGuiProcess: true);
            }
            else if (UseAverageMs1) {
                return new DimsAverageDataProviderFactory(MassTolerance, TimeBegin, TimeEnd, retry: 5, isGuiProcess: true);
            }
            else {
                return new StandardDataProviderFactory(retry: 5, isGuiProcess: true);
            }
        }
    }
}
