using CompMs.Common.Interfaces;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.Common.Components {
    [MessagePackObject]
    public class FastaProperty : IEquatable<FastaProperty>, IFastaProperty {
        [Key(0)]
        public int Index { get; set; }
        [Key(1)]
        public string Header { get; set; }
        [Key(2)]
        public string Sequence { get; set; }
        [Key(3)]
        public string Description { get; set; }

        [Key(4)]
        public string DB { get; set; }
        [Key(5)]
        public string UniqueIdentifier { get; set; }
        [Key(6)]
        public string EntryName { get; set; }
        [Key(7)]
        public string ProteinName { get; set; }
        [Key(8)]
        public string OrganismName { get; set; }
        [Key(9)]
        public string OrganismIdentifier { get; set; }
        [Key(10)]
        public string GeneName { get; set; }
        [Key(11)]
        public string ProteinExistence { get; set; }
        [Key(12)]
        public string SequenceVersion { get; set; }

        [Key(13)]
        public bool IsValidated { get; set; } // no X(any), no *(stop codon), no -(gap)
        [Key(14)]
        public bool IsDecoy { get; set; }

        public FastaProperty Clone() {
            return (FastaProperty)MemberwiseClone();
        }

        public bool Equals(FastaProperty other) {

            //Check whether the compared object is null.
            if (Object.ReferenceEquals(other, null)) return false;

            //Check whether the compared object references the same data.
            if (Object.ReferenceEquals(this, other)) return true;

            //Check whether the products' properties are equal.
            return UniqueIdentifier.Equals(other.UniqueIdentifier);
        }

        // If Equals() returns true for a pair of objects
        // then GetHashCode() must return the same value for these objects.

        public override int GetHashCode() {

            //Get hash code for the Code field.
            int hashProductCode = UniqueIdentifier.GetHashCode();

            //Calculate the hash code for the product.
            return hashProductCode;
        }
    }
}
