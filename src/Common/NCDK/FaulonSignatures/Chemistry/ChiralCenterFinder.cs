using System;
using System.Collections.Generic;

namespace NCDK.FaulonSignatures.Chemistry
{
    public static class ChiralCenterFinder
    {
        public static List<int> FindTetrahedralChiralCenters(Molecule molecule)
        {
            List<int> chiralCenterIndices = new List<int>();
            MoleculeSignature molSig = new MoleculeSignature(molecule);
            List<string> signatureStrings = molSig.GetVertexSignatureStrings();
            for (int i = 0; i < molecule.GetAtomCount(); i++)
            {
                int[] connected = molecule.GetConnected(i);
                if (connected.Length < 4)
                {
                    continue;
                }
                else
                {
                    string s0 = signatureStrings[connected[0]];
                    string s1 = signatureStrings[connected[1]];
                    string s2 = signatureStrings[connected[2]];
                    string s3 = signatureStrings[connected[3]];
                    if (string.Equals(s0, s1, StringComparison.Ordinal)
                     || string.Equals(s0, s2, StringComparison.Ordinal)
                     || string.Equals(s0, s3, StringComparison.Ordinal)
                     || string.Equals(s1, s2, StringComparison.Ordinal)
                     || string.Equals(s1, s3, StringComparison.Ordinal)
                     || string.Equals(s2, s3, StringComparison.Ordinal))
                    {
                        continue;
                    }
                    else
                    {
                        chiralCenterIndices.Add(i);
                    }
                }
            }

            return chiralCenterIndices;
        }
    }
}
