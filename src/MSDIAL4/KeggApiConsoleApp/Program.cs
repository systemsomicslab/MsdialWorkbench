using Riken.Metabolomics.Kegg;
using Riken.Metabolomics.KeggApi.Parser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeggApiConsoleApp {
    class Program {
        static void Main(string[] args) {
            var folderpath = @"C:\Users\hiros\OneDrive\Desktop\KEGG kgml";
            var filepath = @"C:\Users\hiros\OneDrive\Desktop\keggcompoundIDs-round6.txt";
            var ownfilepath = @"C:\Users\hiros\OneDrive\Desktop\Own fields for keggscape.txt";
            var output = @"C:\Users\hiros\OneDrive\Desktop\keggscape-node-vs1.txt";

            new KeggApi().ConvertKeggIDsToFormulas(filepath);
            //new KgmlParser().MergeKgmlFieldAndOwnTextData(filepath, ownfilepath, output);

            //var ecNumbers = new List<string>();
            //using (var sr = new StreamReader(filepath, Encoding.ASCII)) {
            //    while (sr.Peek() > -1) {
            //        var line = sr.ReadLine();
            //        //var lineArray = line.Split('+');
            //        //for (int i = 1; i < lineArray.Length; i++) {
            //        //    if (!ecNumbers.Contains(lineArray[i]))
            //        //        ecNumbers.Add(lineArray[i]);
            //        //}
            //        ecNumbers.Add(line.Trim());
            //    }
            //}

            //using (var sw = new StreamWriter(output, false)) {
            //    //sw.WriteLine("Enzyme (EC number)\tType\tMetabolite name\tKEGG ID");
            //    sw.WriteLine("Kegg gene ID\tOrthology ID\tOrthology Name");
            //    var total = ecNumbers.Count();
            //    var counter = 0;
            //    foreach (var ecNumber in ecNumbers) {
            //        //var result = new KeggApi().GetSubstrateProductFromEcNumber(ecNumber);
            //        //foreach (var substrate in result.KeggSubstrates) {
            //        //    sw.WriteLine(result.EcNumber + "\t" + "Substrate" + "\t" + substrate.SubstrateName + "\t" + substrate.SubstrateID);
            //        //}
            //        //foreach (var product in result.KeggProducts) {
            //        //    sw.WriteLine(result.EcNumber + "\t" + "Product" + "\t" + product.ProductName + "\t" + product.ProductID);
            //        //}
            //        if (ecNumber == "#N/A") {
            //            sw.WriteLine("NA" + "\t" + "NA" + "\t" + "NA");
            //        }
            //        else {
            //            var result = new KeggApi().GetKeggOrthologyFromKeggGeneID(ecNumber);
            //            sw.WriteLine(result.GeneID + "\t" + result.KeggOrthology.OrthologyID + "\t" + result.KeggOrthology.OrthologyName);
            //        }

            //        //var result = new KeggApi().GetKeggOrthologiesFromEcNumber(ecNumber);
            //        //foreach (var orthlogy in result.KeggOrthologies) {
            //        //    sw.WriteLine(result.EcNumber + "\t" + orthlogy.OrthologyID + "\t" + orthlogy.OrthologyName);
            //        //}
            //        counter++;
            //        Console.Write("Done {0}", counter + "/" + total);
            //        Console.SetCursorPosition(0, Console.CursorTop);
            //    }
            //}


            //Console.ReadLine();


        }
    }
}
