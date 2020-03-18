using System;

namespace NCDK.FaulonSignatures
{
    public class InvariantIntIntPair : IComparable<InvariantIntIntPair>
    {
        public int label;
        public int value;
        public int originalIndex;

        public InvariantIntIntPair(int label, int value, int originalIndex)
        {
            this.label = label;
            this.value = value;
            this.originalIndex = originalIndex;
        }

        public bool Equals(int label, int value)
        {
            return this.value == value && this.label == label;
        }

        public bool Equals(InvariantIntIntPair o)
        {
            return this.value == o.value && this.label == o.label;
        }

        public int CompareTo(InvariantIntIntPair o)
        {
            int c = (this.label == o.label) ? 0 :
                ((this.label < o.label) ? -1 : 1);
            if (c == 0)
            {
                if (this.value < o.value)
                {
                    return -1;
                }
                else if (this.value > o.value)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                return c;
            }
        }

        public int GetOriginalIndex()
        {
            return originalIndex;
        }

        public override string ToString()
        {
            return this.label + "|" + this.value + "|" + this.originalIndex;
        }
    }
}
