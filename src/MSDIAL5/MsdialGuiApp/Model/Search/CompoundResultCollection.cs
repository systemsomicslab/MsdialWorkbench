using CompMs.CommonMVVM;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.App.Msdial.Model.Search
{
    public class CompoundResultCollection : BindableBase
    {
        public IReadOnlyList<ICompoundResult>? Results {
            get => results;
            set => SetProperty(ref results, value);
        }
        private IReadOnlyList<ICompoundResult>? results;

        public IList ResultsView => (results as IList) ?? results.ToArray();
    }
}
