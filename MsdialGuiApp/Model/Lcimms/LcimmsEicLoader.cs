using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.Common.Components;
using CompMs.Common.Extension;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialLcImMsApi.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Lcimms
{
    internal sealed class LcimmsEicLoader : IChromatogramLoader
    {
        private readonly MsdialLcImMsParameter _parameter;
        private readonly RawSpectra _rawSpectra;

        public LcimmsEicLoader(IDataProvider provider, MsdialLcImMsParameter parameter) {
            if (provider is null) {
                throw new ArgumentNullException(nameof(provider));
            }

            _parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
            _rawSpectra = new RawSpectra(provider, parameter.IonMode, parameter.AcquisitionType);
        }

        public Task<DataObj.Chromatogram> LoadChromatogramAsync(ChromatogramPeakFeatureModel target, CancellationToken token) {
            if (target is null) {
                return Task.FromResult(new DataObj.Chromatogram(new List<PeakItem>(), new List<PeakItem>(), null, string.Empty, Colors.Black, ChromXType.Drift, ChromXUnit.Msec));
            }
            return Task.Run(() =>
            {
                var ms1Peaks = _rawSpectra.GetDriftChromatogramByScanRtMz(target.InnerModel.MS1RawSpectrumIdTop, (float)target.InnerModel.ChromXsTop.RT.Value, (float)_parameter.AccumulatedRtRange, (float)target.Mass, _parameter.CentroidMs1Tolerance);
                var eic = ms1Peaks.Smoothing(_parameter.SmoothingMethod, _parameter.SmoothingLevel)
                    .Where(peak => peak != null)
                    .Select(peak => new PeakItem(peak))
                    .ToList();
                var area = eic.Where(peak => target.ChromXLeftValue <= peak.Time && peak.Time <= target.ChromXRightValue).ToList();
                var top = area.Argmin(peak => Math.Abs(target.ChromXValue.Value - peak.Time));
                return new DataObj.Chromatogram(eic, area, top, string.Empty, Colors.Black, ChromXType.Drift, ChromXUnit.Msec, $"EIC chromatogram of {target.Mass:N4} tolerance [Da]: {_parameter.CentroidMs1Tolerance:F} RT [min]: {target.InnerModel.ChromXsTop.RT.Value:F2} tolerance [min]: {_parameter.AccumulatedRtRange} Max intensity: {area.Max(peak => peak.Intensity):F0}");
            });
        }
    }
}
