using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using System.Collections.Generic;

namespace CompMs.App.Msdial.ViewModel.Statistics
{
    internal sealed class NormalizationInternalStandardSetViewModel : ViewModelBase
    {
        public IList<AlignmentSpotPropertyModel> Spots { get; }

        public ReactiveCommand ApplyCommand { get; }
        public ReactiveCommand CancelCommand { get; }
    }
}
