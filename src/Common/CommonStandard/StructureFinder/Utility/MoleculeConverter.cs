using AtomProperty = CompMs.Common.StructureFinder.Property.AtomProperty;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Extension;
using CompMs.Common.StructureFinder.Property;
using CompMs.Common.Utility;
using NCDK;
using NCDK.Smiles;
using NCDK.Graphs;
using NCDK.Default;
using NCDK.Tools.Manipulator;
using NCDK.IO.Iterator;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CompMs.Common.StructureFinder.DataObj;
using CompMs.Common.Parameter;
using NCDK.Aromaticities;
using CompMs.Common.ClassyfireApiStandard;
using System.Diagnostics;

namespace CompMs.Common.StructureFinder.Utility
{
    public sealed class MoleculeConverter
    {
        private MoleculeConverter() { }

        private static readonly IChemObjectBuilder builder = CDK.Builder;
        private static SmilesParser parser = new SmilesParser(builder);
        private static SmilesGenerator smilesGenerator = new SmilesGenerator(SmiFlavors.Canonical);


        //public static List<IAtomContainer> SdfToIAtomContainers(string sdfFile)
        //{
        //    var containers = new List<IAtomContainer>();
        //    var sdffileJavaString = java.lang.String.valueOf(sdfFile);

        //    using (var fr = new FileReader(new java.io.File(sdffileJavaString))) {
        //        var imr = new IteratingMDLReader(fr, DefaultChemObjectBuilder.getInstance());
        //        while (imr.hasNext()) {
        //            var container = imr.next();
        //            if (container == null) continue;
        //            containers.Add(container);
        //        }
        //        imr.close();
        //    }

        //    return containers;
        //}

        public static Formula ConvertAtomDicionaryToFormula(Dictionary<int, AtomProperty> atomDictionary)
        {
            var cCount = atomDictionary.Count(n => n.Value.AtomString == "C");
            var hCount = atomDictionary.Count(n => n.Value.AtomString == "H");
            var nCount = atomDictionary.Count(n => n.Value.AtomString == "N");
            var oCount = atomDictionary.Count(n => n.Value.AtomString == "O");
            var sCount = atomDictionary.Count(n => n.Value.AtomString == "S");
            var pCount = atomDictionary.Count(n => n.Value.AtomString == "P");
            var fCount = atomDictionary.Count(n => n.Value.AtomString == "F");
            var clCount = atomDictionary.Count(n => n.Value.AtomString == "Cl");
            var brCount = atomDictionary.Count(n => n.Value.AtomString == "Br");
            var iCount = atomDictionary.Count(n => n.Value.AtomString == "I");
            var siCount = atomDictionary.Count(n => n.Value.AtomString == "Si");

            return new Formula(cCount, hCount, nCount, oCount, pCount, sCount, fCount
                , clCount, brCount, iCount, siCount);
        }

        public static void MoleculeNumbering(IAtomContainer mol)
        {
            var count = 0;
            var countBond = 0;
            var alreadyDone = new List<IAtom>();

            foreach (var bond in mol.Bonds)
            {
                //bond.setID(countBond.ToString(CultureInfo.InvariantCulture));
                bond.Id = countBond.ToString();
                countBond++;

                foreach (var atom in bond.Atoms)
                {
                    if (!alreadyDone.Contains(atom))
                    {
                        //atom.setID(count.ToString(CultureInfo.InvariantCulture));
                        atom.Id = count.ToString();
                        count++;
                        alreadyDone.Add(atom);
                    }
                }
            }
        }

        public static Dictionary<IAtom, List<IBond>> GetAtomBondsDictionary(IAtomContainer atomContainer)
        {
            var dict = new Dictionary<IAtom, List<IBond>>();
            foreach (var bond in atomContainer.Bonds)
            {
                foreach (var atom in bond.Atoms)
                {
                    if (!dict.ContainsKey(atom))
                    {
                        dict[atom] = new List<IBond>();
                    }
                    dict[atom].Add(bond);
                }
            }
            return dict;
        }

