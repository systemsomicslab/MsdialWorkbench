using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Rfx.Riken.OsakaUniv {
    public sealed class AdductResourceParser {
        private AdductResourceParser() { }

        public static List<AdductIonInformationBean> GetAdductIonInformationList(IonMode ionMode) {

            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = string.Empty;
            if (ionMode == IonMode.Positive)
                resourceName = "DatabaseStandard.Resources.AdductIonResource_Positive.txt";
            else
                resourceName = "DatabaseStandard.Resources.AdductIonResource_Negative.txt";
          
            var adductListString = string.Empty;
            using (var stream = assembly.GetManifestResourceStream(resourceName)) {
                using (var reader = new StreamReader(stream)) {
                    adductListString = reader.ReadToEnd();
                }
            }
            //if (ionMode == IonMode.Positive)
            //    adductListString = Properties.Resources.AdductIonResource_Positive;
            //else
            //    adductListString = Properties.Resources.AdductIonResource_Negative;
            var adductList = adductListString.Replace("\r\n", "\n").Split(new[] { '\n', '\r' });

            List<AdductIonInformationBean> adductIonInformationBeanList = new List<AdductIonInformationBean>();
            AdductIonInformationBean adductIonInformationBean = new AdductIonInformationBean();

            bool checker = true;

            for (int i = 1; i < adductList.Length; i++) {
                var line = adductList[i];
                if (line == "") break;
                var lineArray = line.Split('\t');

                adductIonInformationBean = new AdductIonInformationBean();
                adductIonInformationBean.AdductName = lineArray[0];
                adductIonInformationBean.Charge = int.Parse(lineArray[1]);
                adductIonInformationBean.AccurateMass = double.Parse(lineArray[2]);
                adductIonInformationBean.IonMode = ionMode;

                if (checker) { adductIonInformationBean.Included = true; checker = false; }
                else adductIonInformationBean.Included = false;

                adductIonInformationBeanList.Add(adductIonInformationBean);
            }

            return adductIonInformationBeanList;
        }
    }
}
