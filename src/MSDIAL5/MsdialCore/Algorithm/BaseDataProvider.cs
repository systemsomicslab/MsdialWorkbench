using CompMs.Common.DataObj;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using CompMs.Raw.Contract;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.Algorithm
{
    public abstract class BaseDataProvider : IDataProvider
    {
        private readonly Task<IList<RawSpectrum>> _spectraTask;

        protected BaseDataProvider(IEnumerable<RawSpectrum> spectrums) {
            if (spectrums is null) {
                throw new ArgumentNullException(nameof(spectrums));
            }

            _spectraTask = Task.Run(() => {
                var result = (spectrums as IList<RawSpectrum>) ?? spectrums.ToList();
                foreach (var s in result) {
                    if (s.MsLevel == 0) {
                        s.MsLevel = 1;
                    }
                }
                return result;
            });
        }

        protected BaseDataProvider(Task<RawMeasurement> measurementTask) {
            if (measurementTask is null) {
                throw new ArgumentNullException(nameof(measurementTask));
            }

            _spectraTask = Task.Run<IList<RawSpectrum>>(async () =>
            {
                var measurement = await measurementTask.ConfigureAwait(false);
                foreach (var s in measurement.SpectrumList) {
                    if (s.MsLevel == 0) {
                        s.MsLevel = 1;
                    }
                }
                return measurement.SpectrumList;
            });
        }

        protected static async Task<RawMeasurement> LoadMeasurementAsync(AnalysisFileBean file, bool isProfile, bool isImagingMs, bool isGuiProcess, int retry, CancellationToken token) {
            return await Task.Run(() => DataAccess.LoadMeasurement(file, isImagingMs, isGuiProcess, retry, 5000, isProfile), token);
        }

        protected static RawMeasurement LoadMeasurement(AnalysisFileBean file, bool isProfile, bool isImagingMs, bool isGuiProcess, int retry) {
            return DataAccess.LoadMeasurement(file, isImagingMs, isGuiProcess, retry, 5000, isProfile);
        }

        protected static IEnumerable<RawSpectrum> FilterByScanTime(IEnumerable<RawSpectrum> spectrums, double timeBegin, double timeEnd) {
            return spectrums.Where(spec => timeBegin <= spec.ScanStartTime && spec.ScanStartTime <= timeEnd);
        }

        public virtual ReadOnlyCollection<RawSpectrum> LoadMs1Spectrums() {
            return LoadMs1SpectrumsAsync().Result;
        }

        public virtual ReadOnlyCollection<RawSpectrum> LoadMsNSpectrums(int level) {
            return LoadMsNSpectrumsAsync(level).Result;
        }

        public virtual ReadOnlyCollection<RawSpectrum> LoadMsSpectrums() {
            return LoadMsSpectrumsAsync().Result;
        }

        public async Task<ReadOnlyCollection<RawSpectrum>> LoadMsSpectrumsAsync(CancellationToken token = default) {
            var spectra = _spectraTask.IsCompleted
                ? _spectraTask.Result
                : await _spectraTask.ConfigureAwait(false);
            return new ReadOnlyCollection<RawSpectrum>(spectra);
        }

        public Task<ReadOnlyCollection<RawSpectrum>> LoadMs1SpectrumsAsync(CancellationToken token = default) {
            return LoadMsNSpectrumsAsync(1, token);
        }

        private ConcurrentDictionary<int, Lazy<Task<ReadOnlyCollection<RawSpectrum>>>> cache = new ConcurrentDictionary<int, Lazy<Task<ReadOnlyCollection<RawSpectrum>>>>();
        public Task<ReadOnlyCollection<RawSpectrum>> LoadMsNSpectrumsAsync(int level, CancellationToken token = default) {
            return cache.GetOrAdd(level,
                i => new Lazy<Task<ReadOnlyCollection<RawSpectrum>>>(async () => 
                {
                    var spectra = _spectraTask.IsCompleted
                        ? _spectraTask.Result
                        : await _spectraTask.ConfigureAwait(false);
                    return spectra.Where(spectrum => spectrum.MsLevel == level).ToList().AsReadOnly();
                })).Value;
        }

        public List<double> LoadCollisionEnergyTargets() {
            return LoadMsSpectrums().Select(s => s.CollisionEnergy).Distinct().ToList();
        }
    }
}
