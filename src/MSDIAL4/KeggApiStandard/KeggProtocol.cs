using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Riken.Metabolomics.Kegg
{
    public class KeggEnzyme {
        public string EcNumber { get; set; } = string.Empty;
        public List<KeggOrthology> KeggOrthologies { get; set; } = new List<KeggOrthology>();
        public List<KeggProduct> KeggProducts { get; set; } = new List<KeggProduct>();
        public List<KeggSubstrate> KeggSubstrates { get; set; } = new List<KeggSubstrate>();
    }

    public class KeggGene {
        public string GeneID { get; set; } = string.Empty;
        public KeggOrthology KeggOrthology { get; set; } = new KeggOrthology();
    }

    public class KeggOrthology {
        public string OrthologyName { get; set; }
        public string OrthologyID { get; set; }
    }

    public class KeggSubstrate {
        public string SubstrateName { get; set; }
        public string SubstrateID { get; set; }
    }

    public class KeggProduct {
        public string ProductName { get; set; }
        public string ProductID { get; set; }
    }

    public class KeggApi
    {
        private static string prolog = @"http://rest.kegg.jp/get/ec:";
        private static string prourl = @"http://rest.kegg.jp/get/";

        public KeggEnzyme GetSubstrateProductFromEcNumber(string ecNumber) {
            var url = prolog + ecNumber;
            var req = WebRequest.Create(url);
            var res = getWebResponse(req);

            var keggEnzyme = new KeggEnzyme() { EcNumber = ecNumber };
            try {
                using (res) {
                    using (var resStream = res.GetResponseStream()) {
                        using (var sr = new StreamReader(resStream, new UTF8Encoding(false))) {

                            var isSubstrateStarted = false;
                            var isProductStarted = false;

                            while (sr.Peek() > -1) {
                                var line = sr.ReadLine();
                                //Console.WriteLine("Line: {0}", line);

                                if (line.Contains("SUBSTRATE")) isSubstrateStarted = true;
                                if (line.Contains("PRODUCT")) isProductStarted = true;

                                if (isSubstrateStarted) {
                                    if (line[0] != ' ' && !line.Contains("SUBSTRATE")) {
                                        isSubstrateStarted = false;
                                    }
                                    else {
                                        var compoundString = line.Substring(12);
                                        if (!compoundString.Contains("CPD:")) continue;

                                        var compoundName = compoundString.Split(new string[] { "[CPD:" }, StringSplitOptions.None)[0].Trim();
                                        var compoundID = compoundString.Split(new string[] { "[CPD:" }, StringSplitOptions.None)[1].Split(']')[0].Trim();

                                        keggEnzyme.KeggSubstrates.Add(new KeggSubstrate() { SubstrateName = compoundName, SubstrateID = compoundID });

                                        //Console.WriteLine("Substrate compound name {0} and ID {1}", compoundName, compoundID);
                                    }
                                }

                                if (isProductStarted) {
                                    if (line[0] != ' ' && !line.Contains("PRODUCT")) {
                                        isProductStarted = false;
                                    }
                                    else {
                                        var compoundString = line.Substring(12);
                                        if (!compoundString.Contains("CPD:")) continue;

                                        var compoundName = compoundString.Split(new string[] { "[CPD:" }, StringSplitOptions.None)[0].Trim();
                                        var compoundID = compoundString.Split(new string[] { "[CPD:" }, StringSplitOptions.None)[1].Split(']')[0].Trim();
                                        
                                        keggEnzyme.KeggProducts.Add(new KeggProduct() { ProductName = compoundName, ProductID = compoundID });

                                        //Console.WriteLine("Product compound name {0} and ID {1}", compoundName, compoundID);
                                    }
                                }

                            }
                        }
                    }
                }
            }
            catch (System.IO.IOException ex) {
                System.Console.WriteLine("At getPugRestSeviceResult: {0}", ex);
                return null;
            }

            return keggEnzyme;
        }

        public KeggEnzyme GetKeggOrthologiesFromEcNumber(string ecNumber) {
            var url = prolog + ecNumber;
            var req = WebRequest.Create(url);
            var res = getWebResponse(req);

            var keggEnzyme = new KeggEnzyme() { EcNumber = ecNumber };
            try {
                using (res) {
                    using (var resStream = res.GetResponseStream()) {
                        using (var sr = new StreamReader(resStream, new UTF8Encoding(false))) {

                            var isOrthologyStarted = false;

                            while (sr.Peek() > -1) {
                                var line = sr.ReadLine();
                                if (line.Contains("ORTHOLOGY")) isOrthologyStarted = true;

                                if (isOrthologyStarted) {
                                    if (line[0] != ' ' && !line.Contains("ORTHOLOGY")) {
                                        isOrthologyStarted = false;
                                    }
                                    else {

                                        var keggOrthologyID = line.Substring(12, 6);
                                        var keggOrthologyName = line.Substring(20, line.Length - 20);
                                        keggEnzyme.KeggOrthologies.Add(new KeggOrthology() { OrthologyID = keggOrthologyID, OrthologyName = keggOrthologyName });

                                        //Console.WriteLine("Orthology ID {0} and Name {1}", keggOrthologyID, keggOrthologyName);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (System.IO.IOException ex) {
                System.Console.WriteLine("At getPugRestSeviceResult: {0}", ex);
                return null;
            }

            return keggEnzyme;
        }

        // file should include the list of kegg id
        public void ConvertKeggIDsToFormulas(string inputfile) {
            var compoundIDs = new List<string>();
            using (var sr = new StreamReader(inputfile, Encoding.ASCII)) {
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    compoundIDs.Add(line.Trim());
                }
            }

            var outputfile = Path.Combine(System.IO.Path.GetDirectoryName(inputfile), 
                System.IO.Path.GetFileNameWithoutExtension(inputfile) + "-formula.txt");

            var counter = 0;
            var total = compoundIDs.Count;
            using (var sw = new StreamWriter(outputfile, false)) {
                foreach (var id in compoundIDs) {
                    counter++;

                    var url = prourl + id;
                    var req = WebRequest.Create(url);
                    var res = getWebResponse(req);

                    if (res == null) {
                        sw.WriteLine(id + "\t" + "Protocol error");
                    }
                    else {
                        try {
                            using (res) {
                                using (var resStream = res.GetResponseStream()) {
                                    using (var sr = new StreamReader(resStream, new UTF8Encoding(false))) {
                                        var isFormulaFound = false;

                                        while (sr.Peek() > -1) {
                                            var line = sr.ReadLine();
                                            if (line.Contains("FORMULA")) {
                                                var formulaString = line.Substring(12).Trim();
                                                isFormulaFound = true;
                                                sw.WriteLine(id + "\t" + formulaString);
                                                break;
                                            }
                                        }

                                        if (!isFormulaFound) {
                                            sw.WriteLine(id + "\t" + "formula not found");
                                        }
                                    }
                                }
                            }
                        }
                        catch (System.IO.IOException ex) {
                            sw.WriteLine(id + "\t" + "Protocol error");
                        }
                    }

                    Console.Write("Done {0}", counter + "/" + total);
                    Console.SetCursorPosition(0, Console.CursorTop);
                    if (counter % 100 == 0)
                        Thread.Sleep(10000);
                }
            }
        }

        public KeggGene GetKeggOrthologyFromKeggGeneID(string geneID) {
            var url = prourl + geneID;
            var req = WebRequest.Create(url);
            var res = getWebResponse(req);

            var kegggene = new KeggGene() { GeneID = geneID };
            if (res == null) {
                kegggene.KeggOrthology = new KeggOrthology() { OrthologyID = "NA", OrthologyName = "NA" };
                return kegggene;
            }

            try {
                using (res) {
                    using (var resStream = res.GetResponseStream()) {
                        using (var sr = new StreamReader(resStream, new UTF8Encoding(false))) {

                            var isOrthologyStarted = false;

                            while (sr.Peek() > -1) {
                                var line = sr.ReadLine();
                                if (line.Contains("ORTHOLOGY")) isOrthologyStarted = true;

                                if (isOrthologyStarted) {
                                    if (line[0] != ' ' && !line.Contains("ORTHOLOGY")) {
                                        isOrthologyStarted = false;
                                    }
                                    else {

                                        var keggOrthologyID = line.Substring(12, 6);
                                        var keggOrthologyName = line.Substring(20, line.Length - 20);
                                        kegggene.KeggOrthology = new KeggOrthology() { OrthologyID = keggOrthologyID, OrthologyName = keggOrthologyName };

                                        //Console.WriteLine("Orthology ID {0} and Name {1}", keggOrthologyID, keggOrthologyName);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (System.IO.IOException ex) {
                System.Console.WriteLine("At getPugRestSeviceResult: {0}", ex);
                return null;
            }

            return kegggene;
        }



        private WebResponse getWebResponse(WebRequest req) {
            WebResponse res = null;

            try {
                res = req.GetResponse();
            }
            catch (WebException ex) {
                Console.WriteLine("at getWebResponse: status {0}, message {1}", ex.Status, ex.Message);
                res = null;
            }
            finally {
            }
            return res;
        }
    }
}
