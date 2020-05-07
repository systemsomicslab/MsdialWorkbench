using CompMs.Common.Components;
using CompMs.Common.Enum;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CompMs.Common.Parser {
    public sealed class MonaMspParser {
        private MonaMspParser() { }

        /// <summary>
        /// This is the MSP file parcer.
        /// Each MS/MS information will be stored in MspFormatCompoundInformationBean.cs.
        /// </summary>
        /// <param name="databaseFilepath"></param>
        /// <returns></returns>
        public static List<MoleculeMsReference> MspFileReader(string databaseFilepath) {
            var mspFields = new List<MoleculeMsReference>();
            var mspField = new MoleculeMsReference();
            var mspPeak = new SpectrumPeak();
            string wkstr;
            int counter = 0, pairCount = 0;

            System.Text.RegularExpressions.Regex r = new System.Text.RegularExpressions.Regex("\"SMILES=.*\"");

            using (var sr = new StreamReader(databaseFilepath, Encoding.ASCII)) {
                float rt = 0, preMz = 0, ri = 0, intensity = 0;

                while (sr.Peek() > -1) {
                    wkstr = sr.ReadLine();
                    if (Regex.IsMatch(wkstr, "^NAME:.*", RegexOptions.IgnoreCase)) {
                        mspField.ScanID = counter;
                        mspField.Name = wkstr.Substring(wkstr.Split(':')[0].Length + 2).Trim();

                        while (sr.Peek() > -1) {
                            wkstr = sr.ReadLine();
                            if (wkstr == string.Empty || String.IsNullOrWhiteSpace(wkstr)) break;
                            if (Regex.IsMatch(wkstr, "Comments.*:.*", RegexOptions.IgnoreCase)) {
                                mspField.Comment = wkstr.Substring(wkstr.Split(':')[0].Length + 2).Trim();

                                for (int i = 0; i < mspField.Comment.Length; i++) {
                                    var charvalue = mspField.Comment[i];
                                    if (charvalue == 'S' && i + 7 <= mspField.Comment.Length - 1) {
                                        var integWord = charvalue.ToString() + mspField.Comment[i + 1].ToString() + mspField.Comment[i + 2].ToString()
                                            + mspField.Comment[i + 3].ToString() + mspField.Comment[i + 4].ToString()
                                            + mspField.Comment[i + 5].ToString() + mspField.Comment[i + 6].ToString();
                                        if (integWord == "SMILES=") {
                                            var smilesString = string.Empty;
                                            for (int j = i + 7; j < mspField.Comment.Length; j++) {
                                                if (mspField.Comment[j] == '"') break;
                                                smilesString += mspField.Comment[j];
                                            }
                                            mspField.SMILES = smilesString;
                                        }
                                       
                                    }
                                }

                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "IONMODE:.*", RegexOptions.IgnoreCase) ||
                                Regex.IsMatch(wkstr, "ion_mode:.*", RegexOptions.IgnoreCase) ||
                                Regex.IsMatch(wkstr, "Ionization:.*", RegexOptions.IgnoreCase)) {
                                if (wkstr.Split(':')[1].Trim() == "Negative" ||
                                    wkstr.Split(':')[1].Trim().Contains("N")) mspField.IonMode = IonMode.Negative;
                                else mspField.IonMode = IonMode.Positive;
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "PRECURSORMZ:.*", RegexOptions.IgnoreCase) || Regex.IsMatch(wkstr, "precursor_m/z:.*", RegexOptions.IgnoreCase)) {
                                if (float.TryParse(wkstr.Split(':')[1].Trim(), out preMz)) mspField.PrecursorMz = preMz;
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "Spectrum_type:.*", RegexOptions.IgnoreCase)) {
                                mspField.Ontology = wkstr.Substring(wkstr.Split(':')[0].Length + 2).Trim(); // temp
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "PRECURSORTYPE:.*", RegexOptions.IgnoreCase) ||
                                Regex.IsMatch(wkstr, "Precursor_type:.*", RegexOptions.IgnoreCase)) {
                                mspField.AdductType = AdductIonParser.GetAdductIonBean(wkstr.Substring(wkstr.Split(':')[0].Length + 2).Trim());
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "DB#:.*", RegexOptions.IgnoreCase)) {
                                mspField.Links = wkstr.Substring(wkstr.Split(':')[0].Length + 2).Trim();
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "INSTRUMENTTYPE:.*", RegexOptions.IgnoreCase) || Regex.IsMatch(wkstr, "Instrument_type:.*", RegexOptions.IgnoreCase)) {
                                mspField.InstrumentType = wkstr.Substring(wkstr.Split(':')[0].Length + 2).Trim();
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "INSTRUMENT:.*", RegexOptions.IgnoreCase)) {
                                mspField.InstrumentModel = wkstr.Substring(wkstr.Split(':')[0].Length + 2).Trim();
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "Collision_energy:.*", RegexOptions.IgnoreCase)) {
                                var collisionenergy = -1.0F;
                                if (float.TryParse(wkstr.Substring(wkstr.Split(':')[0].Length + 2).Trim(), out collisionenergy))
                                    mspField.CollisionEnergy = collisionenergy;
                                continue;
                            }
                            else if (Regex.IsMatch(wkstr, "Num Peaks:.*", RegexOptions.IgnoreCase)) {
                                var peakNum = 0;
                                mspField.Spectrum = MspFileParcer.ReadSpectrum(sr, wkstr, out peakNum);
                                continue;
                            }
                        }
                        mspFields.Add(mspField);
                        mspField = new MoleculeMsReference();
                        counter++;
                    }
                }
            }

            return mspFields;
        }
    }
}
