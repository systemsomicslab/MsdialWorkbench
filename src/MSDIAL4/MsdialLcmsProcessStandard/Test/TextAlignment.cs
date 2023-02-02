using System;
using Rfx.Riken.OsakaUniv;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using Msdial.Lcms.Dataprocess.Algorithm.Clustering;

namespace Msdial.Lcms.Dataprocess.Test
{
	public class FileContent
	{
		public string FilePath { get; set; }
		public string FileName { get; set; }
		public List<PeakSpotInfo> PeakSpots { get; set; }

		public FileContent()
		{
			PeakSpots = new List<PeakSpotInfo>();
		}
	}

	public class PeakSpotInfo
	{
		public string Name { get; set; }
		public double Mz { get; set; }
		public double Rt { get; set; }
		public AdductIon Adduct { get; set; }
		public double Intensity { get; set; }
		public string AnnotationLevel { get; set; }
		public string Formula { get; set; }
		public string Ontology { get; set; }
		public string InChIKey { get; set; }
		public int CarbonCount { get; set; }
        public List<Peak> Ms1Peaks { get; set; }
		public List<Peak> MsmsPeaks { get; set; }

		public PeakSpotInfo() { MsmsPeaks = new List<Peak>(); }
	}

    public class AlignedSpot {
        public int Id { get; set; }
        public PeakSpotInfo PeakSpotInfo { get; set; }
        public Dictionary<string, int> File_SpotID { get; set; }

        public AlignedSpot () { File_SpotID = new Dictionary<string, int>(); }
    }

