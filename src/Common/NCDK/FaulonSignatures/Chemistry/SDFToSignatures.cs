using System;

namespace NCDK.FaulonSignatures.Chemistry
{
    public class SDFToSignatures
    {
        public static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.Out.WriteLine("Usage : SDFToSignatures <filename>");
            }
            string filename = args[0];
            int molCount = 0;
            foreach (var molecule in MoleculeReader.ReadSDFFile(filename))
            {
                try
                {
                    molCount++;
                    Console.Out.WriteLine("Current molecule: " + molCount);
                    MoleculeSignature signature = new MoleculeSignature(molecule);
                    // get graph signature
                    Console.Out.WriteLine(signature.GetGraphSignature());
                }
                catch (Exception) { }
            }
        }
    }
}
