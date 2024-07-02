using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Interfaces;
using System;
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

        IObservable<IMSScanProperty?> IMsSpectrumLoader<AlignmentSpotPropertyModel>.LoadScanAsObservable(AlignmentSpotPropertyModel target) {
            return LoadCore(target);
        }

        private IObservable<IMSScanProperty?> LoadCore(AlignmentSpotPropertyModel target) {
            if (target is null) {
                throw new ArgumentNullException(nameof(target));
            }
            IObservable<ReadOnlyCollection<AlignmentChromPeakFeatureModel>?> props = target.AlignedPeakPropertiesModelProperty;
            var task = target.AlignedPeakPropertiesModelProperty.ToTask();
            if (target.AlignedPeakPropertiesModelProperty.Value is null) {
                props = Observable.FromAsync(() => task);
            }
            return props.Select(props_ => {
                var prop = props_.FirstOrDefault(p => p.FileID == _file.AnalysisFileId);
                if (prop is null || prop.MSDecResultID < 0) {
                    return null;
                }

                return _file.MSDecLoader.LoadMSDecResult(prop.MSDecResultID);
            });
        }
    }
}