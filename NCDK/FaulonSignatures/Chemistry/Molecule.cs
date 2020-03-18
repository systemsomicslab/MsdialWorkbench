using System;
using System.Collections.Generic;
using System.Text;

namespace NCDK.FaulonSignatures.Chemistry
{
    /// <summary>
    /// A trivial test molecule class, to show how to implement signatures for chemistry libraries.
    /// </summary>
    // @author maclean
    public class Molecule
    {
        public enum BondOrder { None, Single, Double, Triple, Aromatic }

        public class Atom
        {
            public int index;
            public string symbol;

            public Atom(int index, string symbol)
            {
                this.index = index;
                this.symbol = symbol;
            }

            public Atom(Atom other)
            {
                this.index = other.index;
                this.symbol = other.symbol;
            }

            public bool Equals(Atom other)
            {
                return this.index == other.index
                    && string.Equals(this.symbol, other.symbol, StringComparison.Ordinal);
            }

            public override string ToString()
            {
                return this.index + this.symbol;
            }
        }

        public class Bond : IComparable<Bond>
        {
            public Atom a;
            public Atom b;
            public BondOrder order;

            public Bond(Atom a, Atom b, BondOrder order)
            {
                this.a = a;
                this.b = b;
                this.order = order;
            }

            public Bond(Bond other)
            {
                this.a = new Atom(other.a);
                this.b = new Atom(other.b);
                this.order = other.order;
            }

            public int GetConnected(int i)
            {
                if (a.index == i)
                {
                    return this.b.index;
                }
                else if (b.index == i)
                {
                    return this.a.index;
                }
                else
                {
                    return -1;
                }
            }

            public override bool Equals(object o)
            {
                Bond other = (Bond)o;
                return (this.a.Equals(other.a) && this.b.Equals(other.b))
                    || (this.a.Equals(other.b) && this.b.Equals(other.a));
            }

            public override int GetHashCode()
            {
                return a.GetHashCode() * 31 + b.GetHashCode();
            }

            public bool HasBoth(int atomIndexA, int atomIndexB)
            {
                return (this.a.index == atomIndexA && this.b.index == atomIndexB)
                    || (this.b.index == atomIndexA && this.a.index == atomIndexB);
            }

            public override string ToString()
            {
                if (a.index < b.index)
                {
                    return this.a + "-" + this.b + "(" + this.order + ")";
                }
                else
                {
                    return this.b + "-" + this.a + "(" + this.order + ")";
                }
            }

            public int CompareTo(Bond o)
            {
                int thisMin = Math.Min(this.a.index, this.b.index);
                int thisMax = Math.Max(this.a.index, this.b.index);
                int oMin = Math.Min(o.a.index, o.b.index);
                int oMax = Math.Max(o.a.index, o.b.index);
                if (thisMin < oMin)
                {
                    return -1;
                }
                else if (thisMin == oMin)
                {
                    if (thisMax < oMax)
                    {
                        return -1;
                    }
                    else if (thisMax == oMax)
                    {
                        return 0;
                    }
                    else
                    {
                        return 1;
                    }
                }
                else
                {
                    return 1;
                }
            }
        }

        private List<Atom> atoms;
        private List<Bond> bonds;
        public string name;

        public Molecule()
        {
            this.atoms = new List<Atom>();
            this.bonds = new List<Bond>();
        }

        public Molecule(string atomSymbol, int count)
            : this()
        {
            for (int i = 0; i < count; i++)
            {
                this.AddAtom(atomSymbol);
            }
        }

        public Molecule(Molecule other)
            : this()
        {
            foreach (var atom in other.atoms)
            {
                this.atoms.Add(new Atom(atom.index, atom.symbol));
            }

            foreach (var bond in other.bonds)
            {
                Atom oA = this.atoms[bond.a.index];
                Atom oB = this.atoms[bond.b.index];
                this.bonds.Add(new Bond(oA, oB, bond.order));
            }
        }

        public Molecule(Molecule other, int[] permutation)
            : this()
        {
            Atom[] permutedAtoms = new Atom[permutation.Length];
            foreach (var atom in other.atoms)
            {
                int index = permutation[atom.index];
                permutedAtoms[index] = new Atom(index, atom.symbol);
            }
            foreach (var atom in permutedAtoms)
            {
                this.atoms.Add(atom);
            }

            foreach (var bond in other.bonds)
            {
                Atom oA = this.atoms[permutation[bond.a.index]];
                Atom oB = this.atoms[permutation[bond.b.index]];
                this.bonds.Add(new Bond(oA, oB, bond.order));
            }

        }

        public int GetAtomCount()
        {
            return this.atoms.Count;
        }

        public int GetBondCount()
        {
            return this.bonds.Count;
        }

        public List<Bond> Bonds()
        {
            return bonds;
        }

