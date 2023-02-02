using CompMs.Common.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.Common.Utility {
    public sealed class MoleculeMsRefDataRetrieve
    {
        private MoleculeMsRefDataRetrieve() { }

        public static string GetCompoundName(int id, List<MoleculeMsReference> mspDB)
        {
            if (mspDB == null || mspDB.Count - 1 < id || id < 0) return "Unknown";
            else return mspDB[id].Name;
        }

        public static string GetSMILES(int id, List<MoleculeMsReference> mspDB)
        {
            if (mspDB == null || mspDB.Count - 1 < id || id < 0) return string.Empty;
            else if (mspDB[id].SMILES == null || mspDB[id].SMILES == string.Empty) return string.Empty;
            else return mspDB[id].SMILES;
        }

        public static string GetInChIKey(int id, List<MoleculeMsReference> mspDB)
        {
            if (mspDB == null || mspDB.Count - 1 < id || id < 0) return string.Empty;
            else if (mspDB[id].InChIKey == null || mspDB[id].InChIKey == string.Empty) return string.Empty;
            else return mspDB[id].InChIKey;
        }

        public static string GetFormula(int id, List<MoleculeMsReference> mspDB)
        {
            if (mspDB == null || mspDB.Count - 1 < id || id < 0) return string.Empty;
            else if (mspDB[id].Formula == null || mspDB[id].Formula.FormulaString == string.Empty) return string.Empty;
            else return mspDB[id].Formula.FormulaString;
        }

        public static string GetOntology(int id, List<MoleculeMsReference> mspDB) {
            if (mspDB == null || mspDB.Count - 1 < id || id < 0) return string.Empty;
            else if (mspDB[id].CompoundClass != null && mspDB[id].CompoundClass != string.Empty) return mspDB[id].CompoundClass;
            else if (mspDB[id].Ontology == null || mspDB[id].Ontology == string.Empty) return string.Empty;
            else return mspDB[id].Ontology;
        }

        public static double GetRT(int id, List<MoleculeMsReference> mspDB) {
            if (mspDB == null || mspDB.Count - 1 < id || id < 0) return -1;
            return mspDB[id].ChromXs.RT.Value;
        }

        public static double GetRI(int id, List<MoleculeMsReference> mspDB) {
            if (mspDB == null || mspDB.Count - 1 < id || id < 0) return -1;
            return mspDB[id].ChromXs.RI.Value;
        }

        public static double GetMz(int id, List<MoleculeMsReference> mspDB) {
            if (mspDB == null || mspDB.Count - 1 < id || id < 0) return -1;
            return mspDB[id].PrecursorMz;
        }

        public static double GetCCS(int id, List<MoleculeMsReference> mspDB) {
            if (mspDB == null || mspDB.Count - 1 < id || id < 0) return -1;
            return mspDB[id].CollisionCrossSection;
        }

        public static double GetQuantMass(int id, List<MoleculeMsReference> mspDB) {
            if (mspDB == null || mspDB.Count - 1 < id || id < 0) return -1;
            return mspDB[id].QuantMass;
        }

        public static string GetSMILESOrNull(int id, List<MoleculeMsReference> mspDB) {
            if (mspDB == null || mspDB.Count - 1 < id || id < 0) return "null";
            else if (mspDB[id].SMILES == null || mspDB[id].SMILES == string.Empty) return "null";
            else return mspDB[id].SMILES;
        }

        public static string GetInChIKeyOrNull(int id, List<MoleculeMsReference> mspDB) {
            if (mspDB == null || mspDB.Count - 1 < id || id < 0) return "null";
            else if (mspDB[id].InChIKey == null || mspDB[id].InChIKey == string.Empty) return "null";
            else return mspDB[id].InChIKey;
        }

        public static string GetFormulaOrNull(int id, List<MoleculeMsReference> mspDB) {
            if (mspDB == null || mspDB.Count - 1 < id || id < 0) return "null";
            else if (mspDB[id].Formula == null || mspDB[id].Formula.FormulaString == string.Empty) return "null";
            else return mspDB[id].Formula.FormulaString;
        }

        public static string GetOntologyOrNull(int id, List<MoleculeMsReference> mspDB) {
            if (mspDB == null || mspDB.Count - 1 < id || id < 0) return "null";
            else if (mspDB[id].CompoundClass != null && mspDB[id].CompoundClass != string.Empty) return mspDB[id].CompoundClass;
            else if (mspDB[id].Ontology == null || mspDB[id].Ontology == string.Empty) return "null";
            else return mspDB[id].Ontology;
        }

        public static string GetRTOrNull(int id, List<MoleculeMsReference> mspDB) {
            if (mspDB == null || mspDB.Count - 1 < id || id < 0) return "null";
            return String.Format("{0:0.000}", mspDB[id].ChromXs.RT.Value, 3);
        }

        public static string GetRIOrNull(int id, List<MoleculeMsReference> mspDB) {
            if (mspDB == null || mspDB.Count - 1 < id || id < 0) return "null";
            return String.Format("{0:0.00}", mspDB[id].ChromXs.RI.Value, 2);
        }

        public static string GetMzOrNull(int id, List<MoleculeMsReference> mspDB) {
            if (mspDB == null || mspDB.Count - 1 < id || id < 0) return "null";
            return String.Format("{0:0.00000}", mspDB[id].PrecursorMz, 5);
        }

        public static string GetCCSOrNull(int id, List<MoleculeMsReference> mspDB) {
            if (mspDB == null || mspDB.Count - 1 < id || id < 0) return "null";
            return String.Format("{0:0.000}", mspDB[id].CollisionCrossSection, 3);
        }

        public static string GetQuantMassOrNull(int id, List<MoleculeMsReference> mspDB) {
            if (mspDB == null || mspDB.Count - 1 < id || id < 0) return "null";
            return String.Format("{0:0.00000}", mspDB[id].QuantMass, 5);
        }
    }
}
