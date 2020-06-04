using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NCDK.QSAR.Descriptors.Moleculars;
using NCDK.Smiles;
using NCDK.Tools.Manipulator;
using NCDK.Graphs.InChI;
using System.Collections;

namespace CompMs.MspGenerator
{
    public class ExportMSP
    {
        public static void exportMspFile(StreamWriter sw, double precursorMZ, string formula, string name, string smiles, string InChIKey, 
            string adduct, string ionmode, string lipidClass, List<string> flagmentList)
        {
            flagmentList.Sort();

            var peaks = new List<Peak>();

            for(int i = 0;i<flagmentList.Count;i++)
            {
                var mz = Double.Parse(flagmentList[i].Split('\t')[0]);
                var intensity = int.Parse(flagmentList[i].Split('\t')[1]);
                var comment = flagmentList[i].Split('\t')[2];
                if (i == 0) 
                {
                    peaks.Add(new Peak() 
                    { 
                        Mz = mz, 
                        Intensity = intensity ,
                        Comment = comment
                    }); 
                    continue; 
                }
                
                var mz2 = Double.Parse(flagmentList[i-1].Split('\t')[0]);
                var intensity2 = int.Parse(flagmentList[i-1].Split('\t')[1]);

                if (mz == mz2) 
                {
                    peaks.RemoveRange(peaks.Count-1,1);
                    intensity = intensity > intensity2 ? intensity : intensity2;
                    if (intensity > 999) 
                    {
                        intensity = 999;
                    }
                };

                peaks.Add(new Peak()
                {
                    Mz = mz,
                    Intensity = intensity,
                    Comment = comment
                });
            }
            peaks = peaks.OrderBy(n => -n.Mz ).ToList();

            sw.WriteLine(String.Join(": ", new string[] { "NAME", name }));
            sw.WriteLine(String.Join(": ", new string[] { "PRECURSORMZ", precursorMZ.ToString() }));
            sw.WriteLine(String.Join(": ", new string[] { "PRECURSORTYPE", adduct }));
            sw.WriteLine(String.Join(": ", new string[] { "IONMODE", ionmode }));
            sw.WriteLine(String.Join(": ", new string[] { "FORMULA", formula }));
            sw.WriteLine(String.Join(": ", new string[] { "SMILES", smiles }));
            sw.WriteLine(String.Join(": ", new string[] { "INCHIKEY", InChIKey }));
            sw.WriteLine(String.Join(": ", new string[] { "COMPOUNDCLASS", lipidClass }));
            sw.WriteLine(String.Join(": ", new string[] { "RETENTIONTIME", "" }));
            sw.WriteLine(String.Join(": ", new string[] { "COMMENT", "" }));

            //spectrum num
            var numPeaks = peaks.Count.ToString();
            sw.WriteLine(String.Join(": ", new string[] { "Num Peaks", numPeaks }));
            //spectrum list
            foreach (var peak in peaks)
            {
                sw.WriteLine(peak.Mz + "\t" + peak.Intensity 
                    // + "\t\"" + peak.Comment +"\""
                    );
            };
            sw.WriteLine();
        }