        public virtual int[] GetConnected(int atomIndex)
        {
            List<int> connectedList = new List<int>();

            foreach (var bond in this.bonds)
            {
                int connectedIndex = bond.GetConnected(atomIndex);
                if (connectedIndex != -1)
                {
                    connectedList.Add(connectedIndex);
                }
            }
            int[] connected = new int[connectedList.Count];
            for (int i = 0; i < connectedList.Count; i++)
            {
                connected[i] = connectedList[i];
            }
            return connected;
        }

        public bool IsConnected(int i, int j)
        {
            foreach (var bond in bonds)
            {
                if (bond.HasBoth(i, j))
                {
                    return true;
                }
            }
            return false;
        }

        public BondOrder GetBondOrder(int atomIndex, int otherAtomIndex)
        {
            foreach (var bond in this.bonds)
            {
                if (bond.HasBoth(atomIndex, otherAtomIndex))
                {
                    return bond.order;
                }
            }
            return BondOrder.None;
        }

        public static int ConvertBondOrderToInt(BondOrder bondOrder)
        {
            switch (bondOrder)
            {
                case BondOrder.None: return 0;
                case BondOrder.Single: return 1;
                case BondOrder.Double: return 2;
                case BondOrder.Triple: return 3;
                case BondOrder.Aromatic: return 4;   // hmmm...
                default: return 1;
            }
        }

        public int GetTotalOrder(int atomIndex)
        {
            int totalOrder = 0;
            foreach (var bond in bonds)
            {
                if (bond.a.index == atomIndex || bond.b.index == atomIndex)
                {
                    totalOrder += ConvertBondOrderToInt(bond.order);
                }
            }
            return totalOrder;
        }

        public string GetSymbolFor(int atomIndex)
        {
            return this.atoms[atomIndex].symbol;
        }

        public override string ToString()
        {
            var buffer = new StringBuilder();
            foreach (var a in this.atoms)
            {
                buffer.Append(a).Append("|");
            }
            bonds.Sort();
            foreach (var b in this.bonds)
            {
                buffer.Append(b).Append("|");
            }
            return buffer.ToString();
        }

        public void AddAtom(string symbol)
        {
            int i = this.atoms.Count;
            this.AddAtom(i, symbol);
        }

        public void AddAtom(int i, string symbol)
        {
            this.atoms.Add(new Atom(i, symbol));
        }

        public void AddMultipleAtoms(int count, string symbol)
        {
            for (int i = 0; i < count; i++)
            {
                AddAtom(symbol);
            }
        }

        public void AddSingleBond(int atomNumberA, int atomNumberB)
        {
            this.AddBond(atomNumberA, atomNumberB, BondOrder.Single);
        }

        public void AddMultipleSingleBonds(int i, params int[] js)
        {
            foreach (var j in js)
            {
                AddSingleBond(i, j);
            }
        }

        public void AddBond(int atomNumberA, int atomNumberB, Molecule.BondOrder order)
        {
            Atom a = this.atoms[atomNumberA];
            Atom b = this.atoms[atomNumberB];
            this.bonds.Add(new Bond(a, b, order));
        }

        public bool Identical(Molecule other)
        {
            if (this.GetBondCount() != other.GetBondCount()) return false;
            foreach (var bond in this.bonds)
            {
                if (other.bonds.Contains(bond))
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

        public bool BondsOrdered()
        {
            for (int i = 1; i < this.bonds.Count; i++)
            {
                Bond bondA = this.bonds[i - 1];
                Bond bondB = this.bonds[i];
                int aMin = Math.Min(bondA.a.index, bondA.b.index);
                int aMax = Math.Max(bondA.a.index, bondA.b.index);
                int bMin = Math.Min(bondB.a.index, bondB.b.index);
                int bMax = Math.Max(bondB.a.index, bondB.b.index);
                if (aMin < bMin || (aMin == bMin && aMax < bMax))
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

        public string ToEdgeString()
        {
            var edgeString = new StringBuilder();
            List<Bond> listCopy = new List<Bond>();
            foreach (var bond in this.bonds)
            {
                listCopy.Add(new Bond(bond));
            }
            listCopy.Sort();
            foreach (var bond in listCopy)
            {
                if (bond.a.index < bond.b.index)
                {
                    edgeString.Append(bond.a).Append(":").Append(bond.b);
                }
                else
                {
                    edgeString.Append(bond.b).Append(":").Append(bond.a);
                }
                edgeString.Append(",");
            }
            return edgeString.ToString();
        }

        public int GetFirstInBond(int bondIndex)
        {
            return bonds[bondIndex].a.index;
        }

        public int GetSecondInBond(int bondIndex)
        {
            return bonds[bondIndex].b.index;
        }

        public BondOrder GetBondOrder(int bondIndex)
        {
            return bonds[bondIndex].order;
        }

        public int GetBondOrderAsInt(int bondIndex)
        {
            return ConvertBondOrderToInt(bonds[bondIndex].order);
        }
    }
}

