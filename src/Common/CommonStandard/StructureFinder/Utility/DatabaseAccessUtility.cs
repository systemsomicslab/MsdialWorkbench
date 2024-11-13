using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Property;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.Common.Parameter;
using CompMs.Common.StructureFinder.DataObj;
using System.Collections.Generic;

namespace CompMs.Common.StructureFinder.Utility
{
    public sealed class DatabaseAccessUtility {
        private DatabaseAccessUtility() { }

        public static List<int> GetExistingPubChemCIDs(List<ExistStructureQuery> queries) {
            var existCIDs = new List<int>();
            foreach (var query in queries) {
                if (query.PubchemCIDs.Count != 0) {
                    foreach (var cid in query.PubchemCIDs) {
                        existCIDs.Add(cid);
                    }
                }
            }
            return existCIDs;
        }

        public static List<ExistStructureQuery> GetStructureQueries(Formula formula,
            List<ExistStructureQuery> queryDB, DatabaseQuery databaseQuery) {
            var cQueries = new List<ExistStructureQuery>();

            var mass = formula.Mass;
            var tol = 0.00005;
            var startID = getQueryStartIndex(mass, tol, queryDB);

            for (int i = startID; i < queryDB.Count; i++) {
                if (queryDB[i].Formula.Mass < mass - tol) continue;
                if (queryDB[i].Formula.Mass > mass + tol) break;

                if (queryCheck(queryDB[i].Formula, formula, queryDB[i].DatabaseQuery, databaseQuery))
                    cQueries.Add(queryDB[i]);

            }

            return cQueries;
        }

        public static List<ExistStructureQuery> GetStructureQueries(Formula formula,
            List<ExistStructureQuery> userDefinedDB) {
            var cQueries = new List<ExistStructureQuery>();

            var mass = formula.Mass;
            var tol = 0.00005;
            var startID = getQueryStartIndex(mass, tol, userDefinedDB);

            for (int i = startID; i < userDefinedDB.Count; i++) {
                if (userDefinedDB[i].Formula.Mass < mass - tol) continue;
                if (userDefinedDB[i].Formula.Mass > mass + tol) break;

                if (MolecularFormulaUtility.isFormulaMatch(userDefinedDB[i].Formula, formula))
                    cQueries.Add(userDefinedDB[i]);
            }

            return cQueries;
        }

        public static bool queryCheck(Formula cFormula, Formula qFormula, DatabaseQuery cDbQuery, DatabaseQuery qDbQuery) {
            if (cFormula.Cnum == qFormula.Cnum && cFormula.Hnum == qFormula.Hnum && cFormula.Nnum == qFormula.Nnum && cFormula.Onum == qFormula.Onum && cFormula.Pnum == qFormula.Pnum &&
                    cFormula.Snum == qFormula.Snum && cFormula.Fnum == qFormula.Fnum && cFormula.Clnum == qFormula.Clnum && cFormula.Brnum == qFormula.Brnum && cFormula.Inum == qFormula.Inum &&
                    cFormula.Sinum == qFormula.Sinum) {
                if ((qDbQuery.Chebi == true && cDbQuery.Chebi == true) ||
                    (qDbQuery.Hmdb == true && cDbQuery.Hmdb == true) ||
                    (qDbQuery.Pubchem == true && cDbQuery.Pubchem == true) ||
                    (qDbQuery.Smpdb == true && cDbQuery.Smpdb == true) ||
                    (qDbQuery.Unpd == true && cDbQuery.Unpd == true) ||
                    (qDbQuery.Ymdb == true && cDbQuery.Ymdb == true) ||
                    (qDbQuery.Bmdb == true && cDbQuery.Bmdb == true) ||
                    (qDbQuery.Drugbank == true && cDbQuery.Drugbank == true) ||
                    (qDbQuery.Ecmdb == true && cDbQuery.Ecmdb == true) ||
                    (qDbQuery.Foodb == true && cDbQuery.Foodb == true) ||
                    (qDbQuery.Knapsack == true && cDbQuery.Knapsack == true) ||
                    (qDbQuery.Plantcyc == true && cDbQuery.Plantcyc == true) ||
                    (qDbQuery.T3db == true && cDbQuery.T3db == true) ||
                    (qDbQuery.Stoff == true && cDbQuery.Stoff == true) ||
                    (qDbQuery.Nanpdb == true && cDbQuery.Nanpdb == true) ||
                    (qDbQuery.Lipidmaps == true && cDbQuery.Lipidmaps == true) ||
                    (qDbQuery.Feces == true && cDbQuery.Feces == true) ||
                    (qDbQuery.Urine == true && cDbQuery.Urine == true) ||
                    (qDbQuery.Saliva == true && cDbQuery.Saliva == true) ||
                    (qDbQuery.Serum == true && cDbQuery.Serum == true) ||
                    (qDbQuery.Csf == true && cDbQuery.Csf == true) ||
                    (qDbQuery.Blexp == true && cDbQuery.Blexp == true)||
                    (qDbQuery.Npa == true && cDbQuery.Npa == true)||
                    (qDbQuery.Coconut == true && cDbQuery.Coconut == true)
                    )
                {
                    return true;
                }
                else return false;
            }
            else {
                return false;
            }
        }

        public static int getQueryStartIndex(double mass, double tol, List<ExistStructureQuery> queryDB) {
            if (queryDB == null || queryDB.Count == 0) return 0;
            double targetMass = mass - tol;
            int startIndex = 0, endIndex = queryDB.Count - 1;
            int counter = 0;

            while (counter < 10) {
                if (queryDB[startIndex].Formula.Mass <= targetMass && targetMass < queryDB[(startIndex + endIndex) / 2].Formula.Mass) {
                    endIndex = (startIndex + endIndex) / 2;
                }
                else if (queryDB[(startIndex + endIndex) / 2].Formula.Mass <= targetMass && targetMass < queryDB[endIndex].Formula.Mass) {
                    startIndex = (startIndex + endIndex) / 2;
                }
                counter++;
            }
            return startIndex;
        }

        public static int getQueryStartIndex(double mass, double tol, List<MspRecord> mspDB) {
            if (mspDB == null || mspDB.Count == 0) return 0;
            double targetMass = mass - tol;
            int startIndex = 0, endIndex = mspDB.Count - 1;
            int counter = 0;

            while (counter < 10) {
                if (mspDB[startIndex].PrecursorMz <= targetMass && targetMass < mspDB[(startIndex + endIndex) / 2].PrecursorMz) {
                    endIndex = (startIndex + endIndex) / 2;
                }
                else if (mspDB[(startIndex + endIndex) / 2].PrecursorMz <= targetMass && targetMass < mspDB[endIndex].PrecursorMz) {
                    startIndex = (startIndex + endIndex) / 2;
                }
                counter++;
            }
            return startIndex;
        }

    }
}
