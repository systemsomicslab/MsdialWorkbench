/* 
 * Copyright (C) 2018  Kazuya Ujihara <ujihara.kazuya@gmail.com>
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
 * but WITHOUT Any WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Graphs.InChI;
using NCDK.IO;
using NCDK.IO.Iterator;
using NCDK.IO.RandomAccess;
using NCDK.SMARTS;
using NCDK.Smiles;
using System;
using System.IO;

namespace NCDK
{
    /// <summary>
    /// Utility class for quick access to frequently used functions.
    /// </summary>
    /// <remarks>
    /// Most of method names are from RDKit. 
    /// </remarks>
    /// <example>
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Chem_Example.cs"]/*' />
    /// </example>
    public partial class Chem
    {
        public static IAtomContainer MolFromSmiles(string smiles, bool sanitize = true)
        {
            var parser = new SmilesParser(CDK.Builder, kekulise: sanitize);
            return parser.ParseSmiles(smiles);
        }

        public static IReaction ReactionFromSmiles(string smiles, bool sanitize = true)
        {
            var parser = new SmilesParser(CDK.Builder, kekulise: sanitize);
            return parser.ParseReactionSmiles(smiles);
        }

        public static IAtomContainer MolFromFile(string fileName)
        {
            using (var reader = CDK.ReaderFactory.CreateReader(new FileStream(fileName, FileMode.Open)))
            {
                if (reader == null)
                    throw new Exception("Not supported.");
                return reader.Read(NewAtomContainer());
            }
        }

        public static IAtomContainer MolFromInChI(string inchi)
        {
            try
            {
                var mol = InChIToStructure.FromInChI(inchi);
                if (mol.ReturnStatus > InChIReturnCode.Warning)
                    throw new Exception(mol.Message);
                return mol.AtomContainer;
            }
            catch (Exception)
            {
            }
            return null;
        }

        private static IAtomContainer MolFrom(Func<TextReader> readerCreator)
        {
            foreach (var creator in
                new Func<TextReader, ISimpleChemObjectReader>[]
                {
                    n => new MDLV2000Reader(n),
                    n => new MDLV3000Reader(n),
                })
            {
                using (var r = creator(readerCreator()))
                {
                    var m = NewAtomContainer();
                    try
                    {
                        r.Read(m);
                        return m;
                    }
                    catch (Exception)
                    { }
                }
            }
            return null;
        }

        public static IAtomContainer MolFromMolFile(string fileName)
        {
            return MolFrom(() => new StreamReader(fileName));
        }

        public static IAtomContainer MolFromMolBlock(string molBlock)
        {
            return MolFrom(() => new StringReader(molBlock));
        }

        public static IAtomContainer MolFromSmarts(string smarts)
        {
            var mol = CDK.Builder.NewAtomContainer();
            Smarts.Parse(mol, smarts, SmartsFlaver.Daylight);
            return mol;
        }

        public static string MolToSmiles(IAtomContainer mol)
        {
            var gen = new SmilesGenerator();
            return gen.Create(mol);
        }

        public static string MolToSmarts(IAtomContainer mol)
        {
            return Smarts.Generate(mol);
        }


        public static string MolToMolBlock(IAtomContainer mol, bool forceV3000 = false)
        {
            using (var sr = new StringWriter())
            {
                IChemObjectWriter w;
                if (forceV3000 
                 || mol.Atoms.Count > 999
                 || mol.Bonds.Count > 999)
                    w = new MDLV3000Writer(sr);
                else
                    w = new MDLV2000Writer(sr);
                try
                {
                    w.Write(mol);
                }
                finally
                {
                    w.Close();
                }
                return sr.ToString();
            }
        }

        public static IEnumerableChemObjectReader<IAtomContainer> ForwardSDMolSupplier(Stream stream)
        {
            return new EnumerableSDFReader(stream, CDK.Builder);
        }

        public static IRandomAccessChemObjectReader<IAtomContainer> SDMolSupplier(string sdFileName)
        {
            return new RandomAccessSDFReader(sdFileName);
        }

        public static void AddHs(IAtomContainer mol)
        {
            CDK.HydrogenAdder.AddImplicitHydrogens(mol);
        }
    }
}
