
/* Copyright (C) 2010  Egon Willighagen <egonw@users.sf.net>
 * Copyright (C) 2018-2019  Kazuya Ujihara <ujihara.kazuya@gmail.com>
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 * All I ask is that proper credit is given for my work, which includes
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

namespace NCDK
{
    /// <summary>
    /// Helper class to provide general information about this CDK library.
    /// </summary>
    // @cdk.module core
    public static class CDK
    {
        /// <summary>
        /// Returns the version of this CDK library.
        /// </summary>
        /// <returns>The library version</returns>
        public static string Version => typeof(CDK).Assembly.GetName().Version.ToString();

        private static object syncLock = new object();
        private static Config.AtomTypeFactory localAtomTypeFactory = null;
        public static Config.AtomTypeFactory AtomTypeFactory
        {
            get
            {
                if (localAtomTypeFactory == null)
                    lock (syncLock)
                    {
                        if (localAtomTypeFactory == null)
                            localAtomTypeFactory = Config.AtomTypeFactory.GetInstance();
                    }
                return localAtomTypeFactory;
            }
        }
        private static Config.AtomTypeFactory localJmolAtomTypeFactory = null;
        internal static Config.AtomTypeFactory JmolAtomTypeFactory
        {
            get
            {
                if (localJmolAtomTypeFactory == null)
                    lock (syncLock)
                    {
                        if (localJmolAtomTypeFactory == null)
                            localJmolAtomTypeFactory = Config.AtomTypeFactory.GetInstance("NCDK.Config.Data.jmol_atomtypes.txt");
                    }
                return localJmolAtomTypeFactory;
            }
        }
        private static Config.AtomTypeFactory localCdkAtomTypeFactory = null;
        internal static Config.AtomTypeFactory CdkAtomTypeFactory
        {
            get
            {
                if (localCdkAtomTypeFactory == null)
                    lock (syncLock)
                    {
                        if (localCdkAtomTypeFactory == null)
                            localCdkAtomTypeFactory = Config.AtomTypeFactory.GetInstance("NCDK.Dict.Data.cdk-atom-types.owl");
                    }
                return localCdkAtomTypeFactory;
            }
        }
        private static Config.AtomTypeFactory localStructgenAtomTypeFactory = null;
        internal static Config.AtomTypeFactory StructgenAtomTypeFactory
        {
            get
            {
                if (localStructgenAtomTypeFactory == null)
                    lock (syncLock)
                    {
                        if (localStructgenAtomTypeFactory == null)
                            localStructgenAtomTypeFactory = Config.AtomTypeFactory.GetInstance("NCDK.Config.Data.structgen_atomtypes.xml");
                    }
                return localStructgenAtomTypeFactory;
            }
        }
        private static Tools.ISaturationChecker localSaturationChecker = null;
        public static Tools.ISaturationChecker SaturationChecker
        {
            get
            {
                if (localSaturationChecker == null)
                    lock (syncLock)
                    {
                        if (localSaturationChecker == null)
                            localSaturationChecker = new Tools.SaturationChecker();
                    }
                return localSaturationChecker;
            }
        }
        private static IChemObjectBuilder localBuilder = null;
        public static IChemObjectBuilder Builder
        {
            get
            {
                if (localBuilder == null)
                    lock (syncLock)
                    {
                        if (localBuilder == null)
                            localBuilder = Silent.ChemObjectBuilder.Instance;
                    }
                return localBuilder;
            }
        }
        private static Smiles.SmilesParser localSmilesParser = null;
        public static Smiles.SmilesParser SmilesParser
        {
            get
            {
                if (localSmilesParser == null)
                    lock (syncLock)
                    {
                        if (localSmilesParser == null)
                            localSmilesParser = new Smiles.SmilesParser();
                    }
                return localSmilesParser;
            }
        }
        private static Smiles.SmilesGenerator localSmilesGenerator = null;
        public static Smiles.SmilesGenerator SmilesGenerator
        {
            get
            {
                if (localSmilesGenerator == null)
                    lock (syncLock)
                    {
                        if (localSmilesGenerator == null)
                            localSmilesGenerator = new Smiles.SmilesGenerator(Smiles.SmiFlavors.Default);
                    }
                return localSmilesGenerator;
            }
        }
        private static Config.IsotopeFactory localIsotopeFactory = null;
        public static Config.IsotopeFactory IsotopeFactory
        {
            get
            {
                if (localIsotopeFactory == null)
                    lock (syncLock)
                    {
                        if (localIsotopeFactory == null)
                            localIsotopeFactory = Config.BODRIsotopeFactory.Instance;
                    }
                return localIsotopeFactory;
            }
        }
        private static Tools.ILonePairElectronChecker localLonePairElectronChecker = null;
        public static Tools.ILonePairElectronChecker LonePairElectronChecker
        {
            get
            {
                if (localLonePairElectronChecker == null)
                    lock (syncLock)
                    {
                        if (localLonePairElectronChecker == null)
                            localLonePairElectronChecker = new Tools.LonePairElectronChecker();
                    }
                return localLonePairElectronChecker;
            }
        }
        private static AtomTypes.IAtomTypeMatcher localAtomTypeMatcher = null;
        public static AtomTypes.IAtomTypeMatcher AtomTypeMatcher
        {
            get
            {
                if (localAtomTypeMatcher == null)
                    lock (syncLock)
                    {
                        if (localAtomTypeMatcher == null)
                            localAtomTypeMatcher = AtomTypes.CDKAtomTypeMatcher.GetInstance();
                    }
                return localAtomTypeMatcher;
            }
        }
        private static Tools.IHydrogenAdder localHydrogenAdder = null;
        public static Tools.IHydrogenAdder HydrogenAdder
        {
            get
            {
                if (localHydrogenAdder == null)
                    lock (syncLock)
                    {
                        if (localHydrogenAdder == null)
                            localHydrogenAdder = Tools.CDKHydrogenAdder.GetInstance();
                    }
                return localHydrogenAdder;
            }
        }
        private static StructGen.Stochastic.PartialFilledStructureMerger localPartialFilledStructureMerger = null;
        internal static StructGen.Stochastic.PartialFilledStructureMerger PartialFilledStructureMerger
        {
            get
            {
                if (localPartialFilledStructureMerger == null)
                    lock (syncLock)
                    {
                        if (localPartialFilledStructureMerger == null)
                            localPartialFilledStructureMerger = new StructGen.Stochastic.PartialFilledStructureMerger();
                    }
                return localPartialFilledStructureMerger;
            }
        }
        private static Charges.GasteigerMarsiliPartialCharges localGasteigerMarsiliPartialCharges = null;
        internal static Charges.GasteigerMarsiliPartialCharges GasteigerMarsiliPartialCharges
        {
            get
            {
                if (localGasteigerMarsiliPartialCharges == null)
                    lock (syncLock)
                    {
                        if (localGasteigerMarsiliPartialCharges == null)
                            localGasteigerMarsiliPartialCharges = new Charges.GasteigerMarsiliPartialCharges();
                    }
                return localGasteigerMarsiliPartialCharges;
            }
        }
        private static NCDK.IO.ReaderFactory localReaderFactory = null;
        internal static NCDK.IO.ReaderFactory ReaderFactory
        {
            get
            {
                if (localReaderFactory == null)
                    lock (syncLock)
                    {
                        if (localReaderFactory == null)
                            localReaderFactory = new NCDK.IO.ReaderFactory();
                    }
                return localReaderFactory;
            }
        }
    }
}

