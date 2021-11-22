using CompMs.CommonMVVM;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialImmsCore.Parameter;
using System;

namespace CompMs.App.Msdial.Model.Imms
{
    public sealed class ImmsDataCollectionSettingModel : BindableBase
    {
        public ImmsDataCollectionSettingModel(MsdialImmsParameter parameter) {
            Parameter = parameter;
            ProcessParameter = parameter.ProcessBaseParam;
            PeakPickParameter = parameter.PeakPickBaseParam;

            switch (parameter.ProviderFactoryParameter) {
                case ImmsTicDataProviderFactoryParameter ticParameter:
                    UseMs1WithHighestTic = true;
                    TimeBegin = ticParameter.TimeBegin;
                    TimeEnd = ticParameter.TimeEnd;
                    break;
                case ImmsAverageDataProviderFactoryParameter averageParameter:
                    UseMs1WithHighestTic = false;
                    UseAverageMs1 = true;
                    TimeBegin = averageParameter.TimeBegin;
                    TimeEnd = averageParameter.TimeEnd;
                    MassTolerance = averageParameter.MassTolerance;
                    DriftTolerance = averageParameter.DriftTolerance;
                    break;
            }
        }

        public bool UseMs1WithHighestTic {
            get => useMs1WithHighestTic;
            set => SetProperty(ref useMs1WithHighestTic, value);
        }
        private bool useMs1WithHighestTic = false;

        public bool UseAverageMs1 {
            get => useAverageMs1;
            set => SetProperty(ref useAverageMs1, value);
        }
        private bool useAverageMs1 = true;

        public double TimeBegin {
            get => timeBegin;
            set => SetProperty(ref timeBegin, value);
        }
        private double timeBegin = 0d;

        public double TimeEnd {
            get => timeEnd;
            set => SetProperty(ref timeEnd, value);
        }
        private double timeEnd = 100d;

        public double MassTolerance {
            get => massTolerance;
            set => SetProperty(ref massTolerance, value);
        }
        private double massTolerance = 0.01;

        public double DriftTolerance {
            get => driftTolerance;
            set => SetProperty(ref driftTolerance, value);
        }
        private double driftTolerance = 0.02;

        public MsdialImmsParameter Parameter { get; }
        public ProcessBaseParameter ProcessParameter { get; }
        public PeakPickBaseParameter PeakPickParameter { get; }

        public IImmsDataProviderFactoryParameter CreateDataProviderFactoryParameter() {
            if (UseAverageMs1) {
                return new ImmsAverageDataProviderFactoryParameter(MassTolerance, DriftTolerance, TimeBegin, TimeEnd);
            }
            else if (UseMs1WithHighestTic) {
                return new ImmsTicDataProviderFactoryParameter(TimeBegin, TimeEnd);
            }
            throw new NotSupportedException();
        }
    }
}
