/* Copyright (C) 2004-2007  Christian Hoppe <c.hoppe_@web.de>
 *                    2011  Egon Willighagen <egonw@users.sf.net>
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
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 */

using NCDK.Fingerprints;
using NCDK.Graphs;
using NCDK.IO;
using NCDK.IO.Iterator;
using NCDK.Isomorphisms.Matchers;
using NCDK.RingSearches;
using NCDK.Smiles;
using NCDK.Tools.Manipulator;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace NCDK.Modelings.Builder3D
{
    /// <summary>
    /// Helper class that help setup a template library of CDK's Builder3D.
    /// </summary>
    // @author      Christian Hoppe
    // @cdk.module  builder3dtools
    public static class TemplateExtractor
    {
        private readonly static IChemObjectBuilder builder = Silent.ChemObjectBuilder.Instance;

        public static void CleanDataSet(string dataFile)
        {
            var som = builder.NewAtomContainerSet();
            try
            {
                Console.Out.WriteLine("Start clean dataset...");
                using (var fin = new StreamReader(dataFile))
                using (var imdl = new EnumerableSDFReader(fin, builder))
                {
                    Console.Out.WriteLine("READY");
                    int c = 0;
                    foreach (var m in imdl)
                    {
                        c++;
                        if (c % 1000 == 0)
                        {
                            Console.Out.WriteLine("...");
                        }
                        if (m.Atoms.Count > 2)
                        {
                            if (m.Atoms[0].Point3D != null)
                            {
                                som.Add(m);
                            }
                        }
                    }
                }
                Console.Out.Write("Read File in..");
            }
            catch (Exception exc)
            {
                Console.Out.WriteLine($"Could not read Molecules from file {dataFile} due to: {exc.Message}");
            }
            Console.Out.WriteLine($"{som.Count} Templates are read in");
            WriteChemModel(som, dataFile, "_CLEAN");
        }

        public static void ReadNCISdfFileAsTemplate(string dataFile)
        {
            var som = builder.NewAtomContainerSet();
            try
            {
                Console.Out.WriteLine("Start...");
                using (var fin = new StreamReader(dataFile))
                using (var imdl = new EnumerableSDFReader(fin, builder))
                {
                    Console.Out.Write("Read File in..");
                    Console.Out.WriteLine("READY");
                    foreach (var m in imdl)
                    {
                        som.Add(m);
                    }
                }
            }
            catch (Exception exc)
            {
                Console.Out.WriteLine($"Could not read Molecules from file {dataFile} due to: {exc.Message}");
            }
            Console.Out.WriteLine(som.Count + " Templates are read in");
        }

        public static void PartitionRingsFromComplexRing(string dataFile)
        {
            var som = builder.NewAtomContainerSet();
            try
            {
                Console.Out.WriteLine("Start...");
                using (var fin = new StreamReader(dataFile))
                using (var imdl = new EnumerableSDFReader(fin, builder))
                {
                    Console.Out.Write("Read File in..");
                    Console.Out.WriteLine("READY");
                    foreach (var m in imdl)
                    {
                        Console.Out.WriteLine($"Atoms: {m.Atoms.Count}");
                        IRingSet ringSetM = Cycles.FindSSSR(m).ToRingSet();
                        // som.Add(m);
                        for (int i = 0; i < ringSetM.Count; i++)
                        {
                            som.Add(builder.NewAtomContainer(ringSetM[i]));
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                Console.Out.WriteLine($"Could not read Molecules from file {dataFile} due to: {exc.Message}");
            }
            Console.Out.WriteLine($"{som.Count} Templates are read in");
            WriteChemModel(som, dataFile, "_VERSUCH");
        }

        public static void ExtractUniqueRingSystemsFromFile(string dataFile)
        {
            Console.Out.WriteLine("****** EXTRACT UNIQUE RING SYSTEMS ******");
            Console.Out.WriteLine($"From file: {dataFile}");

            Dictionary<string, string> hashRingSystems = new Dictionary<string, string>();
            SmilesGenerator smilesGenerator = new SmilesGenerator();

            int counterRings = 0;
            int counterMolecules = 0;
            int counterUniqueRings = 0;
            IRingSet ringSet = null;
            string key = "";
            IAtomContainer ac = null;

            string molfile = dataFile + "_UniqueRings";

            try
            {
                using (var fout = new FileStream(molfile, FileMode.Create))
                using (var mdlw = new MDLV2000Writer(fout))
                {
                    try
                    {
                        Console.Out.WriteLine("Start...");
                        using (var fin = new StreamReader(dataFile))
                        using (var imdl = new EnumerableSDFReader(fin, builder))
                        {
                            Console.Out.WriteLine("Read File in..");

                            foreach (var m in imdl)
                            {
                                counterMolecules = counterMolecules + 1;
                                
                                IRingSet ringSetM = Cycles.FindSSSR(m).ToRingSet();

                                if (counterMolecules % 1000 == 0)
                                {
                                    Console.Out.WriteLine("Molecules:" + counterMolecules);
                                }

                                if (ringSetM.Count > 0)
                                {
                                    var ringSystems = RingPartitioner.PartitionRings(ringSetM);

                                    for (int i = 0; i < ringSystems.Count; i++)
                                    {
                                        ringSet = (IRingSet)ringSystems[i];
                                        ac = builder.NewAtomContainer();
                                        var containers = RingSetManipulator.GetAllAtomContainers(ringSet);
                                        foreach (var container in containers)
                                        {
                                            ac.Add(container);
                                        }
                                        counterRings = counterRings + 1;
                                        // Only connection is important
                                        for (int j = 0; j < ac.Atoms.Count; j++)
                                        {
                                            (ac.Atoms[j]).Symbol = "C";
                                        }

                                        try
                                        {
                                            key = smilesGenerator.Create(builder.NewAtomContainer(ac));
                                        }
                                        catch (CDKException e)
                                        {
                                            Trace.TraceError(e.Message);
                                            return;
                                        }

                                        if (hashRingSystems.ContainsKey(key))
                                        {
                                        }
                                        else
                                        {
                                            counterUniqueRings = counterUniqueRings + 1;hashRingSystems[key] = "1";
                                            try
                                            {
                                                mdlw.Write(builder.NewAtomContainer(ac));
                                            }
                                            catch (Exception emdl)
                                            {
                                                if (!(emdl is ArgumentException || emdl is CDKException))
                                                    throw;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception exc)
                    {
                        Console.Out.WriteLine($"Could not read Molecules from file {dataFile} due to: {exc.Message}");
                    }
                }
            }
            catch (Exception ex2)
            {
                Console.Out.WriteLine($"IOError:cannot write file due to: {ex2.ToString()}");
            }
            Console.Out.WriteLine($"READY Molecules:{counterMolecules} RingSystems:{counterRings} UniqueRingsSystem:{counterUniqueRings}");
            Console.Out.WriteLine($"HashtableKeys:{hashRingSystems.Count}");
        }

        public static void WriteChemModel(IChemObjectSet<IAtomContainer> som, string file, string endFix)
        {
            Console.Out.WriteLine($"WRITE Molecules:{som.Count}");
            string molfile = file + endFix;
            try
            {
                using (var mdlw = new MDLV2000Writer(new FileStream(molfile, FileMode.Create)))
                {
                    mdlw.Write(som);
                }
            }
            catch (Exception ex2)
            {
                if (!(ex2 is CDKException || ex2 is IOException))
                    throw;
                Console.Out.WriteLine("IOError:cannot write file due to:" + ex2.ToString());
            }
        }

        public static void MakeCanonicalSmileFromRingSystems(string dataFileIn, string dataFileOut)
        {
            Console.Out.WriteLine("Start make SMILES...");
            var data = new List<string>();
            var smiles = new SmilesGenerator();
            try
            {
                Console.Out.WriteLine("Start...");
                using (var imdl = new EnumerableSDFReader(new StreamReader(dataFileIn), builder))
                {
                    Console.Out.WriteLine("Read File in..");

                    foreach (var m in imdl)
                    {
                        try
                        {
                            data.Add((string)smiles.Create(builder.NewAtomContainer(m)));
                        }
                        catch (Exception exc1)
                        {
                            if (!(exc1 is CDKException || exc1 is IOException))
                                throw;
                            Console.Out.WriteLine("Could not create smile due to: " + exc1.Message);
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                Console.Out.WriteLine("Could not read Molecules from file " + dataFileIn + " due to: " + exc.Message);
            }

            Console.Out.Write("...ready\nWrite data...");
            try
            {
                using (var fout = new StreamWriter(dataFileOut))
                {
                    for (int i = 0; i < data.Count; i++)
                    {
                        try
                        {
                            fout.Write(((string)data[i]));
                            fout.WriteLine();
                        }
                        catch (Exception)
                        {
                        }
                    }
                    Console.Out.WriteLine($"number of smiles: {data.Count}");
                }
            }
            catch (Exception exc3)
            {
                Console.Out.WriteLine($"Could not write smile in file {dataFileOut} due to: {exc3.Message}");
            }
            Console.Out.WriteLine("...ready");
        }

        public static IReadOnlyList<IBitFingerprint> MakeFingerprintsFromSdf(bool anyAtom, bool anyAtomAnyBond, Dictionary<string, int> timings, TextReader fin, int limit)
        {
            var fingerPrinter = new HybridizationFingerprinter(HybridizationFingerprinter.DefaultSize, HybridizationFingerprinter.DefaultSearchDepth);
            fingerPrinter.SetHashPseudoAtoms(true);
            IAtomContainer query = null;
            var data = new List<IBitFingerprint>();
            try
            {
                Trace.TraceInformation("Read data file in ...");
                using (var imdl = new EnumerableSDFReader(fin, builder))
                {
                    Trace.TraceInformation("ready");

                    int moleculeCounter = 0;
                    int fingerprintCounter = 0;
                    Trace.TraceInformation($"Generated Fingerprints: {fingerprintCounter}    ");
                    foreach (var m in imdl)
                    {
                        if (!(moleculeCounter < limit || limit == -1))
                            break;
                        moleculeCounter++;
                        if (anyAtom && !anyAtomAnyBond)
                        {
                            query = QueryAtomContainerCreator.CreateAnyAtomContainer(m, false);
                        }
                        else
                        {
                            query = AtomContainerManipulator.Anonymise(m);
                        }
                        try
                        {
                            var time = -DateTime.Now.Ticks / 10000;
                            if (anyAtom || anyAtomAnyBond)
                            {
                                data.Add(fingerPrinter.GetBitFingerprint(query));
                                fingerprintCounter = fingerprintCounter + 1;
                            }
                            else
                            {
                                data.Add(fingerPrinter.GetBitFingerprint(query));
                                fingerprintCounter = fingerprintCounter + 1;
                            }
                            time += (DateTime.Now.Ticks / 10000);
                            // store the time
                            var bin = ((int)Math.Floor(time / 10.0)).ToString(NumberFormatInfo.InvariantInfo);
                            if (timings.ContainsKey(bin))
                            {
                                timings[bin] = (timings[bin]) + 1;
                            }
                            else
                            {
                                timings[bin] = 1;
                            }
                        }
                        catch (Exception exc1)
                        {
                            Trace.TraceInformation($"QueryFingerprintError: from molecule:{moleculeCounter} due to:{exc1.Message}");

                            // OK, just adds a fingerprint with all ones, so that any
                            // structure will match this template, and leave it up
                            // to substructure match to figure things out
                            var allOnesFingerprint = new BitSetFingerprint(fingerPrinter.Length);
                            for (int i = 0; i < fingerPrinter.Length; i++)
                            {
                                allOnesFingerprint.Set(i);
                            }
                            data.Add(allOnesFingerprint);
                            fingerprintCounter = fingerprintCounter + 1;
                        }

                        if (fingerprintCounter % 2 == 0)
                            Trace.TraceInformation("\b" + "/");
                        else
                            Trace.TraceInformation("\b" + "\\");

                        if (fingerprintCounter % 100 == 0)
                            Trace.TraceInformation("\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b"
                                    + "Generated Fingerprints: " + fingerprintCounter + "   \n");

                    }// while
                    Trace.TraceInformation($"...ready with:{moleculeCounter} molecules\nWrite data...of data vector:{data.Count} fingerprintCounter:{fingerprintCounter}");
                }
            }
            catch (Exception exc)
            {
                Console.Out.WriteLine("Could not read Molecules from file" + " due to: " + exc.Message);
            }
            return data;
        }

        public static void MakeFingerprintFromRingSystems(string dataFileIn, string dataFileOut, bool anyAtom, bool anyAtomAnyBond)
        {
            var timings = new Dictionary<string, int>();

            Console.Out.WriteLine($"Start make fingerprint from file: {dataFileIn} ...");
            using (var fin = new StreamReader(dataFileIn))
            {
                var data = MakeFingerprintsFromSdf(anyAtom, anyAtomAnyBond, timings, fin, -1);
                try
                {
                    using (var fout = new StreamWriter(dataFileOut))
                    {
                        for (int i = 0; i < data.Count; i++)
                        {
                            try
                            {
                                fout.Write(data[i].ToString());
                                fout.Write('\n');
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                }
                catch (Exception exc3)
                {
                    Console.Out.WriteLine($"Could not write Fingerprint in file {dataFileOut} due to: {exc3.Message}");
                }

                Console.Out.WriteLine($"\nFingerprints:{data.Count} are written...ready");
                Console.Out.WriteLine($"\nComputing time statistics:\n{timings.ToString()}");
            }
        }

        public static IAtomContainer RemoveLoopBonds(IAtomContainer molecule, int position)
        {
            for (int i = 0; i < molecule.Bonds.Count; i++)
            {
                var bond = molecule.Bonds[i];
                if (bond.Begin== bond.End)
                {
                    Console.Out.WriteLine($"Loop found! Molecule: {position}");
                    molecule.Bonds.Remove(bond);
                }
            }

            return molecule;
        }

        public static IAtomContainer CreateAnyAtomAtomContainer(IAtomContainer atomContainer)
        {
            var query = (IAtomContainer)atomContainer.Clone();
            for (int i = 0; i < query.Atoms.Count; i++)
            {
                query.Atoms[i].Symbol = "C";
            }
            return query;
        }

        public static IAtomContainer ResetFlags(IAtomContainer ac)
        {
            for (int f = 0; f < ac.Atoms.Count; f++)
            {
                ac.Atoms[f].IsVisited = false;
            }
            foreach (var ec in ac.GetElectronContainers())
            {
                ec.IsVisited = false;
            }
            return ac;
        }
    }
}
