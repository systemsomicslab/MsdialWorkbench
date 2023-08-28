using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.Common.PugRestApiStandard
{
    public class PugRestProtocol
    {
        private static string prolog = @"http://pubchem.ncbi.nlm.nih.gov/rest/pug";
        private static string inputFormula = @"/compound/formula/";
        private static string inputListKey = @"/compound/listkey/";
        private static string inputCid = @"/compound/cid/";
        private string formula;

        private Timer timer;
        private PubResponse result;
        private Dictionary<string, string> restUrl_filepath_Dcit;

        private enum OutcomeType { Success, Timeout, Error };

        public bool SearchSdfByFormula(string formula, string downloadFolderPath, int maxRecords, List<int> excludePubCIDs)
        {
            this.formula = formula;
            if (maxRecords < 0) return false;

            var url = prolog + inputFormula + formula + "/JSON";
            var req = WebRequest.Create(url);
            var res = getWebResponse(req);

            if (res == null) return false;

            setPubRestCids(res);

            if (this.result == null || this.result.IdentifierList == null || this.result.IdentifierList.CID.Count == 0) return false;

            setPubRestInChIKeys();

            if (this.result.PropertyTable == null || this.result.PropertyTable.Properties == null || this.result.PropertyTable.Properties.Count == 0) return false;

            this.restUrl_filepath_Dcit = getPugRestCidsUrlDictionary(this.result, downloadFolderPath, maxRecords, excludePubCIDs);

            Parallel.ForEach(this.restUrl_filepath_Dcit, dict =>
            {
                DownloadClient.DownloadSdf(dict.Key, dict.Value);
            });

            return true;
        }

        private void setPubRestInChIKeys()
        {
            var cids = this.result.IdentifierList.CID[0].ToString();
            var counter = 1;

            this.result.PropertyTable = new PropertyTable();

            for (int i = 0; i < this.result.IdentifierList.CID.Count; i++)
            {
                if (i != 0)
                {
                    cids += "," + this.result.IdentifierList.CID[i].ToString();
                    counter++;
                }

                if (counter == 100 || i == this.result.IdentifierList.CID.Count - 1)
                {
                    var url = prolog + inputCid + cids + "/property/InChIKey/JSON";
                    var req = WebRequest.Create(url);
                    var res = getWebResponse(req);

                    if (res == null) return;

                    var properties = getPubRestInChIKeys(res);
                    if (properties == null) return;
                    mergeInchiList(this.result.PropertyTable, properties);

                    if (i != this.result.IdentifierList.CID.Count - 1) cids = this.result.IdentifierList.CID[i + 1].ToString();
                    counter = 0;
                }
            }
        }

        private void mergeInchiList(PropertyTable propertyTable, List<Properties> properties)
        {
            foreach (var property in properties)
            {
                propertyTable.Properties.Add(property);
            }
        }

        private List<Properties> getPubRestInChIKeys(WebResponse res)
        {
            var result = getPugRestSeviceResult(res);
            if (result == null) return null;

            var maxTrial = 2.5;
            var trialCount = 0;
            while (this.result.Waiting != null && trialCount < maxTrial)
            {
                //Thread.Sleep(2000);
                trialCount++;
                var listKeyUrl = prolog + inputListKey + result.Waiting.ListKey + "/inchikey/JSON";
                if (!listKeySearch(listKeyUrl, result)) { return null; }
                else return result.PropertyTable.Properties;
            }
            return result.PropertyTable.Properties;
        }

        private bool listKeySearch(string url, PubResponse result)
        {
            var req = WebRequest.Create(url);
            var res = getWebResponse(req);

            if (res == null)
            {
                return false;
            }

            result = getPugRestSeviceResult(res);
            if (result == null) return false;

            return true;
        }

        private void setPubRestCids(WebResponse res)
        {
            this.result = getPugRestSeviceResult(res);
            if (this.result == null) return;

            var maxTrial = 2.5;
            var trialCount = 0;
            while (this.result.Waiting != null && trialCount < maxTrial)
            {
                //while (this.result.Waiting != null) {
                //Thread.Sleep(2000); 
                trialCount++;

                var listKeyUrl = prolog + inputListKey + result.Waiting.ListKey + "/cids/JSON";
                if (!listKeySearch(listKeyUrl))
                {
                    if (this.result.Waiting != null) continue;
                    this.result.IdentifierList = null;
                    return;
                };
            }
        }

        private PubResponse getPubRestSynonyms(WebResponse res)
        {
            if (res == null) return null;
            var pubResult = getPugRestSeviceResult(res);
            if (pubResult == null) return null;

            var maxTrial = 2.5;
            var trialCount = 0;
            while (pubResult.Waiting != null && trialCount < maxTrial)
            {
                //while (pubResult.Waiting != null) {
                //Thread.Sleep(2000); 
                trialCount++;
                var listKeyUrl = prolog + inputListKey + result.Waiting.ListKey + "/synonyms/JSON";
                if (!listKeySearch(listKeyUrl, pubResult))
                {
                    if (pubResult.Waiting != null) continue;
                    return pubResult;
                };
            }

            return pubResult;
        }

        private bool listKeySearch(string url, out PubResponse pubResponse)
        {
            var req = WebRequest.Create(url);
            var res = getWebResponse(req);

            if (res == null)
            {
                pubResponse = null;
                return false;
            }

            pubResponse = getPugRestSeviceResult(res);
            if (pubResponse == null) return false;
            return true;
        }

        private bool listKeySearch(string url)
        {
            var req = WebRequest.Create(url);
            var res = getWebResponse(req);

            if (res == null)
            {
                return false;
            }

            this.result = getPugRestSeviceResult(res);
            if (this.result == null) return false;
            return true;
        }

        private PubResponse getPugRestSeviceResult(WebResponse res)
        {
            PubResponse result = null;

            try
            {
                using (res)
                {
                    using (var resStream = res.GetResponseStream())
                    {
                        var serializer = new DataContractJsonSerializer(typeof(PubResponse));
                        result = (PubResponse)serializer.ReadObject(resStream);
                    }
                }
            }
            catch (System.IO.IOException ex)
            {
                System.Console.WriteLine("Formula: {0}, Error: {1}", this.formula, ex.Message);
                return null;
            }

            return result;
        }

        private WebResponse getWebResponse(WebRequest req)
        {
            WebResponse res = null;

            try
            {
                res = req.GetResponse();
            }
            catch (WebException ex)
            {
                Console.WriteLine("Formula: {0}, Status: {1}, Message: {2}", this.formula, ex.Status, ex.Message);
                res = null;
            }
            catch (System.OperationCanceledException ex)
            {
                Console.WriteLine("Formula: {0}, Status: {1}, Message: {2}", this.formula, ex.HResult, ex.Message);
                res = null;
            }
            finally
            {
            }
            return res;
        }

        private Dictionary<string, string> getPugRestCidsUrlDictionary(PubResponse result, string folderPath, int maxRecords, List<int> excludePubCIDs)
        {
            setSynonymsProperty(result);

            var rest_file_Dcit = new Dictionary<string, string>();
            var properties = getCuratedProperties(result.PropertyTable.Properties);
            var url = prolog + @"/compound/cid/";

            int counter = 0, fileID = 0;

            properties = properties.OrderByDescending(n => n.SynonymsNumber).ThenBy(n => n.CID).ToList();

            for (int i = 0; i < properties.Count; i++)
            {
                if (excludePubCIDs.Contains(int.Parse(properties[i].CID))) continue;
                if (i >= maxRecords) break;

                url += properties[i].CID.ToString() + ",";
                counter++;

                if (counter == 20)
                {
                    url = url.Substring(0, url.Length - 1) + "/SDF";
                    rest_file_Dcit[url] = Path.Combine(folderPath, fileID.ToString() + ".sdf");

                    url = prolog + @"/compound/cid/";
                    fileID++;
                    counter = 0;
                }
            }

            if (url != prolog + @"/compound/cid/")
            {
                url = url.Substring(0, url.Length - 1) + "/SDF";
                rest_file_Dcit[url] = Path.Combine(folderPath, fileID.ToString() + ".sdf");
            }

            return rest_file_Dcit;
        }

        private void setSynonymsProperty(PubResponse result)
        {
            var properties = result.PropertyTable.Properties;
            var url = prolog + @"/compound/cid/";
            var counter = 0;
            var startID = 0;
            var endID = 0;

            for (int i = 0; i < properties.Count; i++)
            {
                url += properties[i].CID.ToString() + ",";
                counter++;

                if (counter == 20)
                {
                    url = url.Substring(0, url.Length - 1) + "/synonyms/JSON";
                    endID = i;

                    var req = WebRequest.Create(url);
                    var res = getWebResponse(req);
                    var pubResult = getPubRestSynonyms(res);

                    setPubRestSynonyms(properties, pubResult, startID, endID);

                    url = prolog + @"/compound/cid/";
                    counter = 0;
                    startID = i + 1;
                }
            }

            if (url != prolog + @"/compound/cid/")
            {
                url = url.Substring(0, url.Length - 1) + "/synonyms/JSON";
                endID = properties.Count - 1;

                var req = WebRequest.Create(url);
                var res = getWebResponse(req);
                var pubResult = getPubRestSynonyms(res);

                setPubRestSynonyms(properties, pubResult, startID, endID);
            }
        }

        private void setPubRestSynonyms(List<Properties> properties, PubResponse pubResult, int startID, int endID)
        {
            if (pubResult == null) return;
            if (pubResult.InformationList == null) return;
            if (pubResult.InformationList.Information == null) return;
            if (pubResult.InformationList.Information.Count == 0) return;

            for (int i = startID; i <= endID; i++)
            {
                if (pubResult.InformationList.Information[i - startID].Synonym == null) continue;
                properties[i].SynonymsNumber = pubResult.InformationList.Information[i - startID].Synonym.Length;
            }
        }

        private List<Properties> getCuratedProperties(List<Properties> properties)
        {
            properties = properties.OrderBy(n => n.InChIKey).ThenByDescending(n => n.SynonymsNumber).ToList();

            var clist = new List<Properties>() { properties[0] };

            for (int i = 1; i < properties.Count; i++)
            {
                var shortInchi = properties[i].InChIKey.Substring(0, 14);
                var cShortInchi = clist[clist.Count - 1].InChIKey.Substring(0, 14);
                if (shortInchi == cShortInchi)
                {
                    if (clist[clist.Count - 1].SynonymsNumber < properties[i].SynonymsNumber)
                        clist[clist.Count - 1] = properties[i];
                    continue;
                }
                else
                {
                    clist.Add(properties[i]);
                }
            }
            return clist;
        }
    }
}
