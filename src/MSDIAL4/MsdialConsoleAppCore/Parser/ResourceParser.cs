using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Riken.Metabolomics.MsdialConsoleApp.Parser
{
    public sealed class ResourceParser
    {
        private ResourceParser() { }

        //public static IupacReferenceBean ReadIupcaReference()
        //{
        //    var iupacStrings = Properties.Resources.IUPAC;

        //    int iupacID = 0;
        //    string elementName = "";

        //    var iupacReference = new IupacReferenceBean();
        //    var iupacElements = new List<IupacElementPropertyBean>();
        //    var iupacElement = new IupacElementPropertyBean();

        //    foreach (string ap in iupacStrings.Split('\n'))
        //    {
        //        if (ap == string.Empty) break;

        //        var lineArray = ap.Split('\t');
        //        if (iupacID != int.Parse(lineArray[0]))
        //        {
        //            if (iupacID != 0) { iupacReference.IupacID_IupacElementPropertyBeanList[iupacID] = iupacElements; iupacReference.ElementName_IupacElementPropertyBeanList[elementName] = iupacElements; }

        //            iupacElements = new List<IupacElementPropertyBean>();
        //            iupacID = int.Parse(lineArray[0]);
        //            elementName = lineArray[1];

        //            iupacElement = new IupacElementPropertyBean();
        //            iupacElement.AccurateMass = double.Parse(lineArray[4]);
        //            iupacElement.ElementName = elementName;
        //            iupacElement.IupacID = iupacID;
        //            iupacElement.NaturalRelativeAbundance = double.Parse(lineArray[3]);
        //            iupacElement.NominalMass = int.Parse(lineArray[2]);

        //            iupacElements.Add(iupacElement);
        //        }
        //        else
        //        {
        //            iupacElement = new IupacElementPropertyBean();
        //            iupacElement.AccurateMass = double.Parse(lineArray[4]);
        //            iupacElement.ElementName = elementName;
        //            iupacElement.IupacID = iupacID;
        //            iupacElement.NaturalRelativeAbundance = double.Parse(lineArray[3]);
        //            iupacElement.NominalMass = int.Parse(lineArray[2]);

        //            iupacElements.Add(iupacElement);
        //        }
        //    }
        //    //reminder
        //    iupacReference.IupacID_IupacElementPropertyBeanList[iupacID] = iupacElements;
        //    iupacReference.ElementName_IupacElementPropertyBeanList[elementName] = iupacElements;

        //    return iupacReference;
        //}

        //public static List<AdductIonInformationBean> ReadAdductList(IonMode ionmode)
        //{
        //    var adductString = Properties.Resources.AdductIonResource_Positive;
        //    if (ionmode == IonMode.Negative) adductString = Properties.Resources.AdductIonResource_Negative;

        //    var checker = true;
        //    var adductIons = new List<AdductIonInformationBean>();

        //    foreach (string ap in adductString.Split('\n'))
        //    {
        //        if (ap == string.Empty) break;

        //        var lineArray = ap.Split('\t');

        //        var adduction = new AdductIonInformationBean();
        //        adduction.AdductName = lineArray[0];
        //        adduction.Charge = int.Parse(lineArray[1]);
        //        adduction.AccurateMass = double.Parse(lineArray[2]);

        //        if (checker) { adduction.Included = true; checker = false; }
        //        else adduction.Included = false;

        //        adductIons.Add(adduction);
        //    }

        //    return adductIons;
        //}
    }
}
