using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Utility;
using CompMs.Common.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Loader
{
    internal sealed class AlignmentPeaksSpectraLoader
    {
        private readonly Dictionary<AnalysisFileBeanModel, IMsSpectrumLoader<AlignmentSpotPropertyModel>> _loaders;

        public AlignmentPeaksSpectraLoader(AnalysisFileBeanModelCollection files) {
            var dictionary = files.AnalysisFiles.ToDictionary(file => file, file => (IMsSpectrumLoader<AlignmentSpotPropertyModel>)new MsDecSpectrumFromFileLoader(file));
            _loaders = dictionary;
        }

        public IObservable<List<SpectrumPeak>> GetObservableSpectrum(AnalysisFileBeanModel file, IObservable<AlignmentSpotPropertyModel?> target) {
            return target.DefaultIfNull(_loaders[file].LoadSpectrumAsObservable, Observable.Return(new List<SpectrumPeak>(0))).SelectSwitch(x => x!);
        }

        public Task<List<SpectrumPeak>[]> GetCurrentSpectraAsync(IEnumerable<AnalysisFileBeanModel> files, AlignmentSpotPropertyModel target) {
            if (target is null) {
                return Task.FromResult(files.Select(_ => new List<SpectrumPeak>(0)).ToArray());
            }
            return Task.WhenAll(files.Select(f => _loaders[f].LoadSpectrumAsObservable(target).FirstAsync().ToTask()).ToArray());
        }
    }
}
