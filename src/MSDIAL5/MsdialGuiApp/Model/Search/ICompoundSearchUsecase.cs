using CompMs.Common.Parameter;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace CompMs.App.Msdial.Model.Search
{
    internal interface ICompoundSearchUsecase<out TCompoundResult, in TTarget> : INotifyPropertyChanged
    {
        IList SearchMethods { get; }

        object? SearchMethod { get; set; }

        MsRefSearchParameterBase? SearchParameter { get; }

        IReadOnlyList<TCompoundResult> Search(TTarget target);
    }
}
