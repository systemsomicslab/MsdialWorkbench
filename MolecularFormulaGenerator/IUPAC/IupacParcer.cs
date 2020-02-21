using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Resources;

namespace Rfx.Riken.OsakaUniv
{
    public sealed class IupacParcer
    {
        private IupacParcer() { }

        public static Iupac GetIupacReferenceBean()
        {
            Uri fileUri = new Uri("/Resource/Iupac.txt", UriKind.Relative);
            StreamResourceInfo info = Application.GetResourceStream(fileUri);
            Iupac iupacReferenceBean = new Iupac();

            int iupacID = 0;
            string elementName = "";
            string line;
            string[] lineArray;

            List<IupacChemicalElement> iupacElementPropertyBeanList = new List<IupacChemicalElement>();
            IupacChemicalElement iupacElementPropertyBean = new IupacChemicalElement();

            using (StreamReader sr = new StreamReader(info.Stream))
            {
                sr.ReadLine();
                while (sr.Peek() > -1)
                {
                    line = sr.ReadLine();
                    if (line == string.Empty) break;

                    lineArray = line.Split('\t');

                    if (iupacID != int.Parse(lineArray[0]))
                    {
                        if (iupacID != 0) { iupacReferenceBean.IupacID_IupacElementPropertyBeanList[iupacID] = iupacElementPropertyBeanList; iupacReferenceBean.ElementName_IupacElementPropertyBeanList[elementName] = iupacElementPropertyBeanList; }

                        iupacElementPropertyBeanList = new List<IupacChemicalElement>();
                        iupacID = int.Parse(lineArray[0]);
                        elementName = lineArray[1];
                    }

                    iupacElementPropertyBean = getElementProperty(double.Parse(lineArray[4]), elementName, iupacID, double.Parse(lineArray[3]), int.Parse(lineArray[2]));
                    iupacElementPropertyBeanList.Add(iupacElementPropertyBean);
                }
                //reminder
                iupacReferenceBean.IupacID_IupacElementPropertyBeanList[iupacID] = iupacElementPropertyBeanList;
                iupacReferenceBean.ElementName_IupacElementPropertyBeanList[elementName] = iupacElementPropertyBeanList;
            }

            return iupacReferenceBean;
        }

        private static IupacChemicalElement getElementProperty(double accurateMass, string elementName, int iupacID, double naturalAbundance, int noinalMass)
        {
            IupacChemicalElement iupacElementPropertyBean = new IupacChemicalElement();
            iupacElementPropertyBean.AccurateMass = accurateMass;
            iupacElementPropertyBean.ElementName = elementName;
            iupacElementPropertyBean.IupacID = iupacID;
            iupacElementPropertyBean.NaturalRelativeAbundance = naturalAbundance;
            iupacElementPropertyBean.NominalMass = noinalMass;

            return iupacElementPropertyBean;
        }
    }
}
