using Reactive.Bindings;
using System;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Model.Loader;

interface IMultiMsmsSpectrumLoader<T> : IMsSpectrumLoader<T> {
    ReactivePropertySlim<MsSelectionItem?> Ms2IdSelector { get; }
    IObservable<List<MsSelectionItem>> Ms2List { get; }
}
