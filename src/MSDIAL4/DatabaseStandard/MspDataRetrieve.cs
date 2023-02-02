using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rfx.Riken.OsakaUniv
{
    public sealed class RefDataRetrieve {
        private RefDataRetrieve() { }
        public static string GetCompoundName(int mspID, List<MspFormatCompoundInformationBean> mspDB, int txtID, List<PostIdentificatioinReferenceBean> txtDB) {
            if (txtID >= 0) return TxtDBDataRetrieve.GetCompoundName(txtID, txtDB);
            else if (mspID>= 0) return MspDataRetrieve.GetCompoundName(mspID, mspDB);
            return "Unknown";
        }

        public static string GetSMILES(int mspID, List<MspFormatCompoundInformationBean> mspDB, int txtID, List<PostIdentificatioinReferenceBean> txtDB) {
            if (txtID >= 0) return TxtDBDataRetrieve.GetSMILES(txtID, txtDB);
            else if (mspID >= 0) return MspDataRetrieve.GetSMILES(mspID, mspDB);
            return string.Empty;
        }

        public static string GetInChIKey(int mspID, List<MspFormatCompoundInformationBean> mspDB, int txtID, List<PostIdentificatioinReferenceBean> txtDB) {
            if (txtID >= 0) return TxtDBDataRetrieve.GetInChIKey(txtID, txtDB);
            else if (mspID >= 0) return MspDataRetrieve.GetInChIKey(mspID, mspDB);
            return string.Empty;
        }

        public static string GetFormula(int mspID, List<MspFormatCompoundInformationBean> mspDB, int txtID, List<PostIdentificatioinReferenceBean> txtDB) {
            if (txtID >= 0) return TxtDBDataRetrieve.GetFormula(txtID, txtDB);
            else if (mspID >= 0) return MspDataRetrieve.GetFormula(mspID, mspDB);
            return string.Empty;
        }

        public static string GetOntology(int mspID, List<MspFormatCompoundInformationBean> mspDB, int txtID, List<PostIdentificatioinReferenceBean> txtDB) {
            if (txtID >= 0) return TxtDBDataRetrieve.GetOntology(txtID, txtDB);
            else if (mspID >= 0) return MspDataRetrieve.GetOntology(mspID, mspDB);
            return string.Empty;
        }
    }

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

    public sealed class TxtDBDataRetrieve {

        private TxtDBDataRetrieve() { }

        public static string GetCompoundName(int id, List<PostIdentificatioinReferenceBean> txtDB) {
            if (txtDB == null || txtDB.Count - 1 < id || id < 0) return "Unknown";
            else return txtDB[id].MetaboliteName;
        }

        public static string GetSMILES(int id, List<PostIdentificatioinReferenceBean> txtDB) {
            if (txtDB == null || txtDB.Count - 1 < id || id < 0) return string.Empty;
            else if (txtDB[id].Smiles == null || txtDB[id].Smiles == string.Empty) return string.Empty;
            else return txtDB[id].Smiles;
        }

        public static string GetInChIKey(int id, List<PostIdentificatioinReferenceBean> txtDB) {
            if (txtDB == null || txtDB.Count - 1 < id || id < 0) return string.Empty;
            else if (txtDB[id].Inchikey == null || txtDB[id].Inchikey == string.Empty) return string.Empty;
            else return txtDB[id].Inchikey;
        }

        public static string GetFormula(int id, List<PostIdentificatioinReferenceBean> txtDB) {
            if (txtDB == null || txtDB.Count - 1 < id || id < 0) return string.Empty;
            else if (txtDB[id].Formula == null || txtDB[id].Formula.FormulaString == null || txtDB[id].Formula.FormulaString == string.Empty) return string.Empty;
            else return txtDB[id].Formula.FormulaString;
        }

        public static string GetOntology(int id, List<PostIdentificatioinReferenceBean> txtDB) {
            if (txtDB == null || txtDB.Count - 1 < id || id < 0) return string.Empty;
            else if (txtDB[id].Ontology == null || txtDB[id].Ontology == string.Empty) return string.Empty;
            else return txtDB[id].Ontology;
        }
    }
}
