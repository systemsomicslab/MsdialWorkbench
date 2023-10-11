using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Loader
{
    internal sealed class MsDecSpectrumFromFileLoader : IMsSpectrumLoader<AlignmentSpotPropertyModel>
    {
        private readonly AnalysisFileBeanModel _file;

        public MsDecSpectrumFromFileLoader(AnalysisFileBeanModel file) {
            _file = file ?? throw new ArgumentNullException(nameof(file));
        }

        IObservable<List<SpectrumPeak>> IMsSpectrumLoader<AlignmentSpotPropertyModel>.LoadSpectrumAsObservable(AlignmentSpotPropertyModel target) {
            if (target is null) {
                return Observable.Return(new List<SpectrumPeak>(0));
            }
            IObservable<ReadOnlyCollection<AlignmentChromPeakFeatureModel>> props = target.AlignedPeakPropertiesModelProperty;
            var task = target.AlignedPeakPropertiesModelProperty.ToTask();
            if (target.AlignedPeakPropertiesModelProperty.Value is null) {
                props = Observable.FromAsync(() => task);
            }
            return props.Select(props_ => {
                var prop = props_.FirstOrDefault(p => p.FileID == _file.AnalysisFileId);
                if (prop is null || prop.MasterPeakID < 0) {
                    return new List<SpectrumPeak>(0);
                }

                return _file.MSDecLoader.LoadMSDecResult(prop.MasterPeakID).Spectrum;
            });
        }
    }
}