	public class TextAlignment
	{
        public void Aligner(string[] files, double rtTol, double mzTol, string biotransInput,
            string outputNode, string outputEdge) {

            var smilesPath = @"D:\PROJECT for MSFINDER\Classyfire results\ESD-MINE-MSMS-DB contents.txt";
            var dict_inchikey_smiles = getInChIkeySmilesDictionary(smilesPath);

            //yellow	lime	olive	maroon	purple	red	teal	blue	white	fuchsia	silver navy
            //AC AT  GG GM  GU LE  MT NT  OS ST  ZM OP
            //piechart: colorlist="#FFFF00,#00FF00,#808000,#800000,#800080,#FF0000,#008080,#0000FF,#FFFFFF,#FF00FF,#C0C0C0,#000080" valuelist="0,0,1256,0,2373,0,0,0,0,4784,0"
            //
            var dict_plant_color = new Dictionary<string, string>() {
                { "AT", "lime" },
                { "OS", "white" },
                { "ZM", "silver" },
                { "AC", "yellow" },
                { "NT", "blue" },
                { "OP", "navy" },
                { "MT", "teal" },
                { "GM", "maroon" },
                { "GG", "olive" },
                { "GU", "purple" },
                { "LE", "red" },
                { "ST", "fuchsia" }
            };

            var dict_plant_colorcode = new Dictionary<string, string>() {
                { "AT", "#00FF00" },
                { "OS", "#FFFFFF" },
                { "ZM", "#C0C0C0" },
                { "AC", "#FFFF00" },
                { "NT", "#0000FF" },
                { "OP", "#000080" },
                { "MT", "#008080" },
                { "GM", "#800000" },
                { "GG", "#808000" },
                { "GU", "#800080" },
                { "LE", "#FF0000" },
                { "ST", "#FF00FF" }
            };

            var plantAbbres = new List<string>() {
                "AT", "OS", "ZM", "AC", "NT", "OP", "MT", "GM", "GG", "GU", "LE", "ST"
            };

            var fileContents = new List<FileContent>();
            foreach (var file in files) { //retrieving peak info
                var content = new FileContent() {
                    FilePath = file,
                    FileName = System.IO.Path.GetFileNameWithoutExtension(file),
                    PeakSpots = readFile(file)
                };
                fileContents.Add(content);
            }

            var masterSpots = getPeakSpotInfoMasterList(fileContents, rtTol, mzTol);
            var alignedSpots = new List<AlignedSpot>();
            foreach (var mSpot in masterSpots) { //alignment spot initialization
                var file_id_dict = new Dictionary<string, int>();
                foreach (var file in fileContents) {
                    file_id_dict[file.FileName] = -1;
                }
                alignedSpots.Add(new AlignedSpot() { PeakSpotInfo = mSpot, File_SpotID = file_id_dict });
            }

            foreach (var file in fileContents) { //alignment 
                for (int i = 0; i < file.PeakSpots.Count; i++) {
                    var pSpot = file.PeakSpots[i];
                    var matchedSpotID = findBestMatchedSpotID(pSpot, masterSpots, rtTol, mzTol);
                    if (matchedSpotID >= 0)
                        alignedSpots[matchedSpotID].File_SpotID[file.FileName] = i;
                }
            }

            foreach (var aSpot in alignedSpots) { //set representative peak
                var rSpot = findRepresentativeSpot(aSpot, fileContents);
                aSpot.PeakSpotInfo = rSpot;
            }

            //Node writing
            using (var sw = new StreamWriter(outputNode, false, Encoding.ASCII)) {
                sw.Write("ID\tName\tTitle\tm/z\tRT\tAdduct\tIntensity\tLog intensity\tAnnotation level\tCarbon count\t" + 
                    "Formula\tOntology\tInChIKey\tSMILES\tMs1 spectrum\tMSMS spectrum\tFill %\tPie chart\tCircle\tBoader\t");

                foreach (var plant in plantAbbres) { //for total ion value
                    sw.Write(plant + "\t");
                }

                for (int i = 0; i < fileContents.Count; i++) { //each plant tissue's ion
                    if (i == fileContents.Count - 1)
                        sw.WriteLine(fileContents[i].FileName);
                    else
                        sw.Write(fileContents[i].FileName + "\t");
                }

                var counter = 0;
                foreach (var aSpot in alignedSpots) {
                    var spotInfo = aSpot.PeakSpotInfo;
                    if (spotInfo.Formula == null || spotInfo.Formula == string.Empty || spotInfo.Formula.Contains("||")) spotInfo.Formula = "Unknown";
                    if (spotInfo.AnnotationLevel == "Unknown" && spotInfo.MsmsPeaks.Count == 0) continue;

                    aSpot.Id = counter;

                    var ontologyString = spotInfo.Ontology.Contains("||") ? spotInfo.Ontology.Split('|')[0] : spotInfo.Ontology;
                    if (ontologyString == "NA") ontologyString = "Unknown";

                    var inchikeyString = spotInfo.InChIKey.Contains("||") ? spotInfo.InChIKey.Split('|')[0] : spotInfo.InChIKey;

                    var ms1String = getSpectrumString(spotInfo.Ms1Peaks);
                    var msmsString = getSpectrumString(spotInfo.MsmsPeaks);

                    var title = "RT:" + Math.Round(spotInfo.Rt, 2) + " MZ:" + Math.Round(spotInfo.Mz, 4) +
                        " Formula:" + spotInfo.Formula + " Ontology:" + ontologyString + " InChIKey:" + inchikeyString;

                    var logIntensity = spotInfo.Intensity == 1000000000 ? 3.5 : Math.Log10(spotInfo.Intensity);
                    var smiles = "Unknown";
                    if (inchikeyString != "Unknown") {
                        var shortInChIKey = inchikeyString.Split('-')[0];
                        if (dict_inchikey_smiles.ContainsKey(shortInChIKey)) {
                            smiles = dict_inchikey_smiles[shortInChIKey];
                        }
                        else {
                            Console.WriteLine(shortInChIKey + " not found");
                        }
                    }


                    //var smiles = inchikeyString == "Unknown" ? "Unknown" : dict_inchikey_smiles[inchikeyString.Split('-')[0]];

                    sw.Write(aSpot.Id + "\t" + spotInfo.Name + "\t" + title + "\t" + Math.Round(spotInfo.Mz, 5) + "\t" + Math.Round(spotInfo.Rt, 2) + "\t" + spotInfo.Adduct.AdductIonName + "\t" + spotInfo.Intensity + "\t" + 
                        logIntensity + "\t" + spotInfo.AnnotationLevel + "\t" + spotInfo.CarbonCount + "\t" + spotInfo.Formula + "\t" + ontologyString + "\t" +
                        inchikeyString + "\t" + smiles + "\t" + ms1String + "\t" + msmsString + "\t");

                    var fillCount = 0;
                    var dict_plant_totalion = new Dictionary<string, double>();
                    foreach (var plant in plantAbbres) {
                        dict_plant_totalion[plant] = 0.0;
                    }

                    for (int i = 0; i < fileContents.Count; i++) {
                        var content = fileContents[i];
                        var spotID = aSpot.File_SpotID[content.FileName];
                        if (spotID >= 0) {
                            fillCount++;
                            var intensity = content.PeakSpots[spotID].Intensity;
                            var plantName = content.FileName.Split('_')[0];
                            if (dict_plant_totalion.ContainsKey(plantName))
                                dict_plant_totalion[plantName] += intensity;
                        }
                    }

                    var fillPercent = Math.Round((double)fillCount / (double)fileContents.Count, 3);
                    sw.Write(fillPercent + "\t");

                    var piechart = getPieChartString(dict_plant_colorcode, dict_plant_totalion);
                    var circleType = getCircleType(dict_plant_color, dict_plant_totalion);
                    var boaderType = getBoaderType(aSpot);

                    sw.Write(piechart + "\t" + circleType + "\t" + boaderType + "\t");

                    foreach (var plant in plantAbbres) {
                        sw.Write(dict_plant_totalion[plant] + "\t");
                    }

                    for (int i = 0; i < fileContents.Count; i++) {
                        var content = fileContents[i];
                        var spotID = aSpot.File_SpotID[content.FileName];
                        var spotString = 0.0;
                        if (spotID >= 0) {
                            spotString = content.PeakSpots[spotID].Intensity;
                        }

                        if (i == fileContents.Count - 1) {
                            sw.WriteLine(spotString);
                        }
                        else {
                            sw.Write(spotString + "\t");
                        }
                    }
                    counter++;
                }
            }

            //return;

            var mspQueries = convertToMspObject(alignedSpots);
            var formulaEdges = FormulaClustering.GetEdgeInformations(mspQueries, biotransInput);
            var msmsEdges = MsmsClustering.GetEdgeInformations(mspQueries, 1, 0.025);
            var ontologyEdges = OntologyClustering.GetEdgeInformations(mspQueries);

            using (var sw = new StreamWriter(outputEdge, false, Encoding.ASCII)) {

                sw.WriteLine("source\ttarget\tscore\tsource name\ttarget name\tComment");
                foreach (var edge in formulaEdges) {
                    sw.WriteLine(edge.SourceComment + "\t" + edge.TargetComment + "\t" + edge.Score
                                    + "\t" + edge.SourceName + "\t" + edge.TargetName + "\t" + edge.Comment);
                }

                foreach (var edge in msmsEdges) {
                    sw.WriteLine(edge.SourceComment + "\t" + edge.TargetComment + "\t" + edge.Score
                                    + "\t" + edge.SourceName + "\t" + edge.TargetName + "\t" + edge.Comment);
                }

                foreach (var edge in ontologyEdges) {
                    sw.WriteLine(edge.SourceComment + "\t" + edge.TargetComment + "\t" + edge.Score
                                    + "\t" + edge.SourceName + "\t" + edge.TargetName + "\t" + edge.Comment);
                }
            }
        }

