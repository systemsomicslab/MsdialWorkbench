using CompMs.Common.Components;
using CompMs.Common.DataObj.Database;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.MsdialCore.MSDec {

    // this method is for ms/ms spectrum deconvolution
    public sealed class MSDecObjectHandler {
        private MSDecObjectHandler() { }
        public static List<SpectrumPeak> ReplaceDeconvolutedIsopicIonsToOriginalPrecursorIons(MSDecResult result, List<SpectrumPeak> curatedSpectra,
            ChromatogramPeakFeature chromPeakFeature, ParameterBase param) {
            var isotopicRange = param.KeptIsotopeRange;
            var replacedSpectrum = new List<SpectrumPeak>();

            foreach (var spec in result.Spectrum) {
                if (spec.Mass < chromPeakFeature.PrecursorMz - param.CentroidMs2Tolerance)
                    replacedSpectrum.Add(spec);
            }

            foreach (var spec in curatedSpectra) {
                if (spec.Mass >= chromPeakFeature.PrecursorMz - param.CentroidMs2Tolerance)
                    replacedSpectrum.Add(spec);
            }

            return replacedSpectrum.OrderBy(n => n.Mass).ToList();
        }

        public static MSDecResult GetDefaultMSDecResult(ChromatogramPeakFeature chromPeakFeature) {
            var result = new MSDecResult();

            result.ChromXs = chromPeakFeature.ChromXs;
            result.RawSpectrumID = chromPeakFeature.MS2RawSpectrumID;
            result.PrecursorMz = chromPeakFeature.Mass;
            result.ModelPeakMz = (float)chromPeakFeature.Mass;
            result.ModelPeakHeight = (float)chromPeakFeature.PeakHeightTop;
            result.IonMode = chromPeakFeature.IonMode;
            return result;
        }

        public static MSDecResult GetMSDecResultByRawSpectrum(ChromatogramPeakFeature chromPeakFeature, List<SpectrumPeak> spectra) {

            var result = new MSDecResult();
            result.ChromXs = chromPeakFeature.ChromXs;
            result.RawSpectrumID = chromPeakFeature.MS2RawSpectrumID;
            result.PrecursorMz = chromPeakFeature.Mass;
            result.ModelPeakMz = (float)chromPeakFeature.Mass;
            result.ModelPeakHeight = (float)chromPeakFeature.PeakHeightTop;
            result.Spectrum = spectra;
            result.IonMode = chromPeakFeature.IonMode;
            return result;
        }

        public static MSDecResult GetAndromedaSpectrum(ChromatogramPeakFeature chromPeakFeature, List<SpectrumPeak> spectra,
            ParameterBase param, IupacDatabase iupac, int precursorCharge) {

            var result = new MSDecResult();
            result.ChromXs = chromPeakFeature.ChromXs;
            result.RawSpectrumID = chromPeakFeature.MS2RawSpectrumID;
            result.PrecursorMz = chromPeakFeature.Mass;
            result.ModelPeakMz = (float)chromPeakFeature.Mass;
            result.ModelPeakHeight = (float)chromPeakFeature.PeakHeightTop;
            result.IonMode = chromPeakFeature.IonMode;
            result.Spectrum = DataAccess.GetAndromedaMS2Spectrum(spectra, param, iupac, precursorCharge);
            return result;
        }
    }
}
