using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace CompMs.Common.Parser {
    public sealed class AdductResourceParser {
        private AdductResourceParser() { }

        public static List<AdductIon> GetAdductIonInformationList(IonMode ionMode) {

            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = string.Empty;
            if (ionMode == IonMode.Positive)
                resourceName = "CompMs.Common.Resources.AdductIonResource_Positive.txt";
            else
                resourceName = "CompMs.Common.Resources.AdductIonResource_Negative.txt";
          
            var adductListString = string.Empty;
            using (var stream = assembly.GetManifestResourceStream(resourceName)) {
                using (var reader = new StreamReader(stream)) {
                    adductListString = reader.ReadToEnd();
                }
            }
            var adductList = adductListString.Replace("\r\n", "\n").Split(new[] { '\n', '\r' });

            var adductIons = new List<AdductIon>();

            for (int i = 1; i < adductList.Length; i++) {
                var line = adductList[i];
                if (line == "") break;
                var lineArray = line.Split('\t');

                var adductIon = AdductIon.GetAdductIon(lineArray[0]);
                adductIons.Add(adductIon);
            }

            return adductIons;
        }
    }
}
