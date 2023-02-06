/* Copyright (C) 2005-2007  Egon Willighagen <egonw@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 *  This library is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public
 *  License as published by the Free Software Foundation; either
 *  version 2.1 of the License, or (at your option) any later version.
 *
 *  This library is distributed in the hope that it will be useful,
 *  but WITHOUT Any WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 *  Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public
 *  License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.AtomTypes;
using NCDK.IO.Formats;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace NCDK.IO
{
    /// <summary>
    /// An output Writer that writes molecular data into the
    /// <see href="http://www.tripos.com/data/support/mol2.pdf">Tripos Mol2 format</see>.
    /// Writes the atoms and the bonds only at this moment.
    /// </summary>
    // @cdk.module io
    // @cdk.iooptions
    // @author     Egon Willighagen
    public class Mol2Writer : DefaultChemObjectWriter
    {
        private TextWriter writer;
        private SybylAtomTypeMatcher matcher;

        /// <summary>
        /// Constructs a new Mol2 writer.
        /// <param name="output">the stream to write the Mol2 file to.</param>
        /// </summary>
        public Mol2Writer(TextWriter output)
        {
            writer = output;
        }

        public Mol2Writer(Stream output)
            : this(new StreamWriter(output))
        { }

        public override IResourceFormat Format => Mol2Format.Instance;

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    writer.Dispose();
                }

                writer = null;

                disposedValue = true;
                base.Dispose(disposing);
            }
        }
        #endregion

        public override bool Accepts(Type type)
        {
            if (typeof(IAtomContainer).IsAssignableFrom(type)) return true;
            return false;
        }

        public override void Write(IChemObject obj)
        {
            if (obj is IAtomContainer)
            {
                try
                {
                    WriteMolecule((IAtomContainer)obj);
                }
                catch (Exception ex)
                {
                    throw new CDKException("Error while writing Mol2 file: " + ex.Message, ex);
                }
            }
            else
            {
                throw new CDKException("Mol2Writer only supports output of Molecule classes.");
            }
        }

        /// <summary>
        /// Writes a single frame in XYZ format to the Writer.
        /// </summary>
        /// <param name="mol">the Molecule to write</param>
        /// <exception cref="IOException">if there is an error during writing</exception>
        public void WriteMolecule(IAtomContainer mol)
        {
            matcher = SybylAtomTypeMatcher.GetInstance();
            try
            {
                Debug.WriteLine("Writing header...");
                if (mol.Title != null)
                {
                    writer.Write("#        Name: " + mol.Title);
                    writer.Write('\n');
                }
                // FIXME: add other types of meta data
                writer.Write('\n');

                // @<TRIPOS>MOLECULE benzene 12 12 1 0 0 SMALL NO_CHARGES
                
                Debug.WriteLine("Writing molecule block...");
                writer.Write("@<TRIPOS>MOLECULE");
                writer.Write('\n');
                if (mol.Id == null)
                {
                    writer.Write("CDKMolecule");
                }
                else
                {
                    writer.Write(mol.Id);
                }
                writer.Write('\n');
                writer.Write(mol.Atoms.Count + " " + mol.Bonds.Count); // that's the minimum amount of info required the format
                writer.Write('\n');
                writer.Write("SMALL"); // no biopolymer
                writer.Write('\n');
                writer.Write("NO CHARGES"); // other options include Gasteiger charges
                writer.Write('\n');

                // @<TRIPOS>ATOM 1 C1 1.207 2.091 0.000 C.ar 1 BENZENE 0.000 2 C2
                // 2.414 1.394 0.000 C.ar 1 BENZENE 0.000 3 C3 2.414 0.000 0.000
                // C.ar 1 BENZENE 0.000 4 C4 1.207 -0.697 0.000 C.ar 1 BENZENE 0.000
                // 5 C5 0.000 0.000 0.000 C.ar 1 BENZENE 0.000 6 C6 0.000 1.394
                // 0.000 C.ar 1 BENZENE 0.000 7 H1 1.207 3.175 0.000 H 1 BENZENE
                // 0.000 8 H2 3.353 1.936 0.000 H 1 BENZENE 0.000 9 H3 3.353 -0.542
                // 0.000 H 1 BENZENE 0.000 10 H4 1.207 -1.781 0.000 H 1 BENZENE
                // 0.000 11 H5 -0.939 -0.542 0.000 H 1 BENZENE 0.000 12 H6 -0.939
                // 1.936 0.000 H 1 BENZENE 0.000

                // write atom block
                Debug.WriteLine("Writing atom block...");
                writer.Write("@<TRIPOS>ATOM");
                writer.Write('\n');
                for (int i = 0; i < mol.Atoms.Count; i++)
                {
                    IAtom atom = mol.Atoms[i];
                    writer.Write((i + 1) + " " + atom.Symbol + (mol.Atoms.IndexOf(atom) + 1) + " ");
                    if (atom.Point3D != null)
                    {
                        writer.Write(atom.Point3D.Value.X.ToString("F3", NumberFormatInfo.InvariantInfo) + " ");
                        writer.Write(atom.Point3D.Value.Y.ToString("F3", NumberFormatInfo.InvariantInfo) + " ");
                        writer.Write(atom.Point3D.Value.Z.ToString("F3", NumberFormatInfo.InvariantInfo) + " ");
                    }
                    else if (atom.Point2D != null)
                    {
                        writer.Write(atom.Point2D.Value.X.ToString("F3", NumberFormatInfo.InvariantInfo) + " ");
                        writer.Write(atom.Point2D.Value.Y.ToString("F3", NumberFormatInfo.InvariantInfo) + " ");
                        writer.Write(" 0.000 ");
                    }
                    else
                    {
                        writer.Write("0.000 0.000 0.000 ");
                    }
                    IAtomType sybylType = null;
                    try
                    {
                        sybylType = matcher.FindMatchingAtomType(mol, atom);
                    }
                    catch (CDKException e)
                    {
                        Console.Error.WriteLine(e.StackTrace);
                    }
                    if (sybylType != null)
                    {
                        writer.Write(sybylType.AtomTypeName);
                    }
                    else
                    {
                        writer.Write(atom.Symbol);
                    }
                    writer.Write('\n');
                }

                // @<TRIPOS>BOND 1 1 2 ar 2 1 6 ar 3 2 3 ar 4 3 4 ar 5 4 5 ar 6 5 6
                // ar 7 1 7 1 8 2 8 1 9 3 9 1 10 4 10 1 11 5 11 1 12 6 12 1

                // write bond block
                Debug.WriteLine("Writing bond block...");
                writer.Write("@<TRIPOS>BOND");
                writer.Write('\n');

                int counter = 0;
                foreach (var bond in mol.Bonds)
                {
                    string sybylBondOrder = "-1";
                    switch (bond.Order)
                    {
                        case BondOrder.Single:
                            sybylBondOrder = "1";
                            break;
                        case BondOrder.Double:
                            sybylBondOrder = "2";
                            break;
                        case BondOrder.Triple:
                            sybylBondOrder = "3";
                            break;
                    }
                    if (bond.IsAromatic)
                        sybylBondOrder = "ar";

                    // we need to check the atom types to see if we have an amide bond
                    // and we're assuming a 2-centered bond
                    IAtom bondAtom1 = bond.Begin;
                    IAtom bondAtom2 = bond.End;
                    try
                    {
                        IAtomType bondAtom1Type = matcher.FindMatchingAtomType(mol, bondAtom1);
                        IAtomType bondAtom2Type = matcher.FindMatchingAtomType(mol, bondAtom2);
                        if (bondAtom1Type != null && bondAtom2Type != null
                         && ((bondAtom1Type.AtomTypeName.Equals("N.am", StringComparison.Ordinal) && bondAtom2Type.AtomTypeName.Equals("C.2", StringComparison.Ordinal))
                          || (bondAtom2Type.AtomTypeName.Equals("N.am", StringComparison.Ordinal) && bondAtom1Type.AtomTypeName.Equals("C.2", StringComparison.Ordinal))))
                        {
                            sybylBondOrder = "am";
                        }
                    }
                    catch (CDKException e)
                    {
                        Console.Error.WriteLine(e.StackTrace);
                    }

                    writer.Write($"{counter + 1} {mol.Atoms.IndexOf(bond.Begin) + 1} {mol.Atoms.IndexOf(bond.End) + 1} {sybylBondOrder}");
                    writer.Write('\n');
                    counter++;
                }

            }
            catch (IOException)
            {
                throw;
            }
        }
    }
}