        private string getCircleType(Dictionary<string, string> dict_plant_color,
            Dictionary<string, double> dict_plant_totalion) {
            var maxValue = dict_plant_totalion.Values.Max();
            if (maxValue < 0.1) return "gray";

            foreach (var pair in dict_plant_totalion) {
                var plant = pair.Key;
                var intensity = pair.Value;

                if (Math.Abs(maxValue - intensity) < 0.1) {
                    return dict_plant_color[plant];
                }
            }

            return "Not found";
        }

        private object getPieChartString(Dictionary<string, string> dict_plant_colorcode, 
            Dictionary<string, double> dict_plant_totalion) {
            var pieString = "piechart: colorlist=\"";
            pieString += dict_plant_colorcode["AT"] + ",";
            pieString += dict_plant_colorcode["OS"] + ",";
            pieString += dict_plant_colorcode["ZM"] + ",";
            pieString += dict_plant_colorcode["AC"] + ",";
            pieString += dict_plant_colorcode["NT"] + ",";
            pieString += dict_plant_colorcode["OP"] + ",";
            pieString += dict_plant_colorcode["MT"] + ",";
            pieString += dict_plant_colorcode["GM"] + ",";
            pieString += dict_plant_colorcode["GG"] + ",";
            pieString += dict_plant_colorcode["GU"] + ",";
            pieString += dict_plant_colorcode["LE"] + ",";
            pieString += dict_plant_colorcode["ST"] + "\" valuelist=\"";

            pieString += (int)dict_plant_totalion["AT"] + ",";
            pieString += (int)dict_plant_totalion["OS"] + ",";
            pieString += (int)dict_plant_totalion["ZM"] + ",";
            pieString += (int)dict_plant_totalion["AC"] + ",";
            pieString += (int)dict_plant_totalion["NT"] + ",";
            pieString += (int)dict_plant_totalion["OP"] + ",";
            pieString += (int)dict_plant_totalion["MT"] + ",";
            pieString += (int)dict_plant_totalion["GM"] + ",";
            pieString += (int)dict_plant_totalion["GG"] + ",";
            pieString += (int)dict_plant_totalion["GU"] + ",";
            pieString += (int)dict_plant_totalion["LE"] + ",";
            pieString += (int)dict_plant_totalion["ST"] + "\"";

            ////piechart: colorlist="#FFFF00,#00FF00,#808000,#800000,#800080,#FF0000,#008080,#0000FF,#FFFFFF,#FF00FF,#C0C0C0,#000080" valuelist="0,0,1256,0,2373,0,0,0,0,4784,0"

            return pieString;

        }

