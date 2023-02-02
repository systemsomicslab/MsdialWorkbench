using Riken.Metabolomics.Classfire;
using Riken.Metabolomics.StructureFinder.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Riken.Metabolomics.MsfinderConsoleApp.Test {

    public class MsdialQuery {
        public int Id { get; set; }
        public string Name { get; set; }
        public double Rt { get; set; }
        public double Mz { get; set; }
        public string Adduct { get; set; }
        public string InChIKey { get; set; }
        public string CarbonCount { get; set; }
        public string Smiles { get; set; }
        public string Ontology { get; set; }
        public string Intensity { get; set; }
        public string Formula { get; set; }
        public string MsMsSpectrum { get; set; }
        public string Ms1Spectrum { get; set; }
        public string Annotation { get; set; }
        public string IsotopeNumber { get; set; }
    }

    public class MsfinderQuery {
        public int Id { get; set; }
        public string CarbonCount { get; set; }
        public string Formula { get; set; }
        public string Ontology { get; set; }
        public string InChIKey { get; set; }
    }


    public sealed class MergeMsdialMsfinderResult {

        private MergeMsdialMsfinderResult() { }

        public static void Merge(string msdialInput, string msfinderInput, string output, string nametag) {

            var dictionary_inchikey_ontology = getInChIKeyOntologyDictionary(@"D:\PROJECT for MSFINDER\Classyfire results\Final result\Merged-vs6.txt");


            var msdialQueries = new List<MsdialQuery>();
            using (var sr = new StreamReader(msdialInput, Encoding.ASCII)) {
                sr.ReadLine();
                sr.ReadLine();
                sr.ReadLine();
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty) break;
                    var lineArray = line.Split('\t');

                    var query = new MsdialQuery() {
                        Id = int.Parse(lineArray[0]), Rt = double.Parse(lineArray[1]),
                        Mz = double.Parse(lineArray[2]), Name = lineArray[3],
                        Adduct = lineArray[4], Formula = lineArray[8],
                        InChIKey = lineArray[10], 
                        Smiles = lineArray[11], IsotopeNumber = lineArray[14], Ms1Spectrum = lineArray[19],
                        MsMsSpectrum = lineArray[20], Intensity = lineArray[21]
                       
                    };
                    msdialQueries.Add(query);
                }
            }

            var msfinderQueries = new List<MsfinderQuery>();
            using (var sr = new StreamReader(msfinderInput, Encoding.ASCII)) {
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty) break;
                    var lineArray = line.Split('\t');
                    if (lineArray[1] == string.Empty) continue;
                    var query = new MsfinderQuery() {
                        Id = int.Parse(lineArray[0]),
                        CarbonCount = lineArray[1], Formula = lineArray[2],
                        Ontology = lineArray[3], InChIKey = lineArray[4]
                    };
                    msfinderQueries.Add(query);
                }
            }

            msfinderQueries = msfinderQueries.Where(n => n.CarbonCount != string.Empty).ToList();

            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                sw.WriteLine("Name\tm/z\tRT\tAdduct\tIntensity\tAnnotation level\tCarbon count\tFormula\tOntology\tInChIKey\tMS1 spectrum\tMSMS spectrum");

                #region curation
                foreach (var msdialQuery in msdialQueries) {

                    var fieldName = nametag + "-" + msdialQuery.Id;

                    if (msdialQuery.Name == "Unknown") {

                        var matchedID = -1;
                        for (int i = 0; i < msfinderQueries.Count; i++) {
                            if (msfinderQueries[i].Id == msdialQuery.Id) {
                                matchedID = i;
                                break;
                            }
                        }

                        msdialQuery.Name = fieldName;
                        if (matchedID == -1) {

                            msdialQuery.Annotation = "Unknown";
                            msdialQuery.CarbonCount = "Unknown";
                            msdialQuery.Formula = "Unknown";
                            msdialQuery.Ontology = "Unknown";
                            msdialQuery.InChIKey = "Unknown";
                        }
                        else {
                            var msfinderQuery = msfinderQueries[matchedID];
                            var ontology = msfinderQuery.Ontology == string.Empty ? "Unknown" : msfinderQuery.Ontology;
                            var inchikey = msfinderQuery.InChIKey == string.Empty ? "Unknown" : msfinderQuery.InChIKey;
                            var formula = msfinderQuery.Formula == string.Empty ? "Unknown" : msfinderQuery.Formula;

                            var annotation = string.Empty;
                            if (inchikey != "Unknown") annotation = "Structure predicted";
                            else if (ontology != "Unknown") annotation = "Ontology predicted";
                            else if (formula != "Unknown") annotation = "Formula predicted";
                            else annotation = "Carbon shift confirmed";

                            msdialQuery.CarbonCount = msfinderQuery.CarbonCount;
                            msdialQuery.Formula = formula;
                            msdialQuery.Annotation = annotation;
                            msdialQuery.Ontology = ontology;
                            msdialQuery.InChIKey = inchikey;
                        }

                    }
                    else {

                        var annotationLevel = msdialQuery.Name.Contains(@"Level 1") ? "Standard" : "External spectral DBs";
                        var error = string.Empty;
                        var structure = MoleculeConverter.SmilesToStructure(msdialQuery.Smiles, out error);
                        var formula = MoleculeConverter.ConvertAtomDicionaryToFormula(structure.AtomDictionary);

                        var shortInChIKey = msdialQuery.InChIKey.Split('-')[0];
                        var ontology = dictionary_inchikey_ontology[shortInChIKey];

                        //var classyfireAPI = new ClassfireApi();
                        //var classyfireEntity = classyfireAPI.ReadClassyfireEntityByInChIKey(msdialQuery.InChIKey);
                        //if (classyfireEntity != null && classyfireEntity.direct_parent != null) {
                        //    ontology = classyfireEntity.direct_parent.name;
                        //}
                        //if (ontology == string.Empty) {
                        //    var id = classyfireAPI.PostSmilesQuery(msdialQuery.InChIKey, msdialQuery.Smiles);
                        //    if (id >= 0) {
                        //        var querys = classyfireAPI.ReadClassyfireResultByEntryID(id.ToString());
                        //        if (querys != null && querys.entities != null && querys.entities.Length > 0 && querys.entities[0].direct_parent != null) {
                        //            var query = querys.entities[0];
                        //            ontology = query.direct_parent.name;
                        //        }
                        //        else {
                        //            ontology = id.ToString();
                        //        }
                        //    }
                        //}

                        msdialQuery.Name = fieldName;
                        msdialQuery.Annotation = annotationLevel;
                        msdialQuery.CarbonCount = formula.Cnum.ToString();
                        msdialQuery.Formula = formula.FormulaString;
                        msdialQuery.Ontology = ontology;
                    }
                }
                #endregion

                foreach (var query in msdialQueries.Where(n => n.Annotation == "Standard").OrderBy(n => n.Id)) {
                    sw.WriteLine(query.Name + "\t" + query.Mz + "\t" + query.Rt + "\t" + query.Adduct + "\t" + query.Intensity
                        + "\t" + query.Annotation + "\t" + query.CarbonCount + "\t" + query.Formula + "\t" + query.Ontology + "\t" + query.InChIKey
                        + "\t" + query.Ms1Spectrum + "\t" + query.MsMsSpectrum);
                }

                foreach (var query in msdialQueries.Where(n => n.Annotation == "External spectral DBs").OrderBy(n => n.Id)) {
                    sw.WriteLine(query.Name + "\t" + query.Mz + "\t" + query.Rt + "\t" + query.Adduct + "\t" + query.Intensity
                        + "\t" + query.Annotation + "\t" + query.CarbonCount + "\t" + query.Formula + "\t" + query.Ontology + "\t" + query.InChIKey
                        + "\t" + query.Ms1Spectrum + "\t" + query.MsMsSpectrum);
                }

                foreach (var query in msdialQueries.Where(n => n.Annotation == "Structure predicted").OrderBy(n => n.Id)) {
                    sw.WriteLine(query.Name + "\t" + query.Mz + "\t" + query.Rt + "\t" + query.Adduct + "\t" + query.Intensity
                        + "\t" + query.Annotation + "\t" + query.CarbonCount + "\t" + query.Formula + "\t" + query.Ontology + "\t" + query.InChIKey
                        + "\t" + query.Ms1Spectrum + "\t" + query.MsMsSpectrum);
                }

                foreach (var query in msdialQueries.Where(n => n.Annotation == "Ontology predicted").OrderBy(n => n.Id)) {
                    sw.WriteLine(query.Name + "\t" + query.Mz + "\t" + query.Rt + "\t" + query.Adduct + "\t" + query.Intensity
                        + "\t" + query.Annotation + "\t" + query.CarbonCount + "\t" + query.Formula + "\t" + query.Ontology + "\t" + query.InChIKey
                        + "\t" + query.Ms1Spectrum + "\t" + query.MsMsSpectrum);
                }

                foreach (var query in msdialQueries.Where(n => n.Annotation == "Formula predicted" && n.MsMsSpectrum != string.Empty).OrderBy(n => n.Id)) {
                    sw.WriteLine(query.Name + "\t" + query.Mz + "\t" + query.Rt + "\t" + query.Adduct + "\t" + query.Intensity
                        + "\t" + query.Annotation + "\t" + query.CarbonCount + "\t" + query.Formula + "\t" + query.Ontology + "\t" + query.InChIKey
                        + "\t" + query.Ms1Spectrum + "\t" + query.MsMsSpectrum);
                }

                foreach (var query in msdialQueries.Where(n => n.Annotation == "Formula predicted" && n.MsMsSpectrum == string.Empty).OrderBy(n => n.Id)) {
                    sw.WriteLine(query.Name + "\t" + query.Mz + "\t" + query.Rt + "\t" + query.Adduct + "\t" + query.Intensity
                        + "\t" + query.Annotation + "\t" + query.CarbonCount + "\t" + query.Formula + "\t" + query.Ontology + "\t" + query.InChIKey
                        + "\t" + query.Ms1Spectrum + "\t" + query.MsMsSpectrum);
                }

                foreach (var query in msdialQueries.Where(n => n.Annotation == "Carbon shift confirmed" && n.MsMsSpectrum != string.Empty).OrderBy(n => n.Id)) {
                    sw.WriteLine(query.Name + "\t" + query.Mz + "\t" + query.Rt + "\t" + query.Adduct + "\t" + query.Intensity
                        + "\t" + query.Annotation + "\t" + query.CarbonCount + "\t" + query.Formula + "\t" + query.Ontology + "\t" + query.InChIKey
                        + "\t" + query.Ms1Spectrum + "\t" + query.MsMsSpectrum);
                }

                foreach (var query in msdialQueries.Where(n => n.Annotation == "Carbon shift confirmed" && n.MsMsSpectrum == string.Empty).OrderBy(n => n.Id)) {
                    sw.WriteLine(query.Name + "\t" + query.Mz + "\t" + query.Rt + "\t" + query.Adduct + "\t" + query.Intensity
                        + "\t" + query.Annotation + "\t" + query.CarbonCount + "\t" + query.Formula + "\t" + query.Ontology + "\t" + query.InChIKey
                        + "\t" + query.Ms1Spectrum + "\t" + query.MsMsSpectrum);
                }

                foreach (var query in msdialQueries.Where(n => n.Annotation == "Unknown" && n.MsMsSpectrum != string.Empty && n.IsotopeNumber == "0").OrderBy(n => n.Id)) {
                    sw.WriteLine(query.Name + "\t" + query.Mz + "\t" + query.Rt + "\t" + query.Adduct + "\t" + query.Intensity
                        + "\t" + query.Annotation + "\t" + query.CarbonCount + "\t" + query.Formula + "\t" + query.Ontology + "\t" + query.InChIKey
                        + "\t" + query.Ms1Spectrum + "\t" + query.MsMsSpectrum);
                }

                foreach (var query in msdialQueries.Where(n => n.Annotation == "Unknown" && n.MsMsSpectrum == string.Empty && n.IsotopeNumber == "0").OrderBy(n => n.Id)) {
                    sw.WriteLine(query.Name + "\t" + query.Mz + "\t" + query.Rt + "\t" + query.Adduct + "\t" + query.Intensity
                        + "\t" + query.Annotation + "\t" + query.CarbonCount + "\t" + query.Formula + "\t" + query.Ontology + "\t" + query.InChIKey
                        + "\t" + query.Ms1Spectrum + "\t" + query.MsMsSpectrum);
                }
            }
        }

        public static Dictionary<string, string> getInChIKeyOntologyDictionary(string input) {

            var dict = new Dictionary<string, string>();

            using (var sr = new StreamReader(input, Encoding.ASCII)) {
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty)
                        continue;
                    var lineArray = line.Split('\t');
                    var shortInChIKey = lineArray[0].Split('-')[0];
                    var ontology = lineArray[1];

                    dict[shortInChIKey] = ontology;
                }
            }

            return dict;
        }
    }
}
