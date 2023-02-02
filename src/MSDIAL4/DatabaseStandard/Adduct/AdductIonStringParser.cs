using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Rfx.Riken.OsakaUniv
{
    public sealed class AdductIonStringParser
    {
        /// <summary>
        /// This parcer is the old version as the adduct ion string parcer.
        /// But, since AdductIonBean.cs is still used in MS-DIAL program, first this program uses new parcer 'AdductIonParcer.GetAdductIonBean(string adductString)' to get adduct ion information,
        /// and then, new AdductIon.cs should be converted to old AdductIonBean.cs in MS-DIAL program.
        /// </summary>
        /// <param name="adductName"></param>
        /// <returns></returns>
        public static AdductIonBean GetAdductIonBean(string adductName)
        {
            var adductIon = AdductIonParcer.GetAdductIonBean(adductName);

            var adductIonBean = new AdductIonBean()
            {
                AdductIonName = adductIon.AdductIonName,
                AdductIonAccurateMass = (float)adductIon.AdductIonAccurateMass,
                AdductIonXmer = adductIon.AdductIonXmer,
                ChargeNumber = adductIon.ChargeNumber,
                FormatCheck = adductIon.FormatCheck
            };

            if (adductIon.IonMode == IonMode.Positive) adductIonBean.IonType = IonType.Positive;
            else adductIonBean.IonType = IonType.Negative;

            return adductIonBean;
        }
    }
}
