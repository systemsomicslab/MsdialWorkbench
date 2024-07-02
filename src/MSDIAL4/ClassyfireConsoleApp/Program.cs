using Riken.Metabolomics.Classfire;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassyfireConsoleApp {
    class Program {
        static void Main(string[] args) {

            ClassifyInChIKeys(@"E:\6_Projects\PROJECT_MsMachineLearning\msn\msp\neg\MSMS-Public_experimentspectra-neg-VS19-curated_unhandledinchikey.txt",
                @"E:\6_Projects\PROJECT_MsMachineLearning\msn\msp\neg\MSMS-Public_experimentspectra-neg-VS19-curated_unhandledinchikey.classyfire");

            //ClassifyInChIKeys(@"D:\PROJECT for MSFINDER\Classyfire results\MINE contents.txt",
            //   @"D:\PROJECT for MSFINDER\Classyfire results\MINE contents-inchikeyclassyfied.txt");

            //ClassifyInChIKeys(@"D:\PROJECT for MSFINDER\Classyfire results\MSMS DB contents.txt",
            //   @"D:\PROJECT for MSFINDER\Classyfire results\MSMS DB contents-inchikeyclassyfied.txt");

            //RetrieveEntryIDs(@"D:\Paper of Natural Product Reports\Statistics\key4smilesclassyfire_id_result.txt",
            //   @"D:\Paper of Natural Product Reports\Statistics\key4smilesclassyfire_id_result_retrieved.txt");

            //ClassifySmilesCodes(@"D:\PROJECT for MSFINDER\FindMetDatabase\Small Molecular SDFs\20180128_Downloaded\3. Merged metabolite file\MsfinderDb-unclassified-inchikey-smiles-vs2.txt",
            //    @"D:\PROJECT for MSFINDER\FindMetDatabase\Small Molecular SDFs\20180128_Downloaded\3. Merged metabolite file\MsfinderDb-unclassified-inchikey-smiles-vs2-IDs.txt");

            //ClassifySmilesCodes(@"D:\Paper of Natural Product Reports\Statistics\key4smilesclassyfire.txt",
            //   @"D:\Paper of Natural Product Reports\Statistics\key4smilesclassyfire_id_result.txt");


            //ClassifySmilesCodes(@"D:\PROJECT for MSFINDER\Classyfire results\MINE contents-inchikeyclassyfied-unclassified-secondtrialIDs.txt",
            //    @"D:\PROJECT for MSFINDER\Classyfire results\MINE contents-inchikeyclassyfied-unclassified-secondtrialIDs-IDs.txt");

            //ClassifySmilesCodes(@"D:\PROJECT for MSFINDER\Classyfire results\MSMS DB contents-inchikeyclassyfied-unclassified-secondtrialIDs.txt",
            //    @"D:\PROJECT for MSFINDER\Classyfire results\MSMS DB contents-inchikeyclassyfied-unclassified-secondtrialIDs-IDs.txt");

            //RetrieveEntryIDsFromSdfFormat(@"D:\Bruker_Sumner_MetaboBASE_Plant_Library_new\MetaboBase-InChIKey-Smiles-pair-IDs.txt",
            //    @"D:\Bruker_Sumner_MetaboBASE_Plant_Library_new\MetaboBase-InChIKey-Smiles-pair-IDs-classified.txt");

            //RetrieveEntryIDsFromSdfFormat(@"D:\PROJECT for MSFINDER\Classyfire results\MINE contents-inchikeyclassyfied-unclassified-IDs-classified-unclassifieds.txt",
            //   @"D:\PROJECT for MSFINDER\Classyfire results\MINE contents-inchikeyclassyfied-unclassified-IDs-classified-unclassifieds-classified.txt");

            //RetrieveEntryIDsFromSdfFormat(@"D:\PROJECT for MSFINDER\Classyfire results\MSMS DB contents-inchikeyclassyfied-unclassified-IDs-classified-unclassifieds.txt",
            //   @"D:\PROJECT for MSFINDER\Classyfire results\MSMS DB contents-inchikeyclassyfied-unclassified-IDs-classified-unclassifieds-classified.txt");

            //ReadChemOntoFile(@"D:\PROJECT_Plant Specialized Metabolites Annotations\Chemical Ontology project\Classyfire\ChemOnt_2_1.obo",
            //    @"D:\PROJECT_Plant Specialized Metabolites Annotations\Chemical Ontology project\Classyfire\ChemOnt_2_1-extracted.txt");
        }



        public static void ReadChemOntoFile(string input, string output) {
            var id_name = new Dictionary<string, string>();
            using (var sr = new StreamReader(input, Encoding.ASCII)) {
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty)
                        continue;
                    var field = line.Trim();
                    if (field == "[Term]") {
                        var id = sr.ReadLine().Trim().Substring(4);
                        var name = sr.ReadLine().Trim().Substring(6);
                        if (!id_name.ContainsKey(id))
                            id_name[id] = name;
                    }
                }
            }
            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {

                sw.WriteLine("Name\tID");
                foreach (var pair in id_name) {
                    sw.WriteLine(pair.Value + "\t" + pair.Key);
                }
            }
        }

        //[0]inchikey [1]smiles
        public static void ClassifyInChIKeys(string input, string output) {

            var queries = new List<string[]>();
            using (var sr = new StreamReader(input, Encoding.ASCII)) {
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty)
                        continue;
                    queries.Add(line.Split('\t'));
                }
            }

            var cfApi = new ClassfireApi();
            var counter = 0;
            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                sw.WriteLine("InChIKey\tSMILES\tDirect parent name\tDirect parent chemont_id\tKingdom name\tKingdom chemont_id\t" +
                   "Superclass name\tSuperclass chemont_id\t" +
                   "Class name\tClass chemont_id\tSubclass name\tSubclass chemont_id");
                foreach (var key in queries) {

                    var inchikey = key[0];
                    var smiles = key[1];

                    counter++;

                    var query = cfApi.ReadClassyfireEntityByInChIKey(inchikey);
                    if (query != null) {
                        Console.WriteLine(counter + "\t" + inchikey + " Success");

                        sw.Write(inchikey + "\t" + smiles + "\t");

                        if (query.direct_parent == null)
                            sw.Write("\t");
                        else
                            sw.Write(query.direct_parent.name + "\t");

                        if (query.direct_parent == null)
                            sw.Write("\t");
                        else
                            sw.Write(query.direct_parent.chemont_id + "\t");

                        if (query.kingdom == null)
                            sw.Write("\t");
                        else
                            sw.Write(query.kingdom.name + "\t");

                        if (query.kingdom == null)
                            sw.Write("\t");
                        else
                            sw.Write(query.kingdom.chemont_id + "\t");
                        
                        if (query.superclass == null)
                            sw.Write("\t");
                        else
                            sw.Write(query.superclass.name + "\t");

                        if (query.superclass == null)
                            sw.Write("\t");
                        else
                            sw.Write(query.superclass.chemont_id + "\t");

                        if (query.nClass == null)
                            sw.Write("\t");
                        else
                            sw.Write(query.nClass.name + "\t");

                        if (query.nClass == null)
                            sw.Write("\t");
                        else
                            sw.Write(query.nClass.chemont_id + "\t");

                        if (query.subclass == null)
                            sw.Write("\t");
                        else
                            sw.Write(query.subclass.name + "\t");

                        if (query.subclass == null)
                            sw.WriteLine();
                        else
                            sw.WriteLine(query.subclass.chemont_id);
                    }
                    else {
                        Console.WriteLine(counter + "\t" + inchikey + " Failed");

                        sw.Write(inchikey + "\t" + smiles + "\t");
                        sw.Write("\t");
                        sw.Write("\t");
                        sw.Write("\t");
                        sw.Write("\t");
                        sw.Write("\t");
                        sw.Write("\t");
                        sw.Write("\t");
                        sw.Write("\t");
                        sw.Write("\t");
                        sw.WriteLine();
                    }
                }

            }
        }



        public static void RetrieveEntryIDs(string input, string output) {
            var pairs = new List<string[]>(); //[0] inchikey [1] id
            using (var sr = new StreamReader(input, Encoding.ASCII)) {
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty)
                        continue;
                    pairs.Add(line.Split('\t'));
                }
            }

            var cfApi = new ClassfireApi();
            var counter = 0;

            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                sw.WriteLine("ID\tInChIKey\tSMILES\tDirect parent name\tKingdom name\tSuperclass name\tClass name\tSubclass name");

                //sw.WriteLine("ID\tInChIKey\tSMILES\tKingdom name\tKingdom description\t" +
                //    "Kingdom chemont_id\tKingdom url\tSuperclass name\tSuperclass description\t" +
                //    "Superclass chemont_id\tSuperclass url\tClass name\tClass description\t" +
                //    "Class chemont_id\tClass url\tSubclass name\tSubclass description\t" +
                //    "Subclass chemont_id\tSubclass url\tDirect parent name\tDirect parent description\t" +
                //    "Direct parent chemont_id\tDirect parent url\tmolecular_framework");

                foreach (var pair in pairs) {
                    var id = pair[1];
                    var key = pair[0];
                    counter++;

                    var querys = cfApi.ReadClassyfireResultByEntryID(id);
                    if (querys != null && querys.entities != null && querys.entities.Length > 0) {
                        var query = querys.entities[0];

                        #region

                        Console.WriteLine(counter + "\t" + key + " Success");
                        sw.Write(id + "\t");
                        sw.Write(key + "\t");

                        if (query.smiles == null || query.smiles == string.Empty)
                            sw.Write("\t");
                        else
                            sw.Write(query.smiles + "\t");

                        if (query.direct_parent == null)
                            sw.Write("\t");
                        else
                            sw.Write(query.direct_parent.name + "\t");

                        if (query.kingdom == null)
                            sw.Write("\t");
                        else
                            sw.Write(query.kingdom.name + "\t");

                        if (query.superclass == null)
                            sw.Write("\t");
                        else
                            sw.Write(query.superclass.name + "\t");

                        if (query.nClass == null)
                            sw.Write("\t");
                        else
                            sw.Write(query.nClass.name + "\t");

                        if (query.subclass == null)
                            sw.WriteLine();
                        else
                            sw.WriteLine(query.subclass.name);
                        //Console.WriteLine(counter + "\t" + key + " Success");
                        //sw.Write(id + "\t");

                        //sw.Write(key + "\t");

                        //if (query.smiles == null || query.smiles == string.Empty)
                        //    sw.Write("\t");
                        //else
                        //    sw.Write(query.smiles + "\t");

                        //if (query.kingdom == null)
                        //    sw.Write("\t");
                        //else
                        //    sw.Write(query.kingdom.name + "\t");

                        //if (query.kingdom == null)
                        //    sw.Write("\t");
                        //else
                        //    sw.Write(query.kingdom.description + "\t");

                        //if (query.kingdom == null)
                        //    sw.Write("\t");
                        //else
                        //    sw.Write(query.kingdom.chemont_id + "\t");

                        //if (query.kingdom == null)
                        //    sw.Write("\t");
                        //else
                        //    sw.Write(query.kingdom.url + "\t");

                        //if (query.superclass == null)
                        //    sw.Write("\t");
                        //else
                        //    sw.Write(query.superclass.name + "\t");

                        //if (query.superclass == null)
                        //    sw.Write("\t");
                        //else
                        //    sw.Write(query.superclass.description + "\t");

                        //if (query.superclass == null)
                        //    sw.Write("\t");
                        //else
                        //    sw.Write(query.superclass.chemont_id + "\t");

                        //if (query.superclass == null)
                        //    sw.Write("\t");
                        //else
                        //    sw.Write(query.superclass.url + "\t");

                        //if (query.nClass == null)
                        //    sw.Write("\t");
                        //else
                        //    sw.Write(query.nClass.name + "\t");

                        //if (query.nClass == null)
                        //    sw.Write("\t");
                        //else
                        //    sw.Write(query.nClass.description + "\t");

                        //if (query.nClass == null)
                        //    sw.Write("\t");
                        //else
                        //    sw.Write(query.nClass.chemont_id + "\t");

                        //if (query.nClass == null)
                        //    sw.Write("\t");
                        //else
                        //    sw.Write(query.nClass.url + "\t");

                        //if (query.subclass == null)
                        //    sw.Write("\t");
                        //else
                        //    sw.Write(query.subclass.name + "\t");

                        //if (query.subclass == null)
                        //    sw.Write("\t");
                        //else
                        //    sw.Write(query.subclass.description + "\t");

                        //if (query.subclass == null)
                        //    sw.Write("\t");
                        //else
                        //    sw.Write(query.subclass.chemont_id + "\t");

                        //if (query.subclass == null)
                        //    sw.Write("\t");
                        //else
                        //    sw.Write(query.subclass.url + "\t");

                        //if (query.direct_parent == null)
                        //    sw.Write("\t");
                        //else
                        //    sw.Write(query.direct_parent.name + "\t");

                        //if (query.direct_parent == null)
                        //    sw.Write("\t");
                        //else
                        //    sw.Write(query.direct_parent.description + "\t");

                        //if (query.direct_parent == null)
                        //    sw.Write("\t");
                        //else
                        //    sw.Write(query.direct_parent.chemont_id + "\t");

                        //if (query.direct_parent == null)
                        //    sw.Write("\t");
                        //else
                        //    sw.Write(query.direct_parent.url + "\t");

                        //if (query.molecular_framework == null)
                        //    sw.WriteLine();
                        //else
                        //    sw.WriteLine(query.molecular_framework);
                        #endregion
                    }
                    else {
                        #region 
                        Console.WriteLine(counter + "\t" + key + " Failed");
                        sw.Write(id + "\t");
                        sw.Write(key + "\t");
                        sw.Write("\t");
                        sw.Write("\t");
                        sw.Write("\t");
                        sw.Write("\t");
                        sw.Write("\t");
                        sw.WriteLine();

                        //Console.WriteLine(counter + "\t" + key + " Failed");
                        //sw.Write(id + "\t");
                        //sw.Write(key + "\t");
                        //sw.Write("\t");
                        //sw.Write("\t");
                        //sw.Write("\t");
                        //sw.Write("\t");
                        //sw.Write("\t");
                        //sw.Write("\t");
                        //sw.Write("\t");
                        //sw.Write("\t");
                        //sw.Write("\t");
                        //sw.Write("\t");
                        //sw.Write("\t");
                        //sw.Write("\t");
                        //sw.Write("\t");
                        //sw.Write("\t");
                        //sw.Write("\t");
                        //sw.Write("\t");
                        //sw.Write("\t");
                        //sw.Write("\t");
                        //sw.Write("\t");
                        //sw.Write("\t");
                        //sw.Write("\t");
                        //sw.Write("\t");
                        //sw.WriteLine();
                        #endregion
                    }
                }
            }
        }

        public static void RetrieveEntryIDsFromSdfFormat(string input, string output) {
            var pairs = new List<string[]>(); //[0] inchikey [1] id
            using (var sr = new StreamReader(input, Encoding.ASCII)) {
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty)
                        continue;
                    pairs.Add(line.Split('\t'));
                }
            }

            var cfApi = new ClassfireApi();
            var counter = 0;

            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                sw.WriteLine("ID\tInChIKey\tSMILES\tDirect parent name\tKingdom name\tSuperclass name\tClass name\tSubclass name");

                foreach (var pair in pairs) {
                    var id = pair[1];
                    var key = pair[0];
                    counter++;

                    var query = cfApi.ReadClassyfireEntityAsSdfByEntryID(id);
                    if (query != null) {
                        #region
                        Console.WriteLine(counter + "\t" + key + " Success");
                        sw.Write(id + "\t");
                        sw.Write(key + "\t");

                        if (query.smiles == null || query.smiles == string.Empty)
                            sw.Write("\t");
                        else
                            sw.Write(query.smiles + "\t");

                        if (query.direct_parent == null)
                            sw.Write("\t");
                        else
                            sw.Write(query.direct_parent.name + "\t");

                        if (query.kingdom == null)
                            sw.Write("\t");
                        else
                            sw.Write(query.kingdom.name + "\t");

                        if (query.superclass == null)
                            sw.Write("\t");
                        else
                            sw.Write(query.superclass.name + "\t");

                        if (query.nClass == null)
                            sw.Write("\t");
                        else
                            sw.Write(query.nClass.name + "\t");

                        if (query.subclass == null)
                            sw.WriteLine();
                        else
                            sw.WriteLine(query.subclass.name);

                       
                        #endregion
                    }
                    else {
                        #region 
                        Console.WriteLine(counter + "\t" + key + " Failed");
                        sw.Write(id + "\t");
                        sw.Write(key + "\t");
                        sw.Write("\t");
                        sw.Write("\t");
                        sw.Write("\t");
                        sw.Write("\t");
                        sw.Write("\t");
                        sw.WriteLine();
                        #endregion
                    }
                }
            }
        }

        public static void ClassifySmilesCodes(string input, string output) {
            var pairs = new List<string[]>(); //[0] inchikey [1] SMILES
            using (var sr = new StreamReader(input, Encoding.ASCII)) {
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty)
                        continue;
                    pairs.Add(line.Split('\t'));
                }
            }

            var cfApi = new ClassfireApi();
            var counter = 0;
            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                //sw.WriteLine("ID\tInChIKey\tSMILES\tKingdom name\tKingdom description\t" +
                //    "Kingdom chemont_id\tKingdom url\tSuperclass name\tSuperclass description\t" +
                //    "Superclass chemont_id\tSuperclass url\tClass name\tClass description\t" +
                //    "Class chemont_id\tClass url\tSubclass name\tSubclass description\t" +
                //    "Subclass chemont_id\tSubclass url\tDirect parent name\tDirect parent description\t" +
                //    "Direct parent chemont_id\tDirect parent url\tmolecular_framework");
                sw.WriteLine("InChIKey\tID");
                foreach (var pair in pairs) {
                    var key = pair[0];
                    var smiles = pair[1];
                    counter++;

                    var id = cfApi.PostSmilesQuery(key, smiles);
                    sw.WriteLine(key + "\t" + id);
                    Console.WriteLine("Finished: " + counter + "\t" + key + "\t" + id);
                    //if (id == -1) {
                    //    #region 
                    //    Console.WriteLine(counter + "\t" + key + " Failed");
                    //    sw.Write(id + "\t");
                    //    sw.Write(key + "\t");
                    //    sw.Write("\t");
                    //    sw.Write("\t");
                    //    sw.Write("\t");
                    //    sw.Write("\t");
                    //    sw.Write("\t");
                    //    sw.Write("\t");
                    //    sw.Write("\t");
                    //    sw.Write("\t");
                    //    sw.Write("\t");
                    //    sw.Write("\t");
                    //    sw.Write("\t");
                    //    sw.Write("\t");
                    //    sw.Write("\t");
                    //    sw.Write("\t");
                    //    sw.Write("\t");
                    //    sw.Write("\t");
                    //    sw.Write("\t");
                    //    sw.Write("\t");
                    //    sw.Write("\t");
                    //    sw.Write("\t");
                    //    sw.Write("\t");
                    //    sw.Write("\t");
                    //    sw.WriteLine();
                    //    #endregion
                    //}
                    //else {
                    //    var query = cfApi.ReadClassyfireResultByEntryID(id.ToString());
                    //    if (query != null) {
                    //        #region
                    //        Console.WriteLine(counter + "\t" + key + " Success");
                    //        sw.Write(id + "\t");

                    //        sw.Write(key + "\t");

                    //        if (query.smiles == null || query.smiles == string.Empty)
                    //            sw.Write("\t");
                    //        else
                    //            sw.Write(query.smiles + "\t");

                    //        if (query.kingdom == null)
                    //            sw.Write("\t");
                    //        else
                    //            sw.Write(query.kingdom.name + "\t");

                    //        if (query.kingdom == null)
                    //            sw.Write("\t");
                    //        else
                    //            sw.Write(query.kingdom.description + "\t");

                    //        if (query.kingdom == null)
                    //            sw.Write("\t");
                    //        else
                    //            sw.Write(query.kingdom.chemont_id + "\t");

                    //        if (query.kingdom == null)
                    //            sw.Write("\t");
                    //        else
                    //            sw.Write(query.kingdom.url + "\t");

                    //        if (query.superclass == null)
                    //            sw.Write("\t");
                    //        else
                    //            sw.Write(query.superclass.name + "\t");

                    //        if (query.superclass == null)
                    //            sw.Write("\t");
                    //        else
                    //            sw.Write(query.superclass.description + "\t");

                    //        if (query.superclass == null)
                    //            sw.Write("\t");
                    //        else
                    //            sw.Write(query.superclass.chemont_id + "\t");

                    //        if (query.superclass == null)
                    //            sw.Write("\t");
                    //        else
                    //            sw.Write(query.superclass.url + "\t");

                    //        if (query.nClass == null)
                    //            sw.Write("\t");
                    //        else
                    //            sw.Write(query.nClass.name + "\t");

                    //        if (query.nClass == null)
                    //            sw.Write("\t");
                    //        else
                    //            sw.Write(query.nClass.description + "\t");

                    //        if (query.nClass == null)
                    //            sw.Write("\t");
                    //        else
                    //            sw.Write(query.nClass.chemont_id + "\t");

                    //        if (query.nClass == null)
                    //            sw.Write("\t");
                    //        else
                    //            sw.Write(query.nClass.url + "\t");

                    //        if (query.subclass == null)
                    //            sw.Write("\t");
                    //        else
                    //            sw.Write(query.subclass.name + "\t");

                    //        if (query.subclass == null)
                    //            sw.Write("\t");
                    //        else
                    //            sw.Write(query.subclass.description + "\t");

                    //        if (query.subclass == null)
                    //            sw.Write("\t");
                    //        else
                    //            sw.Write(query.subclass.chemont_id + "\t");

                    //        if (query.subclass == null)
                    //            sw.Write("\t");
                    //        else
                    //            sw.Write(query.subclass.url + "\t");

                    //        if (query.direct_parent == null)
                    //            sw.Write("\t");
                    //        else
                    //            sw.Write(query.direct_parent.name + "\t");

                    //        if (query.direct_parent == null)
                    //            sw.Write("\t");
                    //        else
                    //            sw.Write(query.direct_parent.description + "\t");

                    //        if (query.direct_parent == null)
                    //            sw.Write("\t");
                    //        else
                    //            sw.Write(query.direct_parent.chemont_id + "\t");

                    //        if (query.direct_parent == null)
                    //            sw.Write("\t");
                    //        else
                    //            sw.Write(query.direct_parent.url + "\t");

                    //        if (query.molecular_framework == null)
                    //            sw.WriteLine();
                    //        else
                    //            sw.WriteLine(query.molecular_framework);
                    //        #endregion
                    //    }
                    //    else {
                    //        #region 
                    //        Console.WriteLine(counter + "\t" + key + " Failed");
                    //        sw.Write(id + "\t");
                    //        sw.Write(key + "\t");
                    //        sw.Write("\t");
                    //        sw.Write("\t");
                    //        sw.Write("\t");
                    //        sw.Write("\t");
                    //        sw.Write("\t");
                    //        sw.Write("\t");
                    //        sw.Write("\t");
                    //        sw.Write("\t");
                    //        sw.Write("\t");
                    //        sw.Write("\t");
                    //        sw.Write("\t");
                    //        sw.Write("\t");
                    //        sw.Write("\t");
                    //        sw.Write("\t");
                    //        sw.Write("\t");
                    //        sw.Write("\t");
                    //        sw.Write("\t");
                    //        sw.Write("\t");
                    //        sw.Write("\t");
                    //        sw.Write("\t");
                    //        sw.Write("\t");
                    //        sw.Write("\t");
                    //        sw.WriteLine();
                    //        #endregion
                    //    }
                    //}
                }
            }
        }
    }
}