        private Dictionary<string, string> getInChIkeySmilesDictionary(string smilesPath) {
            var dict = new Dictionary<string, string>();

            using (var sr = new StreamReader(smilesPath, Encoding.ASCII)) {
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty)
                        continue;
                    var lineArray = line.Split('\t');
                    var shortInChIKey = lineArray[0].Split('-')[0];
                    var smiles = lineArray[1];

                    dict[shortInChIKey] = smiles;
                }
            }

            return dict;
        }

        private string getShapeType(double fillPercent) {
            if (fillPercent > 0.80) return "Up";
            else if (fillPercent < 0.20) return "Down";
            else return "Normal";
        }

        private string getBoaderType(AlignedSpot aSpot) {
            if (aSpot.PeakSpotInfo.AnnotationLevel == "Standard") return "blue";
            else if (aSpot.PeakSpotInfo.AnnotationLevel == "External spectral DBs") return "green";
            else if (aSpot.PeakSpotInfo.AnnotationLevel == "Structure predicted") return "white";
            else if (aSpot.PeakSpotInfo.AnnotationLevel == "Formula predicted") return "pink";
            else if (aSpot.PeakSpotInfo.AnnotationLevel == "Unknown") return "yellow";
            else return "None";
        }

        private string getCircleType(AlignedSpot aSpot, List<FileContent> fileContents) {
            var atFound = false;
            var osFound = false;

            for (int i = 0; i < fileContents.Count; i++) {
                var content = fileContents[i];
                var spotID = aSpot.File_SpotID[content.FileName];
                if (content.FileName.Contains("AT_") && spotID >= 0)
                    atFound = true;
                if (content.FileName.Contains("OS_") && spotID >= 0)
                    osFound = true;
            }

            if (atFound == false && osFound == false) return "Gray";
            else if (atFound == true && osFound == false) return "Red";
            else if (atFound == false && osFound == true) return "White";
            else if (atFound == true && osFound == true) return "Orange";
            else return "Gray";
        }

        private List<MspFormatCompoundInformationBean> convertToMspObject(List<AlignedSpot> alignedSpots) {
            var mspQueries = new List<MspFormatCompoundInformationBean>();

            foreach (var spot in alignedSpots) {
                var query = new MspFormatCompoundInformationBean() {
                    Name = spot.PeakSpotInfo.Name,
                    AdductIonBean = AdductIonStringParser.GetAdductIonBean(spot.PeakSpotInfo.Adduct.AdductIonName),
                    Id = spot.Id,
                    Comment = spot.Id.ToString(),
                    Formula = spot.PeakSpotInfo.Formula,
                    InchiKey = spot.PeakSpotInfo.InChIKey,
                    Ontology = spot.PeakSpotInfo.Ontology,
                    RetentionTime = (float)spot.PeakSpotInfo.Rt,
                    PrecursorMz = (float)spot.PeakSpotInfo.Mz,
                    PeakNumber = spot.PeakSpotInfo.MsmsPeaks.Count,
                    MzIntensityCommentBeanList = convertToMzIntCommentBeans(spot.PeakSpotInfo.MsmsPeaks)
                };

                mspQueries.Add(query);
            }

            return mspQueries;
        }

        private List<MzIntensityCommentBean> convertToMzIntCommentBeans(List<Peak> peaks) {
            var peakBeans = new List<MzIntensityCommentBean>();
            if (peaks == null || peaks.Count == 0) return peakBeans;
            foreach (var peak in peaks) {
                var bean = new MzIntensityCommentBean() {
                    Mz = (float)peak.Mz,
                    Intensity = (float)peak.Intensity
                };
                peakBeans.Add(bean);
            }
            return peakBeans;
        }

        private string getSpectrumString(List<Peak> peaks) {
            if (peaks == null || peaks.Count == 0) return string.Empty;
            var peakString = string.Empty;
            for (int i = 0; i < peaks.Count; i++) {
                if (i == peaks.Count - 1)
                    peakString += Math.Round(peaks[i].Mz, 5).ToString() + ":" + ((int)peaks[i].Intensity).ToString();
                else
                    peakString += Math.Round(peaks[i].Mz, 5).ToString() + ":" + ((int)peaks[i].Intensity).ToString() + " ";
            }
            return peakString;
        }

        private PeakSpotInfo findRepresentativeSpot(AlignedSpot aSpot, List<FileContent> fileContents) {

            var repSpot = aSpot.PeakSpotInfo;

            foreach (var pair in aSpot.File_SpotID) {
                var filename = pair.Key;
                var spotID = pair.Value;
                if (spotID == -1) continue;

                foreach (var content in fileContents) {
                    if (content.FileName == filename) {
                        var spotProp = content.PeakSpots[spotID];
                        switch (spotProp.AnnotationLevel) {
                            case "Standard":
                                #region
                                switch (repSpot.AnnotationLevel) {

                                    case "Standard":

                                        if (spotProp.Intensity > repSpot.Intensity)
                                            repSpot = spotProp;

                                        break;
                                    case "External spectral DBs":

                                        repSpot = spotProp;

                                        break;
                                    case "Structure predicted":

                                        repSpot = spotProp;

                                        break;
                                    case "Ontology predicted":

                                        repSpot = spotProp;

                                        break;
                                    case "Formula predicted":

                                        repSpot = spotProp;

                                        break;

                                    case "Carbon shift confirmed":

                                        repSpot = spotProp;

                                        break;

                                    case "Unknown":

                                        repSpot = spotProp;

                                        break;
                                }
                                #endregion
                                break;
                            case "External spectral DBs":
                                #region
                                switch (repSpot.AnnotationLevel) {
                                    case "Standard":
                                        break;
                                    case "External spectral DBs":

                                        if (spotProp.Intensity > repSpot.Intensity)
                                            repSpot = spotProp;

                                        break;
                                    case "Structure predicted":
                                        repSpot = spotProp;
                                        break;
                                    case "Ontology predicted":
                                        repSpot = spotProp;
                                        break;
                                    case "Formula predicted":
                                        repSpot = spotProp;
                                        break;
                                    case "Carbon shift confirmed":
                                        repSpot = spotProp;
                                        break;
                                    case "Unknown":
                                        repSpot = spotProp;
                                        break;
                                }
                                #endregion
                                break;
                            case "Structure predicted":
                                #region
                                switch (repSpot.AnnotationLevel) {
                                    case "Standard":
                                        break;
                                    case "External spectral DBs":
                                        break;
                                    case "Structure predicted":

                                        if (countChar(spotProp.Formula, '|') < countChar(repSpot.Formula, '|'))
                                            repSpot = spotProp;
                                        else if (countChar(spotProp.Formula, '|') == countChar(repSpot.Formula, '|') 
                                            && spotProp.Intensity > repSpot.Intensity)
                                            repSpot = spotProp;

                                        break;
                                    case "Ontology predicted":
                                        if (countChar(spotProp.Formula, '|') < countChar(repSpot.Formula, '|'))
                                            repSpot = spotProp;
                                        else if (countChar(spotProp.Formula, '|') == countChar(repSpot.Formula, '|'))
                                            repSpot = spotProp;
                                        break;
                                    case "Formula predicted":
                                        if (countChar(spotProp.Formula, '|') < countChar(repSpot.Formula, '|'))
                                            repSpot = spotProp;
                                        else if (countChar(spotProp.Formula, '|') == countChar(repSpot.Formula, '|'))
                                            repSpot = spotProp;
                                        break;
                                    case "Carbon shift confirmed":
                                        repSpot = spotProp;
                                        break;
                                    case "Unknown":
                                        repSpot = spotProp;
                                        break;
                                }
                                #endregion
                                break;
                            case "Ontology predicted":
                                #region
                                switch (repSpot.AnnotationLevel) {
                                    case "Standard":
                                        break;
                                    case "External spectral DBs":
                                        break;
                                    case "Structure predicted":
                                        if (countChar(spotProp.Formula, '|') < countChar(repSpot.Formula, '|'))
                                            repSpot = spotProp;
                                        break;
                                    case "Ontology predicted":
                                        if (countChar(spotProp.Formula, '|') < countChar(repSpot.Formula, '|'))
                                            repSpot = spotProp;
                                        else if (countChar(spotProp.Formula, '|') == countChar(repSpot.Formula, '|')
                                            && spotProp.Intensity > repSpot.Intensity)
                                            repSpot = spotProp;
                                        break;
                                    case "Formula predicted":
                                        if (countChar(spotProp.Formula, '|') < countChar(repSpot.Formula, '|'))
                                            repSpot = spotProp;
                                        else if (countChar(spotProp.Formula, '|') == countChar(repSpot.Formula, '|'))
                                            repSpot = spotProp;
                                        break;
                                    case "Carbon shift confirmed":
                                        repSpot = spotProp;
                                        break;
                                    case "Unknown":
                                        repSpot = spotProp;
                                        break;
                                }
                                #endregion
                                break;
                            case "Formula predicted":
                                #region
                                switch (repSpot.AnnotationLevel) {
                                    case "Standard":
                                        break;
                                    case "External spectral DBs":
                                        break;
                                    case "Structure predicted":
                                        if (countChar(spotProp.Formula, '|') < countChar(repSpot.Formula, '|'))
                                            repSpot = spotProp;
                                        break;
                                    case "Ontology predicted":
                                        if (countChar(spotProp.Formula, '|') < countChar(repSpot.Formula, '|'))
                                            repSpot = spotProp;
                                        break;
                                    case "Formula predicted":

                                        if (countChar(spotProp.Formula, '|') < countChar(repSpot.Formula, '|'))
                                            repSpot = spotProp;
                                        else if (countChar(spotProp.Formula, '|') == countChar(repSpot.Formula, '|')
                                            && spotProp.Intensity > repSpot.Intensity)
                                            repSpot = spotProp;

                                        break;
                                    case "Carbon shift confirmed":
                                        repSpot = spotProp;
                                        break;
                                    case "Unknown":
                                        repSpot = spotProp;
                                        break;
                                }
                                #endregion
                                break;

                            case "Carbon shift confirmed":
                                #region
                                switch (repSpot.AnnotationLevel) {
                                    case "Standard":
                                        break;
                                    case "External spectral DBs":
                                        break;
                                    case "Structure predicted":
                                        break;
                                    case "Ontology predicted":
                                        break;
                                    case "Formula predicted":
                                        if (repSpot.MsmsPeaks.Count == 0 && spotProp.MsmsPeaks.Count > 0)
                                            repSpot = spotProp;
                                        else if (spotProp.Intensity > repSpot.Intensity)
                                            repSpot = spotProp;
                                        break;
                                    case "Unknown":
                                        repSpot = spotProp;

                                        break;
                                }
                                #endregion
                                break;
                            case "Unknown":
                                #region
                                switch (repSpot.AnnotationLevel) {
                                    case "Standard":
                                        break;
                                    case "External spectral DBs":
                                        break;
                                    case "Structure predicted":
                                        break;
                                    case "Ontology predicted":
                                        break;
                                    case "Formula predicted":
                                        break;
                                    case "Carbon shift confirmed":
                                        break;
                                    case "Unknown":
                                        if (repSpot.MsmsPeaks.Count == 0 && spotProp.MsmsPeaks.Count > 0)
                                            repSpot = spotProp;
                                        else if (spotProp.Intensity > repSpot.Intensity)
                                            repSpot = spotProp;

                                        break;
                                }
                                #endregion
                                break;
                        }
                    }
                }
            }

            return repSpot;                 
        }

        public int countChar(string s, char c) {
            return s.Length - s.Replace(c.ToString(), "").Length;
        }

        /// <summary>
        /// Gets the peak spot info master list.
        /// The master list ordering by mz values will be returned. 
        /// </summary>
        private List<PeakSpotInfo> getPeakSpotInfoMasterList(List<FileContent> fileContents, double rtTol, double mzTol)
		{
            var masterSpots = new List<PeakSpotInfo>();
            foreach(var spot in fileContents[0].PeakSpots) {
                if(spot.Intensity > 1000 && spot.CarbonCount > 0)
                    masterSpots.Add(spot);
            }
			for (int i = 1; i < fileContents.Count; i++)
			{
				var peakSpots = fileContents[i].PeakSpots;
				var addSpots = new List<PeakSpotInfo>();

				foreach (var pSpot in peakSpots)
				{
                    if (pSpot.Intensity > 1000 && pSpot.CarbonCount > 0) {
                        var flg = false;
                        foreach (var mSpot in masterSpots) {
                            if (isMatchedSpot(pSpot, mSpot, rtTol, mzTol)) {
                                flg = true; break;
                            }
                        }
                        if (flg == false)
                            addSpots.Add(pSpot);
                    }
				}

				foreach (var aSpot in addSpots) masterSpots.Add(aSpot);
			}
			return masterSpots.OrderBy(n => n.Mz).ToList();
		}

		/// <summary>
		/// Finds the best matched spot. Master spots must be ordered by mz values
		/// </summary>
		private int findBestMatchedSpotID(PeakSpotInfo targetSpot, List<PeakSpotInfo> masterSpots, double rtTol, double mzTol)
		{
			var startID = getStartIndex(targetSpot.Mz, mzTol, masterSpots);

			var maxScore = double.MinValue;
			var maxID = -1;
			for (int i = startID; i < masterSpots.Count; i++)
			{
				var refSpot = masterSpots[i];
				if (Math.Abs(targetSpot.Rt - refSpot.Rt) > rtTol) continue;
				if (Math.Abs(targetSpot.Mz - refSpot.Mz) > mzTol) continue;
                if (targetSpot.CarbonCount > 0 && refSpot.CarbonCount > 0) {
                    if (targetSpot.CarbonCount != refSpot.CarbonCount) continue;
                }
				if (targetSpot.Mz + mzTol < refSpot.Mz) break;

				var rtSim = Scoring.LcmsScoring.GetGaussianSimilarity((float)targetSpot.Rt, (float)refSpot.Rt, (float)rtTol);
				var mzSim = Scoring.LcmsScoring.GetGaussianSimilarity((float)targetSpot.Mz, (float)refSpot.Mz, (float)mzTol);

				var totalSim = rtSim + mzSim;
				if (maxScore < totalSim)
				{
					maxScore = totalSim;
					maxID = i;
				}
			}

			return maxID;
		}

		private bool isMatchedSpot(PeakSpotInfo spot1, PeakSpotInfo spot2, double rtTol, double mzTol)
		{
			if (Math.Abs(spot1.Rt - spot2.Rt) > rtTol) return false;
			if (Math.Abs(spot1.Mz - spot2.Mz) > mzTol) return false;
            if (spot1.CarbonCount < 0 || spot2.CarbonCount < 0) return false;
			if (spot1.CarbonCount != spot2.CarbonCount) return false;

			return true;
		}

		private List<PeakSpotInfo> readFile(string file)
		{
			var peakSpots = new List<PeakSpotInfo>();

			using (var sr = new StreamReader(file, Encoding.ASCII))
			{
				sr.ReadLine();
				while (sr.Peek() > -1)
				{
					#region

					var line = sr.ReadLine();
					if (line == string.Empty)
						continue;

					var lineArray = line.Split('\t');

					var title = lineArray[0];
					var mz = -1.0; double.TryParse(lineArray[1], out mz);
					var rt = -1.0; double.TryParse(lineArray[2], out rt);
					var adduct = AdductIonParcer.GetAdductIonBean(lineArray[3]);
					var intensity = -1.0; double.TryParse(lineArray[4], out intensity);
					var annotationlevel = lineArray[5];
					var carbon = -1; int.TryParse(lineArray[6], out carbon);
					var formula = lineArray[7];
					var ontology = lineArray[8];
					var inchikey = lineArray[9];
                    var ms1Peaks = new List<Peak>();
					var msmsPeaks = new List<Peak>();

					if (carbon < 0) continue;
                    //if (intensity < 1000) continue;

                    if (lineArray[10].Trim() != string.Empty) {
                        var peakPairs = lineArray[10].Trim().Split(' ');
                        foreach (var pair in peakPairs) {
                            if (!pair.Contains(":")) continue;
                            var mzString = pair.Split(':')[0];
                            var intString = pair.Split(':')[1];

                            var mzValue = -1.0; double.TryParse(mzString, out mzValue);
                            var intValue = -1.0; double.TryParse(intString, out intValue);

                            if (mzValue > 0 && intValue > 0) {
                                ms1Peaks.Add(new Peak() { Mz = mzValue, Intensity = intValue });
                            }
                        }
                    }

                    if (lineArray[11].Trim() != string.Empty)
					{
						var peakPairs = lineArray[11].Trim().Split(' ');
						foreach (var pair in peakPairs)
						{
							if (!pair.Contains(":")) continue;
							var mzString = pair.Split(':')[0];
							var intString = pair.Split(':')[1];

							var mzValue = -1.0; double.TryParse(mzString, out mzValue);
							var intValue = -1.0; double.TryParse(intString, out intValue);

							if (mzValue > 0 && intValue > 0)
							{
								msmsPeaks.Add(new Peak() { Mz = mzValue, Intensity = intValue });
							}
						}
					}

					var spotInfo = new PeakSpotInfo()
					{
						Name = title,
						Mz = mz,
						Rt = rt,
						Intensity = intensity,
						Adduct = adduct,
						AnnotationLevel = annotationlevel,
						CarbonCount = carbon,
						Formula = formula,
						InChIKey = inchikey,
						Ontology = ontology,
                        Ms1Peaks = ms1Peaks,
						MsmsPeaks = msmsPeaks
					};

					peakSpots.Add(spotInfo);
					#endregion
				}
			}
			return peakSpots.OrderBy(n => n.Mz).ToList();
		}

		/// <summary>
		/// Gets the start index.
		/// Peak spots must be ordered by mz values
		/// </summary>
		private int getStartIndex(double mass, double mzTol, List<PeakSpotInfo> peakSpots)
		{
			double targetMass = mass - mzTol;
			int startIndex = 0, endIndex = peakSpots.Count - 1;
			if (targetMass > peakSpots[endIndex].Mz) return endIndex;

			int counter = 0;
			while (counter < 5)
			{
				if (peakSpots[startIndex].Mz <= targetMass && targetMass < peakSpots[(startIndex + endIndex) / 2].Mz)
				{
					endIndex = (startIndex + endIndex) / 2;
				}
				else if (peakSpots[(startIndex + endIndex) / 2].Mz <= targetMass && targetMass < peakSpots[endIndex].Mz)
				{
					startIndex = (startIndex + endIndex) / 2;
				}
				counter++;
			}
			return startIndex;
		}
	}
}
