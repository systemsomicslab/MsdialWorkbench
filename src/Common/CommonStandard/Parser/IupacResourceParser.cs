using CompMs.Common.DataObj.Database;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CompMs.Common.Parser {
    public sealed class IupacResourceParser {
        private IupacResourceParser() { }

        public static void SetIupacReferenceBean(IupacDatabase iupacDB) {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "CompMs.Common.Resources.IUPAC.txt";
            var iupacString = string.Empty;
            using (var stream = assembly.GetManifestResourceStream(resourceName)) {
                using (var reader = new StreamReader(stream)) {
                    iupacString = reader.ReadToEnd();
                }
            }

            var iupacElements = iupacString.Replace("\r\n", "\n").Split(new[] { '\n', '\r' });

            var iupacID = 0;
            var elementName = string.Empty;

            var iupacElementPropertyBeanList = new List<AtomElementProperty>();
            var iupacElementPropertyBean = new AtomElementProperty();

            for (int i = 1; i < iupacElements.Length; i++) {

                var line = iupacElements[i];
                if (line == string.Empty) continue;

                var lineArray = line.Split('\t');

                if (iupacID != int.Parse(lineArray[0])) {
                    if (iupacID != 0) {
                        iupacDB.Id2AtomElementProperties[iupacID] = iupacElementPropertyBeanList;
                        iupacDB.ElementName2AtomElementProperties[elementName] = iupacElementPropertyBeanList; 
                    }

                    iupacElementPropertyBeanList = new List<AtomElementProperty>();
                    iupacID = int.Parse(lineArray[0]);
                    elementName = lineArray[1];

                    iupacElementPropertyBean = new AtomElementProperty();
                    iupacElementPropertyBean.ExactMass = double.Parse(lineArray[4]);
                    iupacElementPropertyBean.ElementName = elementName;
                    iupacElementPropertyBean.ID = iupacID;
                    iupacElementPropertyBean.NaturalRelativeAbundance = double.Parse(lineArray[3]);
                    iupacElementPropertyBean.NominalMass = int.Parse(lineArray[2]);

                    iupacElementPropertyBeanList.Add(iupacElementPropertyBean);
                }
                else {
                    iupacElementPropertyBean = new AtomElementProperty();
                    iupacElementPropertyBean.ExactMass = double.Parse(lineArray[4]);
                    iupacElementPropertyBean.ElementName = elementName;
                    iupacElementPropertyBean.ID = iupacID;
                    iupacElementPropertyBean.NaturalRelativeAbundance = double.Parse(lineArray[3]);
                    iupacElementPropertyBean.NominalMass = int.Parse(lineArray[2]);

                    iupacElementPropertyBeanList.Add(iupacElementPropertyBean);
                }
            }

            //reminder
            iupacDB.Id2AtomElementProperties[iupacID] = iupacElementPropertyBeanList;
            iupacDB.ElementName2AtomElementProperties[elementName] = iupacElementPropertyBeanList;
        }

        public static IupacDatabase GetIupacCHData() {
            var hAtomElements = new List<AtomElementProperty>() {
                new AtomElementProperty() { ID = 1, ElementName = "H", NaturalRelativeAbundance = 99.9885, ExactMass = 1.00782503207, NominalMass = 1 },
                new AtomElementProperty() { ID = 1, ElementName = "H", NaturalRelativeAbundance = 0.0115, ExactMass = 2.01410177780, NominalMass = 2 }
            };

            var cAtomElements = new List<AtomElementProperty>() {
                new AtomElementProperty() { ID = 6, ElementName = "C", NaturalRelativeAbundance = 98.93, ExactMass = 12.00000000000, NominalMass = 12 },
                new AtomElementProperty() { ID = 6, ElementName = "C", NaturalRelativeAbundance = 1.07, ExactMass = 13.00335483780, NominalMass = 13 }
            };

            var db = new IupacDatabase();
            db.Id2AtomElementProperties[1] = hAtomElements;
            db.Id2AtomElementProperties[6] = cAtomElements;
            db.ElementName2AtomElementProperties["H"] = hAtomElements;
            db.ElementName2AtomElementProperties["C"] = cAtomElements;
            return db;
        }

        public static IupacDatabase GetIupacCHNOSPData() {

            var hAtomElements = new List<AtomElementProperty>() {
                new AtomElementProperty() { ID = 1, ElementName = "H", NaturalRelativeAbundance = 99.9885, ExactMass = 1.00782503207, NominalMass = 1 },
                new AtomElementProperty() { ID = 1, ElementName = "H", NaturalRelativeAbundance = 0.0115, ExactMass = 2.01410177780, NominalMass = 2 }
            };

            var cAtomElements = new List<AtomElementProperty>() {
                new AtomElementProperty() { ID = 6, ElementName = "C", NaturalRelativeAbundance = 98.93, ExactMass = 12.00000000000, NominalMass = 12 },
                new AtomElementProperty() { ID = 6, ElementName = "C", NaturalRelativeAbundance = 1.07, ExactMass = 13.00335483780, NominalMass = 13 }
            };

            var nAtomElements = new List<AtomElementProperty>() {
                new AtomElementProperty() { ID = 7, ElementName = "N", NaturalRelativeAbundance = 99.636, ExactMass = 14.00307400480, NominalMass = 14 },
                new AtomElementProperty() { ID = 7, ElementName = "N", NaturalRelativeAbundance = 0.364, ExactMass = 15.00010889820, NominalMass = 15 }
            };

            var oAtomElements = new List<AtomElementProperty>() {
                new AtomElementProperty() { ID = 8, ElementName = "O", NaturalRelativeAbundance = 99.757, ExactMass = 15.99491461956, NominalMass = 16 },
                new AtomElementProperty() { ID = 8, ElementName = "O", NaturalRelativeAbundance = 0.038, ExactMass = 16.99913170000, NominalMass = 17 },
                new AtomElementProperty() { ID = 8, ElementName = "O", NaturalRelativeAbundance = 0.205, ExactMass = 17.99916100000, NominalMass = 18 }
            };

            var pAtomElements = new List<AtomElementProperty>() {
                new AtomElementProperty() { ID = 15, ElementName = "P", NaturalRelativeAbundance = 100, ExactMass = 30.97376163000, NominalMass = 31 },
            };

            var sAtomElements = new List<AtomElementProperty>() {
                new AtomElementProperty() { ID = 16, ElementName = "S", NaturalRelativeAbundance = 94.99, ExactMass = 31.97207100000, NominalMass = 32 },
                new AtomElementProperty() { ID = 16, ElementName = "S", NaturalRelativeAbundance = 0.75, ExactMass = 32.97145876000, NominalMass = 33 },
                new AtomElementProperty() { ID = 16, ElementName = "S", NaturalRelativeAbundance = 4.25, ExactMass = 33.96786690000, NominalMass = 34 },
                new AtomElementProperty() { ID = 16, ElementName = "S", NaturalRelativeAbundance = 0.01, ExactMass = 35.96708076000, NominalMass = 36 }
            };

            var db = new IupacDatabase();
            db.Id2AtomElementProperties[1] = hAtomElements;
            db.Id2AtomElementProperties[6] = cAtomElements;
            db.Id2AtomElementProperties[7] = nAtomElements;
            db.Id2AtomElementProperties[8] = oAtomElements;
            db.Id2AtomElementProperties[15] = pAtomElements;
            db.Id2AtomElementProperties[16] = sAtomElements;
            db.ElementName2AtomElementProperties["H"] = hAtomElements;
            db.ElementName2AtomElementProperties["C"] = cAtomElements;
            db.ElementName2AtomElementProperties["N"] = nAtomElements;
            db.ElementName2AtomElementProperties["O"] = oAtomElements;
            db.ElementName2AtomElementProperties["P"] = pAtomElements;
            db.ElementName2AtomElementProperties["S"] = sAtomElements;
            return db;
        }

        public static IupacDatabase GetIUPACDatabase() {

            var iupacReferenceBean = new IupacDatabase();
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "CompMs.Common.Resources.IUPAC.txt";
            var iupacString = string.Empty;
            using (var stream = assembly.GetManifestResourceStream(resourceName)) {
                using (var reader = new StreamReader(stream)) {
                    iupacString = reader.ReadToEnd();
                }
            }
            var iupacElements = iupacString.Replace("\r\n", "\n").Split(new[] { '\n', '\r' });

            var iupacID = 0;
            var elementName = string.Empty;

            var iupacElementPropertyBeanList = new List<AtomElementProperty>();
            var iupacElementPropertyBean = new AtomElementProperty();

            for (int i = 1; i < iupacElements.Length; i++) {

                var line = iupacElements[i];
                if (line == string.Empty) continue;

                var lineArray = line.Split('\t');

                if (iupacID != int.Parse(lineArray[0])) {
                    if (iupacID != 0) {
                        iupacReferenceBean.Id2AtomElementProperties[iupacID] = iupacElementPropertyBeanList; 
                        iupacReferenceBean.ElementName2AtomElementProperties[elementName] = iupacElementPropertyBeanList; 
                    }

                    iupacElementPropertyBeanList = new List<AtomElementProperty>();
                    iupacID = int.Parse(lineArray[0]);
                    elementName = lineArray[1];

                    iupacElementPropertyBean = new AtomElementProperty();
                    iupacElementPropertyBean.ExactMass = double.Parse(lineArray[4]);
                    iupacElementPropertyBean.ElementName = elementName;
                    iupacElementPropertyBean.ID = iupacID;
                    iupacElementPropertyBean.NaturalRelativeAbundance = double.Parse(lineArray[3]);
                    iupacElementPropertyBean.NominalMass = int.Parse(lineArray[2]);

                    iupacElementPropertyBeanList.Add(iupacElementPropertyBean);
                }
                else {
                    iupacElementPropertyBean = new AtomElementProperty();
                    iupacElementPropertyBean.ExactMass = double.Parse(lineArray[4]);
                    iupacElementPropertyBean.ElementName = elementName;
                    iupacElementPropertyBean.ID = iupacID;
                    iupacElementPropertyBean.NaturalRelativeAbundance = double.Parse(lineArray[3]);
                    iupacElementPropertyBean.NominalMass = int.Parse(lineArray[2]);

                    iupacElementPropertyBeanList.Add(iupacElementPropertyBean);
                }
            }

            //reminder
            iupacReferenceBean.Id2AtomElementProperties[iupacID] = iupacElementPropertyBeanList;
            iupacReferenceBean.ElementName2AtomElementProperties[elementName] = iupacElementPropertyBeanList;

            return iupacReferenceBean;
        }
    }
}
