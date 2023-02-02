using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Rfx.Riken.OsakaUniv {
    public sealed class IupacResourceParser {
        private IupacResourceParser() { }

        public static void SetIupacReferenceBean(IupacReferenceBean iupacReferenceBean) {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "DatabaseStandard.Resources.IUPAC.txt";
            var iupacString = string.Empty;
            using (var stream = assembly.GetManifestResourceStream(resourceName)) {
                using (var reader = new StreamReader(stream)) {
                    iupacString = reader.ReadToEnd();
                }
            }

            var iupacElements = iupacString.Replace("\r\n", "\n").Split(new[] { '\n', '\r' });

            var iupacID = 0;
            var elementName = string.Empty;

            var iupacElementPropertyBeanList = new List<IupacElementPropertyBean>();
            var iupacElementPropertyBean = new IupacElementPropertyBean();

            for (int i = 1; i < iupacElements.Length; i++) {

                var line = iupacElements[i];
                if (line == string.Empty) continue;

                var lineArray = line.Split('\t');

                if (iupacID != int.Parse(lineArray[0])) {
                    if (iupacID != 0) { iupacReferenceBean.IupacID_IupacElementPropertyBeanList[iupacID] = iupacElementPropertyBeanList; iupacReferenceBean.ElementName_IupacElementPropertyBeanList[elementName] = iupacElementPropertyBeanList; }

                    iupacElementPropertyBeanList = new List<IupacElementPropertyBean>();
                    iupacID = int.Parse(lineArray[0]);
                    elementName = lineArray[1];

                    iupacElementPropertyBean = new IupacElementPropertyBean();
                    iupacElementPropertyBean.AccurateMass = double.Parse(lineArray[4]);
                    iupacElementPropertyBean.ElementName = elementName;
                    iupacElementPropertyBean.IupacID = iupacID;
                    iupacElementPropertyBean.NaturalRelativeAbundance = double.Parse(lineArray[3]);
                    iupacElementPropertyBean.NominalMass = int.Parse(lineArray[2]);

                    iupacElementPropertyBeanList.Add(iupacElementPropertyBean);
                }
                else {
                    iupacElementPropertyBean = new IupacElementPropertyBean();
                    iupacElementPropertyBean.AccurateMass = double.Parse(lineArray[4]);
                    iupacElementPropertyBean.ElementName = elementName;
                    iupacElementPropertyBean.IupacID = iupacID;
                    iupacElementPropertyBean.NaturalRelativeAbundance = double.Parse(lineArray[3]);
                    iupacElementPropertyBean.NominalMass = int.Parse(lineArray[2]);

                    iupacElementPropertyBeanList.Add(iupacElementPropertyBean);
                }
            }

            //reminder
            iupacReferenceBean.IupacID_IupacElementPropertyBeanList[iupacID] = iupacElementPropertyBeanList;
            iupacReferenceBean.ElementName_IupacElementPropertyBeanList[elementName] = iupacElementPropertyBeanList;
        }

        public static IupacReferenceBean GetIupacReferenceBean() {

            var iupacReferenceBean = new IupacReferenceBean();
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "DatabaseStandard.Resources.IUPAC.txt";
            var iupacString = string.Empty;
            using (var stream = assembly.GetManifestResourceStream(resourceName)) {
                using (var reader = new StreamReader(stream)) {
                    iupacString = reader.ReadToEnd();
                }
            }
            var iupacElements = iupacString.Replace("\r\n", "\n").Split(new[] { '\n', '\r' });

            var iupacID = 0;
            var elementName = string.Empty;

            var iupacElementPropertyBeanList = new List<IupacElementPropertyBean>();
            var iupacElementPropertyBean = new IupacElementPropertyBean();

            for (int i = 1; i < iupacElements.Length; i++) {

                var line = iupacElements[i];
                if (line == string.Empty) continue;

                var lineArray = line.Split('\t');

                if (iupacID != int.Parse(lineArray[0])) {
                    if (iupacID != 0) { iupacReferenceBean.IupacID_IupacElementPropertyBeanList[iupacID] = iupacElementPropertyBeanList; iupacReferenceBean.ElementName_IupacElementPropertyBeanList[elementName] = iupacElementPropertyBeanList; }

                    iupacElementPropertyBeanList = new List<IupacElementPropertyBean>();
                    iupacID = int.Parse(lineArray[0]);
                    elementName = lineArray[1];

                    iupacElementPropertyBean = new IupacElementPropertyBean();
                    iupacElementPropertyBean.AccurateMass = double.Parse(lineArray[4]);
                    iupacElementPropertyBean.ElementName = elementName;
                    iupacElementPropertyBean.IupacID = iupacID;
                    iupacElementPropertyBean.NaturalRelativeAbundance = double.Parse(lineArray[3]);
                    iupacElementPropertyBean.NominalMass = int.Parse(lineArray[2]);

                    iupacElementPropertyBeanList.Add(iupacElementPropertyBean);
                }
                else {
                    iupacElementPropertyBean = new IupacElementPropertyBean();
                    iupacElementPropertyBean.AccurateMass = double.Parse(lineArray[4]);
                    iupacElementPropertyBean.ElementName = elementName;
                    iupacElementPropertyBean.IupacID = iupacID;
                    iupacElementPropertyBean.NaturalRelativeAbundance = double.Parse(lineArray[3]);
                    iupacElementPropertyBean.NominalMass = int.Parse(lineArray[2]);

                    iupacElementPropertyBeanList.Add(iupacElementPropertyBean);
                }
            }

            //reminder
            iupacReferenceBean.IupacID_IupacElementPropertyBeanList[iupacID] = iupacElementPropertyBeanList;
            iupacReferenceBean.ElementName_IupacElementPropertyBeanList[elementName] = iupacElementPropertyBeanList;

            return iupacReferenceBean;
        }
    }
}
