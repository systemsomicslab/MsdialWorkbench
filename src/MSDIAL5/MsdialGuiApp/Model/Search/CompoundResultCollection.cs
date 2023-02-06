using CompMs.CommonMVVM;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Model.Search
{
    public class CompoundResultCollection : BindableBase
    {
        public IList<ICompoundResult> Results {
            get => results;
            set => SetProperty(ref results, value);
        }
        private IList<ICompoundResult> results;
    }
}
