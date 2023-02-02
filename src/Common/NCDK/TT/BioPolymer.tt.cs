



// .NET Framework port by Kazuya Ujihara
// Copyright (C) 2016-2017  Kazuya Ujihara <ujihara.kazuya@gmail.com>

/* Copyright (C) 1997-2007  Edgar Luttmann <edgar@uni-paderborn.de>
 *
 *  Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 * All we ask is that proper credit is given for our work, which includes
 * - but is not limited to - adding the above copyright notice to the beginning
 * of your source code files, and to any copyright notice that you may distribute
 * with programs based on this work.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using System;
using System.Collections.Generic;
using System.Linq;

namespace NCDK.Default
{
    public class BioPolymer
        : Polymer, IBioPolymer
    {
        private Dictionary<string, IStrand> strands;

        public BioPolymer()
            : base()
        {
            strands = new Dictionary<string, IStrand>();
        }

        /// <summary>
        /// Adds the atom oAtom to a specified Strand, whereas the Monomer is unspecified. Hence
        /// the atom will be added to a Monomer of type Unknown in the specified Strand.
        /// </summary>
        /// <param name="oAtom">The atom to add</param>
        /// <param name="oStrand">The strand the atom belongs to</param>
        public void AddAtom(IAtom oAtom, IStrand oStrand)
        {
            int atomCount = base.Atoms.Count;

            // Add atom to AtomContainer
            base.Atoms.Add(oAtom);

            if (atomCount != base.Atoms.Count && oStrand != null)
            { // Maybe better to throw null pointer exception here, so user realises that
              // Strand == null and Atom only gets added to this BioPolymer, but not to a Strand.
                oStrand.AddAtom(oAtom);
                if (!strands.ContainsKey(oStrand.StrandName))
                {
                    strands.Add(oStrand.StrandName, oStrand);
                }
            }
            
            // NotifyChanged() is called by AddAtom in AtomContainer
        }

        public void AddAtom(IAtom oAtom, IMonomer oMonomer, IStrand oStrand)
        {
            int atomCount = Atoms.Count;

            // Add atom to AtomContainer
            base.Atoms.Add(oAtom);

            if (atomCount != base.Atoms.Count // OK, super did not yet contain the atom
                                              // Add atom to Strand (also adds the atom to the monomer).
             && oStrand != null)
            {
                oStrand.AddAtom(oAtom, oMonomer); // Same problem as above: better to throw nullpointer exception?
                if (!strands.ContainsKey(oStrand.StrandName))
                {
                    strands.Add(oStrand.StrandName, oStrand);
                }
            }

            // The reasoning above is: All Monomers have to belong to a Strand and
            // all atoms belonging to strands have to belong to a Monomer => ?
            // oMonomer != null and oStrand != null, oAtom is added to BioPolymer
            // and to oMonomer in oStrand ? oMonomer == null and oStrand != null,
            // oAtom is added to BioPolymer and default Monomer in oStrand ?
            // oMonomer != null and oStrand == null, oAtom is added to BioPolymer,
            // but not to a Monomer or Strand (especially good to maybe throw
            // exception in this case) ? oMonomer == null and oStrand == null, oAtom
            // is added to BioPolymer, but not to a Monomer or Strand
        }

        public IMonomer GetMonomer(string monomerName, string strandName)
        {
            if (!strands.TryGetValue(strandName, out IStrand strand))
                return null;
            return strand.GetMonomer(monomerName);
        }

        public override IEnumerable<KeyValuePair<string, IMonomer>> GetMonomerMap()
        {
            if (strands.Count == 0)
            {
                foreach (var pair in base.GetMonomerMap())
                    yield return pair;
                yield break;
            }

            foreach (var strand in strands)
                foreach (var pair in strand.Value.GetMonomerMap())
                    yield return pair;
            yield break;
        }

        public override IEnumerable<string> GetMonomerNames()
        {
            return LazyGetMonomerNames().Distinct();
        }

        private IEnumerable<string> LazyGetMonomerNames()
        {
            if (strands.Count == 0)
            {
                foreach (var name in base.GetMonomerNames())
                    yield return name;
                yield break;
            }

            foreach (var strand in strands)
                foreach (var name in strand.Value.GetMonomerNames())
                    yield return name;
            yield break;
        }

        public int GetStrandCount() => strands.Count;

        public IStrand GetStrand(string cName)
        {
            if (strands.TryGetValue(cName, out IStrand strand))
                return strand;
            return null;
        }

        public IEnumerable<string> GetStrandNames() => strands.Keys;

        public void RemoveStrand(string name)
        {
            if (strands.ContainsKey(name))
            {
                var strand = strands[name];
                this.Remove(strand);
                strands.Remove(name);
            }
        }

        public IReadOnlyDictionary<string, IStrand> GetStrandMap()
        {
            return strands;
        }

        public override ICDKObject Clone(CDKObjectMap map)
        {
            var clone = (BioPolymer)base.Clone(map);
            clone.strands = new Dictionary<string, IStrand>();
            foreach (var strandPair in strands)
            {
                string name = strandPair.Key;
                IStrand original = strandPair.Value;
                IStrand cloned = (IStrand)original.Clone(map);
                clone.strands.Add(name, cloned);
            }
            return clone;
        }
    }
}
namespace NCDK.Silent
{
    public class BioPolymer
        : Polymer, IBioPolymer
    {
        private Dictionary<string, IStrand> strands;

        public BioPolymer()
            : base()
        {
            strands = new Dictionary<string, IStrand>();
        }

        /// <summary>
        /// Adds the atom oAtom to a specified Strand, whereas the Monomer is unspecified. Hence
        /// the atom will be added to a Monomer of type Unknown in the specified Strand.
        /// </summary>
        /// <param name="oAtom">The atom to add</param>
        /// <param name="oStrand">The strand the atom belongs to</param>
        public void AddAtom(IAtom oAtom, IStrand oStrand)
        {
            int atomCount = base.Atoms.Count;

            // Add atom to AtomContainer
            base.Atoms.Add(oAtom);

            if (atomCount != base.Atoms.Count && oStrand != null)
            { // Maybe better to throw null pointer exception here, so user realises that
              // Strand == null and Atom only gets added to this BioPolymer, but not to a Strand.
                oStrand.AddAtom(oAtom);
                if (!strands.ContainsKey(oStrand.StrandName))
                {
                    strands.Add(oStrand.StrandName, oStrand);
                }
            }
            
            // NotifyChanged() is called by AddAtom in AtomContainer
        }

        public void AddAtom(IAtom oAtom, IMonomer oMonomer, IStrand oStrand)
        {
            int atomCount = Atoms.Count;

            // Add atom to AtomContainer
            base.Atoms.Add(oAtom);

            if (atomCount != base.Atoms.Count // OK, super did not yet contain the atom
                                              // Add atom to Strand (also adds the atom to the monomer).
             && oStrand != null)
            {
                oStrand.AddAtom(oAtom, oMonomer); // Same problem as above: better to throw nullpointer exception?
                if (!strands.ContainsKey(oStrand.StrandName))
                {
                    strands.Add(oStrand.StrandName, oStrand);
                }
            }

            // The reasoning above is: All Monomers have to belong to a Strand and
            // all atoms belonging to strands have to belong to a Monomer => ?
            // oMonomer != null and oStrand != null, oAtom is added to BioPolymer
            // and to oMonomer in oStrand ? oMonomer == null and oStrand != null,
            // oAtom is added to BioPolymer and default Monomer in oStrand ?
            // oMonomer != null and oStrand == null, oAtom is added to BioPolymer,
            // but not to a Monomer or Strand (especially good to maybe throw
            // exception in this case) ? oMonomer == null and oStrand == null, oAtom
            // is added to BioPolymer, but not to a Monomer or Strand
        }

        public IMonomer GetMonomer(string monomerName, string strandName)
        {
            if (!strands.TryGetValue(strandName, out IStrand strand))
                return null;
            return strand.GetMonomer(monomerName);
        }

        public override IEnumerable<KeyValuePair<string, IMonomer>> GetMonomerMap()
        {
            if (strands.Count == 0)
            {
                foreach (var pair in base.GetMonomerMap())
                    yield return pair;
                yield break;
            }

            foreach (var strand in strands)
                foreach (var pair in strand.Value.GetMonomerMap())
                    yield return pair;
            yield break;
        }

        public override IEnumerable<string> GetMonomerNames()
        {
            return LazyGetMonomerNames().Distinct();
        }

        private IEnumerable<string> LazyGetMonomerNames()
        {
            if (strands.Count == 0)
            {
                foreach (var name in base.GetMonomerNames())
                    yield return name;
                yield break;
            }

            foreach (var strand in strands)
                foreach (var name in strand.Value.GetMonomerNames())
                    yield return name;
            yield break;
        }

        public int GetStrandCount() => strands.Count;

        public IStrand GetStrand(string cName)
        {
            if (strands.TryGetValue(cName, out IStrand strand))
                return strand;
            return null;
        }

        public IEnumerable<string> GetStrandNames() => strands.Keys;

        public void RemoveStrand(string name)
        {
            if (strands.ContainsKey(name))
            {
                var strand = strands[name];
                this.Remove(strand);
                strands.Remove(name);
            }
        }

        public IReadOnlyDictionary<string, IStrand> GetStrandMap()
        {
            return strands;
        }

        public override ICDKObject Clone(CDKObjectMap map)
        {
            var clone = (BioPolymer)base.Clone(map);
            clone.strands = new Dictionary<string, IStrand>();
            foreach (var strandPair in strands)
            {
                string name = strandPair.Key;
                IStrand original = strandPair.Value;
                IStrand cloned = (IStrand)original.Clone(map);
                clone.strands.Add(name, cloned);
            }
            return clone;
        }
    }
}