        public static void exportMspFile(StreamWriter sw, double precursorMZ, string formula, string name, string smiles, string InChIKey,
            string adduct, string ionmode, string lipidClass, List<string> flagmentList, double logP)
        {
            flagmentList.Sort();

            var peaks = new List<Peak>();

            for (int i = 0; i < flagmentList.Count; i++)
            {
                var mz = Double.Parse(flagmentList[i].Split('\t')[0]);
                var intensity = int.Parse(flagmentList[i].Split('\t')[1]);
                var comment = flagmentList[i].Split('\t')[2];
                if (i == 0)
                {
                    peaks.Add(new Peak()
                    {
                        Mz = mz,
                        Intensity = intensity,
                        Comment = comment
                    }
                    );
                    continue;
                }

                var mz2 = Double.Parse(flagmentList[i - 1].Split('\t')[0]);
                var intensity2 = int.Parse(flagmentList[i - 1].Split('\t')[1]);

                if (mz == mz2)
                {
                    peaks.RemoveRange(peaks.Count - 1, 1);
                    intensity = intensity > intensity2 ? intensity : intensity2;
                    if (intensity > 999)
                    {
                        intensity = 999;
                    }
                };

                peaks.Add(new Peak()
                {
                    Mz = mz,
                    Intensity = intensity,
                    Comment = comment
                });
            }
            peaks = peaks.OrderBy(n => -n.Mz).ToList();

            sw.WriteLine(String.Join(": ", new string[] { "NAME", name }));
            sw.WriteLine(String.Join(": ", new string[] { "PRECURSORMZ", precursorMZ.ToString() }));
            sw.WriteLine(String.Join(": ", new string[] { "PRECURSORTYPE", adduct }));
            sw.WriteLine(String.Join(": ", new string[] { "IONMODE", ionmode }));
            sw.WriteLine(String.Join(": ", new string[] { "FORMULA", formula }));
            sw.WriteLine(String.Join(": ", new string[] { "SMILES", smiles }));
            sw.WriteLine(String.Join(": ", new string[] { "INCHIKEY", InChIKey }));
            sw.WriteLine(String.Join(": ", new string[] { "COMPOUNDCLASS", lipidClass }));
            sw.WriteLine(String.Join(": ", new string[] { "RETENTIONTIME", "" }));
            if (logP != 0.0)
            {
                sw.WriteLine(String.Join(": ", new string[] { "COMMENT", "LogP =" + logP }));
            }
            sw.WriteLine(String.Join(": ", new string[] { "COMMENT", "" }));


            //spectrum num
            var numPeaks = peaks.Count.ToString();
            sw.WriteLine(String.Join(": ", new string[] { "Num Peaks", numPeaks }));
            //spectrum list
            foreach (var peak in peaks)
            {
                sw.WriteLine(peak.Mz + "\t" + peak.Intensity
                    + "\t\"" + peak.Comment + "\""
                    );
            };

            sw.WriteLine();
        }

        public static void fromSMILEStoMsp(string inputFile, string outputFile)
        {
            var smileslist = new List<string>();

            var adducts = new List<string>() { "[M+HCOO]-", "[M-H]-", "[M+CH3COO]-", "[M+H]+", "[M+Na]+", "[M+NH4]+", };
            foreach (var adductIon in adducts)
            {
                var adduct = adductDic.adductIonDic[adductIon];
                var ionmode = adduct.IonMode;
                var fileSurfix = adduct.AdductSurfix + "_" + ionmode.Substring(0, 3);

                using (var sw = new StreamWriter(Path.GetDirectoryName(outputFile) + "\\" + Path.GetFileNameWithoutExtension(outputFile) + "_" + fileSurfix + ".msp", false, Encoding.ASCII))
                {
                    using (var sr = new StreamReader(inputFile, Encoding.ASCII))
                    {
                        var line = "";

                        while ((line = sr.ReadLine()) != null)
                        {
                            var lineArray = line.Split('\t');
                            var name = lineArray[0];
                            var rawSmiles = lineArray[1];

                            name = name + ";" + adduct.AdductIonName;

                            var SmilesParser = new SmilesParser();
                            var SmilesGenerator = new SmilesGenerator(SmiFlavors.Canonical);
                            var iAtomContainer = SmilesParser.ParseSmiles(rawSmiles);
                            var smiles = SmilesGenerator.Create(iAtomContainer);
                            //var smiles2 = SmilesGenerator.Create(iAtomContainer, SmiFlavors.Canonical, new int[iAtomContainer.Atoms.Count]);

                            var InChIGeneratorFactory = new InChIGeneratorFactory();
                            var InChIKey = InChIGeneratorFactory.GetInChIGenerator(iAtomContainer).GetInChIKey();

                            var JPlogPDescriptor = new JPlogPDescriptor();
                            var logP = JPlogPDescriptor.Calculate(iAtomContainer).JLogP;

                            var iMolecularFormula = MolecularFormulaManipulator.GetMolecularFormula(iAtomContainer);
                            var formula = MolecularFormulaManipulator.GetString(iMolecularFormula);
                            var exactMass = MolecularFormulaManipulator.GetMass(iMolecularFormula, MolecularWeightTypes.MonoIsotopic);

                            // flagment
                            var flagmentList = new List<string>();


                            var fla01mass = exactMass + adduct.AdductIonMass;
                            var fla01int = 999;
                            var fla01comment = adduct.AdductIonName;
                            flagmentList.Add(fla01mass + "\t" + fla01int + "\t" + fla01comment);


                            //
                            var precursorMZ = Math.Round(exactMass + adduct.AdductIonMass, 4);
                            ExportMSP.exportMspFile(sw, precursorMZ, formula, name, smiles, InChIKey, adduct.AdductIonName, ionmode, "", flagmentList, logP);

                            smileslist.Add(InChIKey + "\t" + smiles);
                        }
                    }
                }
            }

        }

    }
}
