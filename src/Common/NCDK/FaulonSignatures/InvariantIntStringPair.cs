using System;

namespace NCDK.FaulonSignatures
{
    public class InvariantIntStringPair : IComparable<InvariantIntStringPair>
    {
        private readonly string stringValue;
        private readonly int value;
        private readonly int originalIndex;

        public InvariantIntStringPair(string stringValue, int value, int originalIndex)
        {
            this.stringValue = stringValue;
            this.value = value;
            this.originalIndex = originalIndex;
        }

        public bool Equals(string stringValue, int value)
        {
            return this.value == value && string.Equals(this.stringValue, stringValue, StringComparison.Ordinal);
        }

        public bool Equals(InvariantIntStringPair o)
        {
            if (this.stringValue == null || o.stringValue == null) return false;
            return this.value == o.value && string.Equals(this.stringValue, o.stringValue, StringComparison.Ordinal);
        }

        public int CompareTo(InvariantIntStringPair o)
        {
            if (this.stringValue == null || o.stringValue == null) return 0;
            int c = string.CompareOrdinal(this.stringValue, o.stringValue);
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
            return this.stringValue + "|" + this.value + "|" + this.originalIndex;
        }
    }
}
