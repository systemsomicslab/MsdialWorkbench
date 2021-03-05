using Riken.Metabolomics.StructureFinder.Parser;
using Riken.Metabolomics.StructureFinder.Result;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Rfx.Riken.OsakaUniv
{
    public sealed class MassBankFragmentAssigner
    {
        public static void Fragmentasigner()
        {
            var mspPosiFiles = System.IO.Directory.GetFiles(@"C:\Users\tensa_000\Desktop\MassBank TK\MassBank-R170-pos-MSMS-valset-HTcurate", "*.msp", System.IO.SearchOption.TopDirectoryOnly);
            var mspNegaFiles = System.IO.Directory.GetFiles(@"C:\Users\tensa_000\Desktop\MassBank TK\MassBank-R170-neg-MSMS-valset-HTcurate", "*.msp", System.IO.SearchOption.TopDirectoryOnly);

            var analysisParam = new AnalysisParamOfMsfinder();

            //makeMspFile(mspPosiFiles);
            //makeMspFile(mspNegaFiles);
            //runFragmentAssigner(mspPosiFiles, analysisParam);
            //runFragmentAssigner(mspNegaFiles, analysisParam);
        }

        //private static void runFragmentAssigner(string[] files, AnalysisParamOfMsfinder param)
        //{
        //    Parallel.ForEach(files, file =>
        //    {
        //        var sdfFile = System.IO.Path.GetDirectoryName(file) + "\\" + System.IO.Path.GetFileNameWithoutExtension(file) + ".sdf";
        //        var outputFile = System.IO.Path.GetDirectoryName(file) + "\\" + System.IO.Path.GetFileNameWithoutExtension(file) + ".sfd";
        //        var rawData = RawDataParcer.RawDataFileReader(file, param);

        //        var adductIon = AdductIonParcer.GetAdductIonBean(rawData.PrecursorType);
        //        var centroidSpectrum = FragmentAssigner.GetCentroidMsMsSpectrum(rawData);
        //        var refinedPeaklist = FragmentAssigner.GetRefinedPeaklist(centroidSpectrum, param.RelativeAbundanceCutOff, rawData.PrecursorMz, param.Mass2Tolerance, param.MassTolType);

        //        var results = InSilicoFragmenter.MainFragmenter(sdfFile, rawData.PrecursorMz, refinedPeaklist, adductIon.IonMode, param.Mass2Tolerance, param.MassTolType, param.TreeDepth, adductIon, CancellationToken.None);
        //        FragmenterResultParcer.FragmenterResultWriter(outputFile, results, false);
        //    });
        //}

        private static void makeMspFile(string[] files, AnalysisParamOfMsfinder param)
        {
            Parallel.ForEach(files, file =>
            {
                var rawData = RawDataParcer.RawDataFileReader(file, param);
                var sfdFile = Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file) + ".sfd");
                var outputFile = Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file) + ".sfdmsp");
                var strctureData = FragmenterResultParcer.FragmenterResultReader(sfdFile);

                writeMspFile(outputFile, rawData, strctureData[0]);
            });
        }

        private static void writeMspFile(string outputFile, RawData rawData, FragmenterResult fragmenterResult)
        {
            using (var sw = new StreamWriter(outputFile, false, Encoding.ASCII))
            {
                sw.WriteLine("NAME: " + rawData.Name);
                sw.WriteLine("SCANNUMBER: " + rawData.ScanNumber);
                sw.WriteLine("RETENTIONTIME: " + rawData.RetentionTime);
                sw.WriteLine("PRECURSORMZ: " + rawData.PrecursorMz);
                sw.WriteLine("PRECURSORTYPE: " + rawData.PrecursorType);
                sw.WriteLine("IONMODE: " + rawData.IonMode);
                sw.WriteLine("INCHIKEY: " + fragmenterResult.Inchikey);
                sw.WriteLine("SMILES: " + fragmenterResult.Smiles);
                sw.WriteLine("COLLISIONENERGY: " + rawData.CollisionEnergy);
                sw.WriteLine("FORMULA: " + rawData.Formula);
                sw.WriteLine("SPECTRUMTYPE: " + rawData.SpectrumType);
                sw.WriteLine("INTENSITY: " + rawData.Intensity);
                sw.WriteLine("METABOLITENAME: " + rawData.MetaboliteName);

                sw.WriteLine("Num Peaks:" + fragmenterResult.FragmentPics.Count);

                if (fragmenterResult.FragmentPics.Count == 0) {sw.WriteLine(); return;}
                foreach (var frag in fragmenterResult.FragmentPics.OrderBy(n => n.MatchedFragmentInfo.MatchedMass).ToList())
                {
                    var matchedInfo = frag.MatchedFragmentInfo;
                    sw.WriteLine(matchedInfo.MatchedMass + "\t" + matchedInfo.TotalLikelihood + "\t" + "\"" + "Rearranged hydrogens: " + matchedInfo.RearrangedHydrogen + "& SMILES: " + matchedInfo.Smiles + "\"");
                }
            }
        }

    }
}
