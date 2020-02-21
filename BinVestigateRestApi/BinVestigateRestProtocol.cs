using Newtonsoft.Json;
using Rfx.Riken.OsakaUniv;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace Riken.Metabolomics.BinVestigate
{
    [DataContract]
    public class SimilarityRequest
    {
        [DataMember(Name = "spectra")]
        public string spectra { get; set; }
        [DataMember(Name = "minSimilarity")]
        public double minSimilarity { get; set; }
        [DataMember(Name = "kovatsWindows")]
        public double kovatsWindows { get; set; }
        [DataMember(Name = "kovatsRI")]
        public double kovatsRI { get; set; }
    }

    [DataContract]
    public class SimilarityResponse
    {
        [DataMember(Name = "bin")]
        public double bin { get; set; }
        [DataMember(Name = "similarity")]
        public double similarity { get; set; }
    }

    [DataContract]
    public class BinBaseSpectrum
    {
        [DataMember(Name = "id")] // binbase id
        public string id { get; set; }
        [DataMember(Name = "name")] // common name of metabolite
        public string name { get; set; }
        [DataMember(Name = "inchikey")] 
        public string inchikey { get; set; }
        [DataMember(Name = "retentionIndex")] // Fiehn RI
        public double retentionIndex { get; set; }
        [DataMember(Name = "kovats")] //kovats RI
        public double kovats { get; set; }
        [DataMember(Name = "quantMass")] 
        public double quantMass { get; set; }
        [DataMember(Name = "spectra")] 
        public string spectra { get; set; }
        [DataMember(Name = "splash")]
        public string splash { get; set; }
        [DataMember(Name = "purity")]
        public double purity { get; set; }
        [DataMember(Name = "uniqueMass")]
        public double uniqueMass { get; set; }
        [DataMember(Name = "sample")]
        public string sample { get; set; }

        public double BinVestigateSearchSimilarity { get; set; }
        public List<Peak> Peaks { get; set; }
    }

    [DataContract]
    public class BinBaseQuant
    {
        [DataMember(Name = "_id")] // binbase id
        public string id { get; set; }
        [DataMember(Name = "value")] // 
        public Dictionary<string, Dictionary<string, BinBaseCountIntensityPair>> attributes { get; set; }
    }

    public class BinBaseQuantTree : ObservableCollection<BinBaseQuantTree>
    {
        public string ClassName { get; set; }
        public double StudyCount { get; set; }
        public double Intensity { get; set; }
        public double Error { get; set; }
        public string Description { get; set; }
        public BinBaseQuantTree SubClass { get; set; }
    }

    [DataContract]
    public class BinBaseCountIntensityPair
    {
        [DataMember(Name = "count")] // how often does BinBase report this Bin to be found in samples that have the species, organ relationship?
        public string count { get; set; }
        [DataMember(Name = "intensity")] //  "average raw peak intensity" for a specific Bin in a specific species, tissue
        public string intensity { get; set; }
    }

    public class BinVestigateRestProtocol
    {
        private static string prolog = @"https://binvestigate.fiehnlab.ucdavis.edu";

        public List<SimilarityResponse> SimilaritySearch(double ri, double riTol, List<Peak> peaks, double minSimilarity)
        {
            if (peaks == null || peaks.Count == 0) return new List<SimilarityResponse>();
            var url = prolog + "/rest/bin/similarity";
            var spectralString = getSpectralString(peaks);
            var res = string.Empty;
            var result = new List<SimilarityResponse>();
            
            using (var client = new WebClient()) {
                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                client.Encoding = Encoding.UTF8;
                
                //create json client
                var json = getJsonPostRequest(ri, riTol, spectralString, minSimilarity);
                try {
                    res = client.UploadString(url, "POST", json);
                    result = JsonConvert.DeserializeObject<List<SimilarityResponse>>(res);
                }
                catch (WebException ex) {
                    Debug.WriteLine("{0}: {1}", ex.Status, ex.Message);
                }
            }
            return result;
        }

        public BinBaseSpectrum GetBinBaseDiagnosis(int binID)
        {
            var url = prolog + "/rest/bin/" + binID.ToString();

            var req = WebRequest.Create(url);
            var res = getWebResponse(req);
            BinBaseSpectrum result = null;

            if (res == null) return null;

            try {
                using (var sr = new StreamReader(res.GetResponseStream())) {
                    var resString = sr.ReadToEnd();
                    Debug.WriteLine(resString);
                    result = JsonConvert.DeserializeObject<BinBaseSpectrum>(resString);
                }
            }
            catch (System.IO.IOException ex) {
                System.Console.WriteLine(ex);
                return null;
            }
            return result;
        }

        public BinBaseQuantTree GetBinBaseQuantStatisticsResults(int binID)
        {
            var url = prolog + "/rest/bin/classificationTree/" + binID.ToString();

            var req = WebRequest.Create(url);
            var res = getWebResponse(req);
            BinBaseQuant result = null;

            if (res == null) return null;

            try {
                using (var sr = new StreamReader(res.GetResponseStream())) {
                    var resString = sr.ReadToEnd();
                    Debug.WriteLine(resString);
                    result = JsonConvert.DeserializeObject<BinBaseQuant>(resString);
                    if (result == null) return null;
                }
            }
            catch (System.IO.IOException ex) {
                System.Console.WriteLine(ex);
                return null;
            }

            var binbaseQuantTrees = new BinBaseQuantTree();
            var totalIntensity = 0.0;
            var totalCount = 0.0;
            #region converting rest response to BinBaseQuantStatistics Bean
            foreach (var layer in result.attributes) {
                var layerString = layer.Key;
                var layerDict = layer.Value;
                double checkvalue = 0.0;
                if (double.TryParse(layerString, out checkvalue)) continue;

                var layerResult = new BinBaseQuantTree() { ClassName = layerString, SubClass = new BinBaseQuantTree() };
                var subCount = 0.0;
                var subIntensity = 0.0;

                foreach (var subLayer in layerDict) {
                    var subLayerString = subLayer.Key;
                    var subLayerPair = subLayer.Value;
                    var subClassInfo = new BinBaseQuantTree() { ClassName = subLayerString };
                    if (double.TryParse(subLayerPair.count, out checkvalue)) {
                        subClassInfo.StudyCount = checkvalue;
                        subCount += checkvalue;
                    }
                    if (double.TryParse(subLayerPair.intensity, out checkvalue)) {
                        subClassInfo.Intensity = checkvalue;
                        subIntensity += checkvalue;
                    }
                    subClassInfo.Description = subLayerString + " Count: " + subClassInfo.StudyCount + " Intensity: " + subClassInfo.Intensity;
                    layerResult.SubClass.Add(subClassInfo);
                }
                layerResult.StudyCount = subCount;
                layerResult.Intensity = subIntensity;
                layerResult.Description = layerString + " Count: " + layerResult.StudyCount + " Intensity: " + layerResult.Intensity;
                binbaseQuantTrees.Add(layerResult);

                totalIntensity += subIntensity;
                totalCount += subCount;
            }
            binbaseQuantTrees.StudyCount = totalCount;
            binbaseQuantTrees.Intensity = totalIntensity;
            binbaseQuantTrees.Description = binID.ToString() + " Count: " + totalCount + " Intensity: " + totalIntensity;
            #endregion
            return binbaseQuantTrees;
        }

        private string getJsonPostRequest(double ri, double riTol, string spectralString, double minSimilarity)
        {
            var request = new SimilarityRequest() {
                spectra = spectralString,
                kovatsRI = ri,
                kovatsWindows = riTol,
                minSimilarity = minSimilarity
            };
            var json = JsonConvert.SerializeObject(request, Formatting.Indented);
            return json;
        }

        private string getSpectralString(List<Peak> peaks)
        {
            var specString = string.Empty;
            for (int i = 0; i < peaks.Count; i++) {
                var peak = peaks[i];
                if (i == peaks.Count - 1) {
                    specString += Math.Round(peak.Mz, 0).ToString() + ":" + Math.Round(peak.Intensity, 0).ToString();
                }
                else {
                    specString += Math.Round(peak.Mz, 0).ToString() + ":" + Math.Round(peak.Intensity, 0).ToString() + " ";
                }
            }

            return specString;
        }

        private WebResponse getWebResponse(WebRequest req)
        {
            WebResponse res = null;

            try {
                res = req.GetResponse();
            }
            catch (WebException ex) {
                Console.WriteLine("{0}: {1}", ex.Status, ex.Message);
                res = null;
            }
            finally {
            }
            return res;
        }
    }
}
