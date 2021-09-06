using CompMs.CommonMVVM;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialDimsCore.Parameter;
using System;

namespace CompMs.App.Msdial.Model.Dims
{
    public sealed class DimsDataCollectionSettingModel : BindableBase
    {
        public DimsDataCollectionSettingModel(ProcessBaseParameter processParameter, PeakPickBaseParameter peakPickParameter) {
            ProcessParameter = processParameter;
            PeakPickParameter = peakPickParameter;
        }

        public bool UseMs1WithHighestTic {
            get => useMs1WithHighestTic;
            set => SetProperty(ref useMs1WithHighestTic, value);
        }
        private bool useMs1WithHighestTic = true;

        public bool UseMs1WithHighestBpi {
            get => useMs1WithHighestBpi;
            set => SetProperty(ref useMs1WithHighestBpi, value);
        }
        private bool useMs1WithHighestBpi = false;

        public bool UseAverageMs1 {
            get => useAverageMs1;
            set => SetProperty(ref useAverageMs1, value);
        }
        private bool useAverageMs1 = false;

        public double TimeBegin {
            get => timeBegin;
            set => SetProperty(ref timeBegin, value);
        }
        private double timeBegin = 0.5;

        public double TimeEnd {
            get => timeEnd;
            set => SetProperty(ref timeEnd, value);
        }
        private double timeEnd = 6;

        public double MassTolerance {
            get => massTolerance;
            set => SetProperty(ref massTolerance, value);
        }
        private double massTolerance = 0.01;

        public ProcessBaseParameter ProcessParameter { get; }
        public PeakPickBaseParameter PeakPickParameter { get; }

        public IDimsDataProviderFactoryParameter CreateDataProviderFactoryParameter() {
            if (UseMs1WithHighestTic) {
                return new DimsTicDataProviderFactoryParameter(TimeBegin, TimeEnd);
            }
            else if (UseMs1WithHighestBpi) {
                return new DimsBpiDataProviderFactoryParameter(TimeBegin, TimeEnd);
            }
            else if (UseAverageMs1) {
                return new DimsAverageDataProviderFactoryParameter(TimeBegin, TimeEnd, MassTolerance);
            }
            throw new NotSupportedException();
        }
    }
}
