using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rfx.Riken.OsakaUniv
{
    public sealed class MspDataRetrieve
    {

        private MspDataRetrieve() { }

        public static string GetCompoundName(int id, List<MspFormatCompoundInformationBean> mspDB)
        {
            if (mspDB == null || mspDB.Count - 1 < id || id < 0) return "Unknown";
            else return mspDB[id].Name;
        }

        public static string GetSMILES(int id, List<MspFormatCompoundInformationBean> mspDB)
        {
            if (mspDB == null || mspDB.Count - 1 < id || id < 0) return string.Empty;
            else if (mspDB[id].Smiles == null || mspDB[id].Smiles == string.Empty) return string.Empty;
            else return mspDB[id].Smiles;
        }

        public static string GetInChIKey(int id, List<MspFormatCompoundInformationBean> mspDB)
        {
            if (mspDB == null || mspDB.Count - 1 < id || id < 0) return string.Empty;
            else if (mspDB[id].InchiKey == null || mspDB[id].InchiKey == string.Empty) return string.Empty;
            else return mspDB[id].InchiKey;
        }

        public static string GetFormula(int id, List<MspFormatCompoundInformationBean> mspDB)
        {
            if (mspDB == null || mspDB.Count - 1 < id || id < 0) return string.Empty;
            else if (mspDB[id].Formula == null || mspDB[id].Formula == string.Empty) return string.Empty;
            else return mspDB[id].Formula;
        }

        public static string GetOntology(int id, List<MspFormatCompoundInformationBean> mspDB) {
            if (mspDB == null || mspDB.Count - 1 < id || id < 0) return string.Empty;
            else if (mspDB[id].CompoundClass != null && mspDB[id].CompoundClass != string.Empty) return mspDB[id].CompoundClass;
            else if (mspDB[id].Ontology == null || mspDB[id].Ontology == string.Empty) return string.Empty;
            else return mspDB[id].Ontology;
        }
    }
}
