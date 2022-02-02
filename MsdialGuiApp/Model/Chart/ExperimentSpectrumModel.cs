using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace CompMs.App.Msdial.Model.Chart
{
    public class ExperimentSpectrumModel : BindableBase {
        private readonly IDataProvider provider;

        public ExperimentSpectrumModel(
            RangeSelectableChromatogramModel model,
            IDataProvider provider,
            ChromatogramPeakFeature peak,
            IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer,
            ParameterBase parameter) {

            RangeSelectableChromatogramModel = model;
            this.provider = provider;
            Spectrums = new ObservableCollection<SpectrumCollection>();
            Peak = peak;
            Refer = refer;
            Parameter = parameter;
        }

        public RangeSelectableChromatogramModel RangeSelectableChromatogramModel { get; }

        public ObservableCollection<SpectrumCollection> Spectrums {
            get => spectrums;
            set => SetProperty(ref spectrums, value);
        }
        private ObservableCollection<SpectrumCollection> spectrums;

        public ChromatogramPeakFeature Peak { get; }

        public IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> Refer { get; }

        public ParameterBase Parameter { get; }

        public bool CanSetExperimentSpectrum() {
            return RangeSelectableChromatogramModel.SelectedRanges.Count == 2;
        }

        public void SetExperimentSpectrum() {
            var spectrum = provider.LoadMsNSpectrums(level: 2);
            var experiments = spectrum.Select(spec => spec.ExperimentID).Distinct().OrderBy(v => v).ToArray();

            var rangeModel = RangeSelectableChromatogramModel;
            (var mainStart, var mainEnd) = rangeModel.ConvertToRt(rangeModel.SelectedRanges[1]);
            (var subStart, var subEnd) = rangeModel.ConvertToRt(rangeModel.SelectedRanges[0]);

            Spectrums = new ObservableCollection<SpectrumCollection>(experiments.Select(exp => new SpectrumCollection(DataAccess.GetSubtractSpectrum(spectrum, mainStart, mainEnd, subStart, subEnd, 1e-2, exp), exp)));
        }

        public void SaveSpectrumAsNist(string mspFileName) {
            var comment = Peak?.Comment ?? string.Empty;
            using (var fs = File.Open(mspFileName, FileMode.Create, FileAccess.Write, FileShare.None)) {
                foreach (var spectra in Spectrums) {
                    Peak.Comment = $"{comment}|ExperimentId={spectra.ExperimentId}";
                    SpectraExport.SaveSpectraTableAsNistFormat(fs, Peak, spectra.Spectrums, Refer, Parameter);
                }
            }
            Peak.Comment = comment;
        }

        public bool CanSaveSpectrumAsNist(string mspFileName) {
            return Spectrums != null;
        }

    }

    public class SpectrumCollection : BindableBase
    {
        public SpectrumCollection(List<SpectrumPeak> spectrums, int experimentId) {
            Spectrums = spectrums;
            ExperimentId = experimentId;
        }

        public List<SpectrumPeak> Spectrums {
            get => spectrums;
            set => SetProperty(ref spectrums, value);
        }
        private List<SpectrumPeak> spectrums;

        public int ExperimentId { get; }
    }
}
