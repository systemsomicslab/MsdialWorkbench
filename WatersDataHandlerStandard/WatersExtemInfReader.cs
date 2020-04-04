using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace Riken.Metabolomics.WatersDataHandler.Parser
{

    internal class ReadMobCal {
        internal string rawFilePath;
        public string mobcalFilePath;
        internal bool doesMobCalFileExist { get; private set; }

        public double Coefficient { get; set; } = -1.0;
        public double Exponent { get; set; } = -1.0;
        public double T0 { get; set; } = -1.0;

        internal ReadMobCal(string rawFilePath) {
            this.rawFilePath = rawFilePath;
            this.mobcalFilePath = Path.Combine(this.rawFilePath, "mob_cal.csv");
            this.doesMobCalFileExist = File.Exists(this.mobcalFilePath);
            if (!this.doesMobCalFileExist) return;
            this.loadMobCalFile();
        }

        private void loadMobCalFile() {
            using (var sr = new StreamReader(this.mobcalFilePath, Encoding.ASCII)) {
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty) break;
                    if (line.StartsWith("* Coefficient:")) {
                        var coeffString = line.Split(':')[1].Trim();
                        double coeff;
                        if (double.TryParse(coeffString, out coeff))
                            this.Coefficient = coeff;
                    }
                    else if (line.StartsWith("* Exponent:")) {
                        var exponentString = line.Split(':')[1].Trim();
                        double exponent;
                        if (double.TryParse(exponentString, out exponent))
                            this.Exponent = exponent;
                    }
                    else if (line.StartsWith("* t0:")) {
                        var t0String = line.Split(':')[1].Trim();
                        double t0;
                        if (double.TryParse(t0String, out t0))
                            this.T0 = t0;
                    }
                }
            }
        }
    }

    internal class ReadExternInf
    {
        internal string rawFilePath;
        public string extInfFilePath;
        public Dictionary<int, char> functionToPolarity = new Dictionary<int, char>();
        public Dictionary<int, bool> functionIsReference = new Dictionary<int, bool>();
        public Dictionary<int, string> functionToString = new Dictionary<int, string>();
        public Dictionary<int, bool> functionToIsIonMobility = new Dictionary<int, bool>();
        public Dictionary<int, string> functionToCollisionEnergy = new Dictionary<int, string>();
        public Dictionary<int, double[]> functionToMassRange = new Dictionary<int, double[]>();

        internal bool doesExternalInfExist { get; private set; }

        internal ReadExternInf(string rawFilePath) {
            this.rawFilePath = rawFilePath;
            this.extInfFilePath = Path.Combine(this.rawFilePath, "_extern.inf");
            this.doesExternalInfExist = File.Exists(this.extInfFilePath);
            if (!this.doesExternalInfExist) return;
            this.loadExternalInfFile();
        }

        internal char GetPolarity(int functionNumber) {
            if (this.functionToPolarity.ContainsKey(functionNumber)) {
                return this.functionToPolarity[functionNumber];
            }
            else if (this.functionToPolarity.ContainsKey(0)) {
                return this.functionToPolarity[0];
            }
            else {
                throw new ApplicationException("GetPolarity failed. Possibly missing polarity information in _extern.inf");
            }
        }

        internal bool IsReferenceFunction(int functionNumber) {
            bool ret = false;
            if (this.functionIsReference.TryGetValue(functionNumber, out ret)) {
                return ret;
            }
            return false;
        }

        private enum LineStatus { None = 0, BlockHead = 1, BlockContents = 2 }

        private void loadExternalInfFile() {
            using (FileStream fs = new FileStream(this.extInfFilePath, FileMode.Open, FileAccess.Read))
            using (StreamReader sr = new StreamReader(fs, Encoding.UTF8)) {
                LineStatus lineStatus = LineStatus.None;
                string header = "";
                bool isFunctionInfo = false;
                bool isGlobalInfo = false;
                Regex rxFunction = new Regex(@"Function (\w+):");
                Regex rxFunction2 = new Regex(@"Function (\d+)\s+\-\s+(.*?)\s*$");
                Regex rxPolarity = new Regex(@"^Polarity\s+(.*)$");
                Regex rxReference = new Regex(@"^REFERENCE$", RegexOptions.IgnoreCase);
                int functionNo = 0;
                while (0 < sr.Peek()) {
                    string line = sr.ReadLine().Trim();

                    // meta data parser required for ion mobility/mse/collision energy
                    if (line.StartsWith("[MOBILITY]", StringComparison.CurrentCultureIgnoreCase)) {
                        functionToIsIonMobility[functionNo] = true;
                    }
                    if (line.StartsWith("[COLLISION ENERGY]", StringComparison.CurrentCultureIgnoreCase)) {
                        line = sr.ReadLine().Trim();
                        if (line.Contains("Collision Energy (eV)")) {
                            var energy = Regex.Replace(line, @"[^0-9￥.]", "");
                            functionToCollisionEnergy[functionNo] = energy;
                        }
                        else if (line.Contains("Collision Energy Low (eV)")) {
                            var energy = Regex.Replace(line, @"[^0-9￥.]", "");
                            functionToCollisionEnergy[functionNo] = energy;

                            line = sr.ReadLine().Trim();
                            if (line.Contains("Collision Energy High (eV)")) {
                                energy = Regex.Replace(line, @"[^0-9￥.]", "");
                                functionToCollisionEnergy[functionNo] = energy; // ajusted to higher one
                            }
                            continue;
                        }
                        else if (line.Contains("Collision Energy High (eV)")) {
                            var energy = Regex.Replace(line, @"[^0-9￥.]", "");
                            functionToCollisionEnergy[functionNo] = energy;

                            line = sr.ReadLine().Trim();
                            //if (line.Contains("Collision Energy Low (eV)")) {
                            //    energy = Regex.Replace(line, @"[^0-9￥.]", "");
                            //    functionToCollisionEnergy[functionNo] += "-" + energy;
                            //}
                        }
                    }
                    if (line.StartsWith("[PARENT MS SURVEY]", StringComparison.CurrentCultureIgnoreCase)) {
                        line = sr.ReadLine().Trim();
                        var massrange = new double[] { 0, 2000 };
                        double mass;
                        if (line.Contains("Start Mass")) {
                            var massString = Regex.Replace(line, @"[^0-9￥.]", "");
                            if (double.TryParse(massString, out mass)) {
                                massrange[0] = mass;
                                line = sr.ReadLine().Trim();
                                if (line.Contains("End Mass")) {
                                    massString = Regex.Replace(line, @"[^0-9￥.]", "");
                                    if (double.TryParse(massString, out mass)) {
                                        massrange[1] = mass;
                                        functionToMassRange[functionNo] = massrange;
                                        continue;
                                    }
                                }
                            }
                        }
                        else if (line.Contains("End Mass")) {
                            var massString = Regex.Replace(line, @"[^0-9￥.]", "");
                            if (double.TryParse(massString, out mass)) {
                                massrange[1] = mass;
                                line = sr.ReadLine().Trim();
                                if (line.Contains("Start Mass")) {
                                    massString = Regex.Replace(line, @"[^0-9￥.]", "");
                                    if (double.TryParse(massString, out mass)) {
                                        massrange[0] = mass;
                                        functionToMassRange[functionNo] = massrange;
                                        continue;
                                    }
                                }
                            }
                        }
                    }

                    // Console.WriteLine("functionNo {0}, comment {1}", functionNo, line);

                    // set lineStatus
                    if (line == "") {
                        lineStatus = LineStatus.None;
                        isFunctionInfo = isGlobalInfo = false;
                        functionNo = 0;
                    }
                    else if (lineStatus == LineStatus.None) {

                        lineStatus = LineStatus.BlockHead;
                        isFunctionInfo = isGlobalInfo = false;
                        header = line;
                        if (header.StartsWith("Instrument Parameters - Function", StringComparison.CurrentCultureIgnoreCase)) {
                            isFunctionInfo = true;
                            Match mch = rxFunction.Match(line);
                            if (!mch.Success)
                                throw new ApplicationException("Unable to parse Function No from _extern.inf: " + line);
                            functionNo = int.Parse(mch.Groups[1].Value);
                        }
                        else if (header.StartsWith("Function Parameters - Function", StringComparison.CurrentCultureIgnoreCase)) {
                            isFunctionInfo = true;
                            Match mch = rxFunction2.Match(line);
                            if (!mch.Success)
                                throw new ApplicationException("Unable to parse Function No from _extern.inf: " + line);
                            functionNo = int.Parse(mch.Groups[1].Value);
                            string functionDesc = mch.Groups[2].Value;
                            if (!functionToString.ContainsKey(functionNo)) {
                                functionToString[functionNo] = functionDesc;
                                if (functionDesc.StartsWith("MOBILITY", StringComparison.CurrentCultureIgnoreCase)) {
                                    functionToIsIonMobility[functionNo] = true;
                                }
                                else {
                                    functionToIsIonMobility[functionNo] = false;
                                }
                            }

                            if (rxReference.IsMatch(functionDesc)) {
                                this.functionIsReference.Add(functionNo, true);
                            }
                        }
                        else if (header.StartsWith("Experimental Instrument Parameters", StringComparison.CurrentCultureIgnoreCase)) {
                            isGlobalInfo = true;
                        }
                    }
                    else if (lineStatus == LineStatus.BlockHead) {
                        lineStatus = LineStatus.BlockContents;
                    }
                    // read actual contents;
                    if (lineStatus == LineStatus.BlockContents) {
                        if (!isFunctionInfo && !isGlobalInfo) continue;
                        if (line.StartsWith("Polarity", StringComparison.CurrentCultureIgnoreCase)) {
                            Match mch = rxPolarity.Match(line);
                            if (!mch.Success)
                                throw new ApplicationException("Unable to parse Polarity from _extern.inf: " + line);
                            string esPolarity = mch.Groups[1].Value;
                            char polarity = '\0';
                            switch (esPolarity) {
                                case "ES+":
                                case "ESCi+":
                                case "Positive":
                                    polarity = '+';
                                    break;
                                case "ES-":
                                case "ESCi-":
                                case "Negative":
                                    polarity = '-';
                                    break;
                                default:
                                    throw new ApplicationException("Unknown ES Polarity type in _extern.inf: " + esPolarity);
                            }
                            if (isFunctionInfo) {
                                if (functionNo == 0)
                                    throw new ApplicationException("Unable to determine Function No. in _extern.inf");
                                if (this.functionToPolarity.ContainsKey(functionNo)) {
                                    if (this.functionToPolarity[functionNo] != polarity)
                                        throw new ApplicationException("Inconsistent polarity defined for Function " + functionNo.ToString());
                                }
                                else {
                                    this.functionToPolarity.Add(functionNo, polarity);
                                }
                            }
                            else if (isGlobalInfo) {
                                if (this.functionToPolarity.ContainsKey(0)) {
                                    if (this.functionToPolarity[0] != polarity)
                                        throw new ApplicationException("Inconsistent polarity defined as Global Parameter ");
                                }
                                else {
                                    this.functionToPolarity.Add(0, polarity);
                                }
                            }
                        } // end of if line.StartWith("Polarity");
                    } // end of if lineStatus is BlockContents;
                } // end of While(Peak) loop;

                // check if MSE or not
                checkMSEparameters();
            } // end of using streamReader;
        }

        private void checkMSEparameters() {
            if (functionToString.Count() == functionToCollisionEnergy.Count()) {
                var lowestCE = double.MaxValue;
                var lowestCeID = 0;
                foreach (var pair in functionToCollisionEnergy) {
                    var id = pair.Key;
                    var ce = 0.0;
                    if (double.TryParse(pair.Value, out ce)) {
                        if (lowestCE > ce) {
                            lowestCE = ce;
                            lowestCeID = id;
                        }
                    }
                }
                foreach (var pair in functionToCollisionEnergy) {
                    var id = pair.Key;
                    if (id != lowestCeID) {
                        functionToString[id] += " MSMS";
                    }
                }
            }
        }
    }

}
