using System;
using System.Collections.Generic;

namespace NCDK.FaulonSignatures
{
    public class InvariantList : IComparable<InvariantList>
    {
        public List<int> invariants;
        public int originalIndex;

        public InvariantList(int originalIndex)
        {
            this.invariants = new List<int>();
            this.originalIndex = originalIndex;
        }

        public bool Equals(List<int> other)
        {
            // Check the size first. If it differs return false.
            if (!(this.invariants.Count == other.Count))
            {
                return false;
            }
            for (int i = 0; i < this.invariants.Count; i++)
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

        public override int GetHashCode()
        {
            int hash = 0;
            foreach (var e in invariants)
            {
                hash *= 31;
                hash += e.GetHashCode();
            }
            return hash;
        }

        public void Add(int i)
        {
            this.invariants.Add(i);
        }

        public void AddAll(ICollection<int> other)
        {
            this.invariants.AddRange(other);
        }

        public override bool Equals(object o)
        {
            if (o is InvariantList other)
            {
                return this.Equals(other.invariants);
            }
            return false;
        }

        public int CompareTo(InvariantList o)
        {
            int lA = this.invariants.Count;
            int lB = o.invariants.Count;
            if (lA < lB)
            {
                return -1;
            }
            else if (lA > lB)
            {
                return 1;
            }
            else
            {
                for (int i = 0; i < this.invariants.Count; i++)
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
        }

        public override string ToString()
        {
            return originalIndex + " " + invariants;
        }
    }
}
