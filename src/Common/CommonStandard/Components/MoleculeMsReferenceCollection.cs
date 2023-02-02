using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CompMs.Common.Components
{
    public class MoleculeMsReferenceCollection : Collection<MoleculeMsReference>
    {
        public MoleculeMsReferenceCollection() {

        }

        public MoleculeMsReferenceCollection(IList<MoleculeMsReference> list)
            : base(list) {

        }
    }
}
