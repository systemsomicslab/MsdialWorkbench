using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CompMs.Common.Components
{
    public class MoleculeMsReferenceCollection : Collection<MoleculeMsReference>
    {
        private int _currentID = 0;

        public MoleculeMsReferenceCollection() {

        }

        public MoleculeMsReferenceCollection(IList<MoleculeMsReference> list)
            : base(list) {

        }

        protected override void InsertItem(int index, MoleculeMsReference item) {
            base.InsertItem(index, item);
            if (item.ScanID >= _currentID) {
                _currentID = item.ScanID + 1;
            }
        }

        public int GetNextID() {
            return _currentID;
        }
    }
}
