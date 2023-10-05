using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Algorithm.Annotation;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Loader
{
    internal sealed class ReferenceSpectrumLoader<T> : IMsSpectrumLoader<MsScanMatchResult> where T: IMSScanProperty
    {
        private readonly IMatchResultRefer<T, MsScanMatchResult> _refer;

        public ReferenceSpectrumLoader(IMatchResultRefer<T, MsScanMatchResult> refer) {
            _refer = refer ?? throw new ArgumentNullException(nameof(refer));
        }

        IObservable<List<SpectrumPeak>> IMsSpectrumLoader<MsScanMatchResult>.LoadSpectrumAsObservable(MsScanMatchResult target) {
            return Observable.Return(LoadSpectrumCore(target));
        }

        private List<SpectrumPeak> LoadSpectrumCore(MsScanMatchResult target) {
            if (target is null) {
                throw new ArgumentNullException(nameof(target));
            }
            var reference = _refer.Refer(target);
            if (reference != null) {
                return reference.Spectrum;
            }
            return new List<SpectrumPeak>();
        }
    }
}
