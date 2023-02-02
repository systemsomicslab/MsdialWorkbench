using NCDK.Common.Collections;
using System;

namespace NCDK.FaulonSignatures
{
    public class InvariantArray : IComparable<InvariantArray>
    {
        public readonly int[] invariants;
        public readonly int originalIndex;

        public InvariantArray(int[] invariants, int originalIndex)
        {
            this.invariants = invariants;
            this.originalIndex = originalIndex;
        }

        public bool Equals(int[] other)
        {
            if (this.invariants == null || other == null) return false;
            for (int i = 0; i < this.invariants.Length; i++)
            {
                if (this.invariants[i] == other[i])
                {
                    continue;
                }
                else
                {
                    return false;
                }
            }
            return true;

        }

        public override bool Equals(object o)
        {
            if (o is InvariantArray other)
            {
                return this.Equals(other.invariants);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Arrays.GetHashCode(invariants);
        }

        public int CompareTo(InvariantArray o)
        {
            if (this.invariants == null || o.invariants == null) return 0;
            for (int i = 0; i < this.invariants.Length; i++)
            {
                if (this.invariants[i] < o.invariants[i])
                {
                    return -1;
                }
                else if (this.invariants[i] > o.invariants[i])
                {
                    return 1;
                }
            }
            return 0;
        }

        public override string ToString()
        {
            return Arrays.ToJavaString(this.invariants) + ":" + this.originalIndex;
        }
    }
}