        public static IAtomContainer SmilesToIAtomContainer(string smiles, out string error)
        {
            error = "Error\r\n";

            IAtomContainer container = null;
            try
            {
                //var smilesParser = new SmilesParser();
                container = parser.ParseSmiles(smiles);
            }
            catch (InvalidSmilesException)
            {
                error += "SMILES: cannot be converted.\r\n";
                return null;
            }

            if (!ConnectivityChecker.IsConnected(container))
            {
                error += "SMILES: the connectivity is not correct.\r\n";
                return null;
            }

            return container;
        }

        public static IAtomContainer DictionaryToAtomContainer(Dictionary<int, AtomProperty> atomDictionary, Dictionary<int, BondProperty> bondDictionary)
        {
            var atoms = new List<IAtom>();
            var bonds = new List<IBond>();

            foreach (var atom in atomDictionary.Values)
            {
                atoms.Add(atom.IAtom);
            }

            foreach (var bond in bondDictionary.Values)
            {
                bonds.Add(bond.IBond);
            }

            var iContainer = new AtomContainer();
            iContainer.SetAtoms(atoms.ToArray());
            iContainer.SetBonds(bonds.ToArray());

            return iContainer;
        }

        public static string AtomContainerToSmiles(IAtomContainer container)
        {
            IAtomContainer implicitHydrizedContainer = null;
            var smiles = string.Empty;

            try
            {
                implicitHydrizedContainer = AtomContainerManipulator.RemoveHydrogens(container);
                //ExtAtomContainerManipulator.convertExplicitToImplicitHydrogens(container);
                smiles = smilesGenerator.Create(implicitHydrizedContainer);
            }
            catch (CDKException ex)
            {
                Console.WriteLine(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                Console.WriteLine(ex.Message);
            }

            if (smiles.IsEmptyOrNull())
            {
                try
                {
                    smiles = smilesGenerator.Create(container);
                }
                catch (CDKException ex)
                {
                    Console.WriteLine(ex.Message);
                }
                catch (KeyNotFoundException ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            return smiles;
        }

        public static string AtomContainerToInChIKey(IAtomContainer container)
        {
            var inchikey = string.Empty;
            //try {
            //    var inchiFactory = InChIGeneratorFactory.getInstance();
            //    var inchiGenerator = inchiFactory.getInChIGenerator(container);
            //    inchikey = inchiGenerator.getInchiKey();
            //}
            //catch (CDKException e) {
            //    e.printStackTrace();  //To change body of catch statement use File | Settings | File Templates.
            //}
            //catch (RemoteException e) {
            //    e.printStackTrace();  //To change body of catch statement use File | Settings | File Templates.
            //}

            return inchikey;
        }

        public static List<Structure> SdfToStructures(string sdfFile, List<ExistStructureQuery> existStructureDB, AnalysisParamOfMsfinder param,
            int tmsCount = 0, int meoxCount = 0)
        {
            var structures = new List<Structure>();
            var dbQueryStrings = getDabaseQueries(param.DatabaseQuery);

            var counter = 0;
            while (ErrorHandler.IsFileLocked(sdfFile, out string error))
            {
                System.Threading.Thread.Sleep(2000);
                counter++;
                if (counter > 3)
                {
                    error = "Cannot open this file: " + sdfFile;
                    Console.WriteLine(error);
                    return structures;
                }
            }

            using (var fr = new FileStream(sdfFile, FileMode.Open))
            {
                var imr = new EnumerableSDFReader(fr, ChemObjectBuilder.Instance);
                foreach (var container in imr)
                {
                    if (container == null)
                        continue;

                    var title = string.Empty;
                    var id = string.Empty;
                    var inchikey = string.Empty;

                    readPubChemMetadata(container, out title, out id, out inchikey);

                    Kekulization.Kekulize(container);
                    Structure structure = null;
                    if (tmsCount > 0 || meoxCount > 0)
                    {
                        var derivativeContainer = Derivatization.TmsMeoxDerivatization(container, tmsCount, meoxCount);
                        structure = new Structure(derivativeContainer);
                        // container = Derivatization.TmsMeoxDerivatization(container, tmsCount, meoxCount);
                    }
                    else
                    {
                        structure = new Structure(container);
                    }

                    // var structure = new Structure(container);
                    if (structure.IsValidatedStructure == true)
                    {
                        //Debug.WriteLine("Exact mass {0}, Atom count {1}, Bond count {2}, Charge count {3}",
                        //    structure.ExactMass, structure.AtomDictionary.Count,
                        //    structure.BondDictionary.Count,
                        //    structure.AtomDictionary.Count(n => n.Value.AtomCharge != 0));

                        structure.Title = title;
                        structure.Id = id;
                        structure.DatabaseQueries = dbQueryStrings;

                        var resourceNames = string.Empty;
                        var resourceNumber = 0;
                        var ontology = string.Empty;
                        var ontologyID = string.Empty;
                        findStructureQueries(structure.Formula, inchikey, existStructureDB, param.DatabaseQuery,
                            out resourceNames, out resourceNumber, out ontology, out ontologyID);

                        structure.Inchikey = inchikey;
                        structure.ResourceNames = resourceNames;
                        structure.ResourceNumber = resourceNumber;
                        structure.Ontology = ontology;
                        structure.OntologyID = ontologyID;

                        structures.Add(structure);
                    }
                }
            }

            //var sdffileJavaString = java.lang.String.valueOf(sdfFile);

            //using (var fr = new FileReader(new java.io.File(sdffileJavaString))) {
            //    var imr = new IteratingMDLReader(fr, DefaultChemObjectBuilder.getInstance());
            //    while (imr.hasNext()) {
            //        var container = imr.next();
            //        if (container == null)
            //            continue;

            //        var title = string.Empty;
            //        var id = string.Empty;
            //        var inchikey = string.Empty;

            //        readPubChemMetadata(container, out title, out id, out inchikey);

            //        Kekulization.Kekulize(container);
            //        if (tmsCount > 0 || meoxCount > 0)
            //            container = Derivatization.TmsMeoxDerivatization(container, tmsCount, meoxCount);

            //        var structure = new Structure(container);
            //        if (structure.IsValidatedStructure == true) {
            //            //Debug.WriteLine("Exact mass {0}, Atom count {1}, Bond count {2}, Charge count {3}",
            //            //    structure.ExactMass, structure.AtomDictionary.Count,
            //            //    structure.BondDictionary.Count,
            //            //    structure.AtomDictionary.Count(n => n.Value.AtomCharge != 0));

            //            structure.Title = title;
            //            structure.Id = id;
            //            structure.DatabaseQueries = dbQueryStrings;

            //            var resourceNames = string.Empty;
            //            var resourceNumber = 0;
            //            var ontology = string.Empty;
            //            var ontologyID = string.Empty;
            //            findStructureQueries(structure.Formula, inchikey, existStructureDB, param.DatabaseQuery,
            //                out resourceNames, out resourceNumber, out ontology, out ontologyID);

            //            structure.Inchikey = inchikey;
            //            structure.ResourceNames = resourceNames;
            //            structure.ResourceNumber = resourceNumber;
            //            structure.Ontology = ontology;
            //            structure.OntologyID = ontologyID;

            //            structures.Add(structure);
            //        }
            //    }
            //    imr.close();
            //}

            return structures;
        }

        private static string getDabaseQueries(DatabaseQuery databaseQuery)
        {
            var queryStrings = string.Empty;
            var infoArray = databaseQuery.GetType().GetProperties();
            foreach (var info in infoArray)
            {
                if ((bool)info.GetValue(databaseQuery, null) == true)
                {
                    queryStrings += info.Name + ";";
                }
            }
            return queryStrings;
        }

        private static void findStructureQueries(Formula formula, string inchikey, List<ExistStructureQuery> existStructureDB, DatabaseQuery dbQueries,
            out string resourceNames, out int resourceNumber, out string ontology, out string ontologyID)
        {

            resourceNames = string.Empty;
            resourceNumber = 0;
            ontology = string.Empty;
            ontologyID = string.Empty;

            if (inchikey == null || inchikey == string.Empty) return;
            var eQueries = DatabaseAccessUtility.GetStructureQueries(formula, existStructureDB, dbQueries);

            var flg = false;
            if (eQueries != null && eQueries.Count != 0)
            {
                var shortInchiKey = inchikey.Split('-')[0];
                foreach (var query in eQueries)
                {
                    if (query.ShortInchiKey == shortInchiKey)
                    {
                        resourceNames = query.ResourceNames;
                        resourceNumber = query.ResourceNumber;
                        ontology = query.ClassyfireOntology;
                        ontologyID = query.ClassyfireID;
                        flg = true;
                        break;
                    }
                }
            }

            if (flg == true) return;

            var classyfireAPI = new ClassfireApi();
            var classyfireEntity = classyfireAPI.ReadClassyfireEntityByInChIKey(inchikey);
            if (classyfireEntity != null && classyfireEntity.direct_parent != null)
            {
                ontology = classyfireEntity.direct_parent.name;
                ontologyID = classyfireEntity.direct_parent.chemont_id;
            }
        }

        private static void readPubChemMetadata(IAtomContainer originalCompound, out string title, out string id, out string inchikey)
        {
            var titleCandidate = string.Empty;
            title = string.Empty;
            id = string.Empty;
            inchikey = string.Empty;

            if (originalCompound.GetProperty<string>("PUBCHEM_IUPAC_TRADITIONAL_NAME") != null)
                titleCandidate = originalCompound.GetProperty<string>("PUBCHEM_IUPAC_TRADITIONAL_NAME").ToString();
            else if (originalCompound.GetProperty<string>("PUBCHEM_IUPAC_OPENEYE_NAME") != null)
                titleCandidate = originalCompound.GetProperty<string>("PUBCHEM_IUPAC_OPENEYE_NAME").ToString();
            else if (originalCompound.GetProperty<string>(CDKPropertyName.Title) != null)
                titleCandidate = originalCompound.GetProperty<string>(CDKPropertyName.Title).ToString();

            if (originalCompound.GetProperty<string>("PUBCHEM_COMPOUND_CID") != null)
                id = originalCompound.GetProperty<string>("PUBCHEM_COMPOUND_CID").ToString();
            if (originalCompound.GetProperty<string>("PUBCHEM_IUPAC_INCHIKEY") != null)
            {
                inchikey = originalCompound.GetProperty<string>("PUBCHEM_IUPAC_INCHIKEY").ToString();
                var shortInchiKey = inchikey.Split('-')[0];
                if (shortInchiKey.Length > 0)
                    id = shortInchiKey;
            }

            title = titleCandidate == string.Empty ? id : titleCandidate;
        }

        public static string BondPropertyToString(BondProperty bondProp, bool isHalogenConvertToX)
        {
            var order = StructureEnumConverter.BondTypeToString(bondProp.BondType);
            var atom1 = bondProp.ConnectedAtoms[0];
            var atom2 = bondProp.ConnectedAtoms[1];

            var atom1String = atom1.AtomString;
            var atom2String = atom2.AtomString;
            if (isHalogenConvertToX == true)
            {
                if (atom1String == "F" || atom1String == "Cl" || atom1String == "Br" || atom1String == "I") atom1String = "X";
                if (atom2String == "F" || atom2String == "Cl" || atom2String == "Br" || atom2String == "I") atom2String = "X";
            }

            //priority C > N > O > S > P > Si
            if (atom1.AtomString == atom2.AtomString) return atom1String + order + atom2String;

            var atom1PrioriValue = atom1.AtomPriority;
            var atom2PrioriValue = atom2.AtomPriority;
            if (atom1PrioriValue > atom2PrioriValue)
            {
                return atom1String + order + atom2String;
            }
            else
            {
                return atom2String + order + atom1String;
            }
        }

        public static string BondPropertyToString(BondProperty bondProp)
        {
            var order = StructureEnumConverter.BondTypeToString(bondProp.BondType);
            var atom1 = bondProp.ConnectedAtoms[0];
            var atom2 = bondProp.ConnectedAtoms[1];

            var atom1String = atom1.AtomString;
            var atom2String = atom2.AtomString;

            //priority C > N > O > S > P > Si
            if (atom1String == atom2String) return atom1String + order + atom2String;

            var atom1PrioriValue = atom1.AtomPriority;
            var atom2PrioriValue = atom2.AtomPriority;
            if (atom1PrioriValue > atom2PrioriValue)
            {
                return atom1String + order + atom2String;
            }
            else
            {
                return atom2String + order + atom1String;
            }
        }

        public static int GetAtomPriorityValue(string atomString)
        {
            //priority C > N > O > S > P > Si >F > Cl > Br > I >  H > Others
            switch (atomString)
            {
                case "C": return 11;
                case "N": return 10;
                case "O": return 9;
                case "S": return 8;
                case "P": return 7;
                case "Si": return 6;
                case "F": return 5;
                case "Cl": return 4;
                case "Br": return 3;
                case "I": return 2;
                case "H": return 1;
                default: return 0;
            }
        }

        public static int GetAtomBondPairPriorityValue(AtomProperty atom, BondProperty bond)
        {
            var atomBondPair = atom.AtomString + StructureEnumConverter.BondTypeToString(bond.BondType);

            switch (atomBondPair)
            {
                case "C#": return 17;
                case "C=": return 16;
                case "C-": return 15;
                case "N#": return 14;
                case "N=": return 13;
                case "N-": return 12;
                case "O=": return 11;
                case "O-": return 10;
                case "P=": return 9;
                case "P-": return 8;
                case "S=": return 7;
                case "S-": return 6;
                case "F-": return 5;
                case "Cl-": return 4;
                case "Br-": return 3;
                case "I-": return 2;
                case "Si-": return 1;
                default: return 0;
            }
        }

        public static List<Structure> QueriesToStructures(List<ExistStructureQuery> queries, string databaseQueries, int tmsCount = 0, int meoxCount = 0)
        {
            var structures = new List<Structure>();

            var syncObj = new object();
            //Parallel.ForEach(queries, query => {
            //    var error = string.Empty;
            //    var structure = ExistStructureQueryToStructure(query, databaseQueries, out error, tmsCount, meoxCount);
            //    if (error != string.Empty) Debug.WriteLine(error);
            //    if (structure != null) {
            //        lock (syncObj) {
            //            structures.Add(structure);
            //        }
            //    }
            //});
            foreach (var query in queries)
            {
                var error = string.Empty;
                //Console.WriteLine(query.Smiles);
                var structure = ExistStructureQueryToStructure(query, databaseQueries, out error, tmsCount, meoxCount);
                if (error != string.Empty) Debug.WriteLine(error);
                if (structure != null)
                {
                    structures.Add(structure);
                }
            };
            return structures;
        }

        public static Structure ExistStructureQueryToStructure(ExistStructureQuery query, string databaseQueries, out string error,
            int tmsCount = 0, int meoxCount = 0)
        {
            error = string.Empty;
            var smiles = query.Smiles;
            if (smiles != null && smiles != string.Empty)
            {
                var structure = SmilesToStructure(smiles, out error, tmsCount, meoxCount);
                if (structure == null) return null;
                if (structure.IsValidatedStructure == true)
                {
                    structure.Title = query.Title;
                    structure.Id = query.ShortInchiKey;
                    structure.Inchikey = query.InchiKey;
                    structure.Ontology = query.ClassyfireOntology;
                    structure.OntologyID = query.ClassyfireID;
                    structure.ResourceNames = query.ResourceNames;
                    structure.ResourceNumber = query.ResourceNumber;
                    structure.DatabaseQueries = databaseQueries;
                    structure.Retentiontime = query.Retentiontime;
                    structure.AdductToCcs = query.AdductToCCS;
                    return structure;
                }
                else
                {
                    return null;
                }
            }
            return null;
        }

        public static Structure SmilesToStructure(string smiles, out string error, int tmsCount = 0, int meoxCount = 0)
        {
            error = string.Empty;

            IAtomContainer container = null;
            try
            {
                // var smilesParser = new SmilesParser();
                container = parser.ParseSmiles(smiles);
                //AtomContainerManipulator.GetMass(container);
                //AtomContainerManipulator.PercieveAtomTypesAndConfigureAtoms(container);
                if (container != null && smiles.Contains('c'))
                {
                    Kekulization.Kekulize(container);
                }
                if (tmsCount > 0 || meoxCount > 0)
                    container = Derivatization.TmsMeoxDerivatization(container, tmsCount, meoxCount);
                if (container == null) return null;
            }
            catch (InvalidSmilesException)
            {
                error += "SMILES: cannot be converted.\r\n";
                return null;
            }

            if (!ConnectivityChecker.IsConnected(container))
            {
                error += "SMILES: the connectivity is not correct.\r\n";
                return null;
            }

            var structure = new Structure(container);
            if (structure.IsValidatedStructure == true)
            {
                #region //validation
                //Debug.WriteLine("Exact mass {0}, Atom count {1}, Bond count {2}, Charge count {3}",
                //            structure.ExactMass, structure.AtomDictionary.Count,
                //            structure.BondDictionary.Count,
                //            structure.AtomDictionary.Count(n => n.Value.AtomCharge != 0));

                //foreach (var atomElem in structure.AtomDictionary) {
                //    var atomID = atomElem.Key;
                //    var atomValue = atomElem.Value;
                //    Debug.WriteLine("ID {0}, Symbol {1}, Mass {2}, Charge {3}, Bond count {4}, Type {5}",
                //        atomID, atomValue.AtomString, atomValue.AtomMass, atomValue.AtomCharge, atomValue.ConnectedBonds.Count, atomValue.AtomFunctionType);
                //}

                //foreach (var bondElem in structure.BondDictionary) {
                //    var bondID = bondElem.Key;
                //    var bondValue = bondElem.Value;
                //    Debug.WriteLine("ID {0}, Type {1}, Direction {2}, Aromaticity {3}, In ring {4}, atom count {5}",
                //        bondID, bondValue.BondType, bondValue.BondDirection, bondValue.IsAromaticity, bondValue.IsInRing, bondValue.ConnectedAtoms.Count);
                //}

                //foreach (var ringsetElem in structure.RingsetDictionary) {
                //    var ringsetID = ringsetElem.Key;
                //    var ringsetValue = ringsetElem.Value;

                //    Debug.WriteLine("ID {0}, Type {1}, Aromaticity {2}, Heterosity {3}",
                //        ringsetID, ringsetValue.RingsetFunctionType, ringsetValue.IsAromaticRingset, ringsetValue.IsHeteroRingset);

                //    foreach (var ringID in ringsetValue.RingIDs) {
                //        var ring = structure.RingDictionary[ringID];
                //        Debug.WriteLine("ID {0}, Ringset ID {1}, Type {2}, Benzene {3}, Sugar {4}, Aromatic {5}",
                //        ring.RingID, ring.RingsetID, ring.RingFunctionType, ring.IsBenzeneRing, ring.IsSugarRing, ring.IsAromaticRing);

                //    }
                //}

                //var nContainer = MoleculeConverter.DictionaryToAtomContainer(structure.AtomDictionary, structure.BondDictionary);
                //var nSmiles = MoleculeConverter.AtomContainerToSmiles(nContainer);
                //Debug.WriteLine(nSmiles);
                #endregion
                return structure;
            }
            else
            {
                error += structure.ErrorMessage;
                return null;
            }
        }
    }
}
