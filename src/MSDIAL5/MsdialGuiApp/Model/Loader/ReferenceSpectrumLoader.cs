using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Algorithm.Annotation;
using System;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Loader
{
    internal sealed class ReferenceSpectrumLoader<T> : IMsSpectrumLoader<MsScanMatchResult> where T: IMSScanProperty?
    {
        private readonly IMatchResultRefer<T, MsScanMatchResult?> _refer;

        public ReferenceSpectrumLoader(IMatchResultRefer<T, MsScanMatchResult?> refer) {
            _refer = refer ?? throw new ArgumentNullException(nameof(refer));
        }

        IObservable<IMSScanProperty?> IMsSpectrumLoader<MsScanMatchResult>.LoadScanAsObservable(MsScanMatchResult target) {
            return Observable.Return(LoadScanCore(target));
        }

        private IMSScanProperty? LoadScanCore(MsScanMatchResult target) {
            if (target is null) {
                throw new ArgumentNullException(nameof(target));
            }
            var reference = _refer.Refer(target);
            if (reference != null) {
                return reference;
            }
            return null;
        }
    }
}
