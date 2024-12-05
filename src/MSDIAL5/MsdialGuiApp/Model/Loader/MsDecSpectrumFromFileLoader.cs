using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Loader
{
    internal sealed class MsDecSpectrumFromFileLoader : IMsSpectrumLoader<AlignmentSpotPropertyModel>, IMsSpectrumLoader<ChromatogramPeakFeatureModel?>
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
                var rep = ((IAnnotatedObject)prop).MatchResults.Representative;
                if (!rep.IsUnknown && _file.GetMSDecLoader(rep.CollisionEnergy) is { } loader) {
                    return loader.LoadMSDecResult(prop.MSDecResultID);
                }
                return _file.MSDecLoader.LoadMSDecResult(prop.MSDecResultID);
            });
        }

        IObservable<IMSScanProperty?> IMsSpectrumLoader<ChromatogramPeakFeatureModel?>.LoadScanAsObservable(ChromatogramPeakFeatureModel? target) {
            return Observable.Return(LoadCore(target));
        }

        public MSDecResult? LoadMSDecResult(ChromatogramPeakFeatureModel? target) {
            return LoadCore(target);
        }

        private MSDecResult? LoadCore(ChromatogramPeakFeatureModel? target) {
            if (target is null || target.MSDecResultIDUsedForAnnotation < 0) {
                return null;
            }
            var rep = ((IAnnotatedObject)target).MatchResults.Representative;
            if (!rep.IsUnknown && _file.GetMSDecLoader(rep.CollisionEnergy) is { } loader) {
                return loader.LoadMSDecResult(target.MSDecResultIDUsedForAnnotation);
            }
            return _file.MSDecLoader.LoadMSDecResult(target.MSDecResultIDUsedForAnnotation);
        }
    }
}