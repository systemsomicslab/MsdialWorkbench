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
