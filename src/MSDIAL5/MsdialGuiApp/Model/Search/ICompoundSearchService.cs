using System.Collections.Generic;
using System.ComponentModel;

namespace CompMs.App.Msdial.Model.Search
{
    internal interface ICompoundSearchService<out TCompoundResult, in TTarget> : INotifyPropertyChanged
    {
        IReadOnlyList<CompoundSearcher> CompoundSearchers { get; }

        CompoundSearcher SelectedCompoundSearcher { get; set; }

        IReadOnlyList<TCompoundResult> Search(TTarget target);
    }
}
