using System;

namespace NCDK.FaulonSignatures
{
    public class InvariantInt : IComparable<InvariantInt>
    {
        public int invariant;
        public int index;

        public InvariantInt(int invariant, int index)
        {
            this.invariant = invariant;
            this.index = index;
        }

        public int CompareTo(InvariantInt o)
        {
            if (this.invariant < o.invariant)
            {
                return -1;
            }
            else if (this.invariant > o.invariant)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        public override string ToString()
        {
            return invariant + "/" + index;
        }
    }
}
