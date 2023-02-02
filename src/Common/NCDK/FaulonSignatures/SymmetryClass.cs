using System;
using System.Collections;
using System.Collections.Generic;

namespace NCDK.FaulonSignatures
{
    /// <summary>
    /// A collection of vertex indices with the same canonical signature string.
    /// </summary>
    // @author maclean
    public class SymmetryClass : IComparable<SymmetryClass>, IEnumerable<int>
    {
        /// <summary>
        /// The signature string that the vertices all share
        /// </summary>
        private readonly string signatureString;

        /// <summary>
        /// The set of vertex indices that have this signature string
        /// </summary>
        private readonly SortedSet<int> vertexIndices;

        /// <summary>
        /// Make a symmetry class for the signature string <paramref name="signatureString"/>.
        /// </summary>
        /// <param name="signatureString">the signature string for this symmetry class</param>
        public SymmetryClass(string signatureString)
        {
            this.signatureString = signatureString;
            this.vertexIndices = new SortedSet<int>();
        }

        public IEnumerator<int> GetEnumerator()
        {
            return this.vertexIndices.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public int Count => vertexIndices.Count;

        public string GetSignatureString()
        {
            return this.signatureString;
        }

        /// <summary>
        /// Check that the symmetry class' string is the same as the supplied string.
        /// </summary>
        /// <param name="otherSignatureString">the string to check</param>
        /// <returns>true if the strings are equal</returns>
        public bool HasSignature(string otherSignatureString)
        {
            return string.Equals(this.signatureString, otherSignatureString, StringComparison.Ordinal);
        }

        /// <summary>
        /// Add a vertex index to the list.
        /// </summary>
        /// <param name="vertexIndex">the vertex index to add</param>
        public void AddIndex(int vertexIndex)
        {
            this.vertexIndices.Add(vertexIndex);
        }

        /// <summary>
        /// If the vertex indexed by <paramref name="vertexIndex"/> is in the symmetry 
        /// class then return the smaller of it and the lowest element. If it is not
        /// in the symmetry class, return -1.
        /// </summary>
        /// <param name="vertexIndex"></param>
        /// <param name="used"></param>
        /// <returns></returns>
        public int GetMinimal(int vertexIndex, List<int> used)
        {
            int min = -1;
            foreach (var classIndex in this.vertexIndices)
            {
                if (classIndex == vertexIndex)
                {
                    if (min == -1)
                    {
                        return vertexIndex;
                    }
                    else
                    {
                        return min;
                    }
                }
                else
                {
                    if (used.Contains(classIndex))
                    {
                        continue;
                    }
                    else
                    {
                        min = classIndex;
                    }
                }
            }

            // the vertexIndex is not in the symmetry class
            return -1;
        }

        /// <seealso cref="IComparable.CompareTo(object)"/>
        public int CompareTo(SymmetryClass o)
        {
            return string.Compare(this.signatureString, o.signatureString, StringComparison.Ordinal);
        }

        public override string ToString()
        {
            return this.signatureString + " " + this.vertexIndices;
        }
    }
}
