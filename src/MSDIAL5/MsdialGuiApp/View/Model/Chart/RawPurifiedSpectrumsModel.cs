using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings.Extensions;
using System;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Chart
{
    internal sealed class RawPurifiedSpectrumsModel : DisposableModelBase {

        public RawPurifiedSpectrumsModel(SingleSpectrumModel rawSpectrumModel, SingleSpectrumModel decSpectrumModel)
        {
            RawSpectrumModel = rawSpectrumModel ?? throw new ArgumentNullException(nameof(rawSpectrumModel));
            DecSpectrumModel = decSpectrumModel ?? throw new ArgumentNullException(nameof(decSpectrumModel));

            HorizontalAxis = new[]
            {
                rawSpectrumModel.GetHorizontalRange(),
                decSpectrumModel.GetHorizontalRange(),
            }.CombineLatest(ranges => (ranges.Min(range => range.Item1), ranges.Max(range => range.Item2)))
            .ToReactiveContinuousAxisManager(new ConstantMargin(40))
            .AddTo(Disposables);
        }

        public SingleSpectrumModel RawSpectrumModel { get; }
        public SingleSpectrumModel DecSpectrumModel { get; }
        public ReactiveAxisManager<double> HorizontalAxis { get; }
    }
}
