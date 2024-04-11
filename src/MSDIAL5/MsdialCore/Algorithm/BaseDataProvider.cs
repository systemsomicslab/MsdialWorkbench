using CompMs.Common.DataObj;
using CompMs.MsdialCore.DataObj;
using CompMs.RawDataHandler.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
            using (var access = new RawDataAccess(file.AnalysisFilePath, 0, isProfile, isImagingMs, isGuiProcess, file.RetentionTimeCorrectionBean.PredictedRt)) {
                for (var i = 0; i < retry; i++) {
                    var rawObj = await Task.Run(() => access.GetMeasurement(), token).ConfigureAwait(false);
                    if (rawObj != null) {
                        return rawObj;
                    }
                    Thread.Sleep(5000);
                    token.ThrowIfCancellationRequested();
                }
            }
            throw new FileLoadException($"Loading {file.AnalysisFilePath} failed.");
        }

        protected static RawMeasurement LoadMeasurement(AnalysisFileBean file, bool isProfile, bool isImagingMs, bool isGuiProcess, int retry) {
            using (var access = new RawDataAccess(file.AnalysisFilePath, 0, isProfile, isImagingMs, isGuiProcess, file.RetentionTimeCorrectionBean.PredictedRt)) {
                for (var i = 0; i < retry; i++) {
                    var rawObj = access.GetMeasurement();
                    if (rawObj != null) {
                        return rawObj;
                    }
                    Thread.Sleep(5000);
                }
            }
            throw new FileLoadException($"Loading {file.AnalysisFilePath} failed.");
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
    }
}
