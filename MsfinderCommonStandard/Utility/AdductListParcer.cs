using Rfx.Riken.OsakaUniv;
using Riken.Metabolomics.MsfinderCommon.Query;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;

namespace Riken.Metabolomics.MsfinderCommon.Utility
{
	public sealed class AdductListParcer
    {
        private AdductListParcer() { }

        public static List<AdductIon> GetAdductPositiveResources()
        {
            var adducts = new List<AdductIon>();
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "MsfinderCommonStandard.Resources.AdductPositives.apf";

            //var filePath = Properties.Resources.AdductPositives;
            var filePath = string.Empty;
            using (var stream = assembly.GetManifestResourceStream(resourceName)) {
                using (var reader = new StreamReader(assembly.GetManifestResourceStream(resourceName))) {
                    filePath = reader.ReadToEnd();
                }
            }

            foreach (string ap in filePath.Replace("\r\n", "\n").Split('\n'))
			{
				adducts.Add(AdductIonParcer.GetAdductIonBean(ap));
			}

			return adducts;

		}

		public static List<AdductIon> GetAdductNegativeResources()
        {
            var adducts = new List<AdductIon>();
            //var filePath = Properties.Resources.AdductNegatives;
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "MsfinderCommonStandard.Resources.AdductNegatives.anf";
            var filePath = string.Empty;
            using (var stream = assembly.GetManifestResourceStream(resourceName)) {
                using (var reader = new StreamReader(stream)) {
                    filePath = reader.ReadToEnd();
                }
            }

            foreach (string ap in filePath.Replace("\r\n", "\n").Split('\n'))
			{
				adducts.Add(AdductIonParcer.GetAdductIonBean(ap));
			}

			return adducts;

		}

		public static void WriteAdductPositiveResources(List<AdductIon> adducts)
        {
            //var mainDirectory = Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath);
            var mainDirectory = System.AppDomain.CurrentDomain.BaseDirectory;
            var filePath = Directory.GetFiles(mainDirectory + "Resources", "*." + SaveFileFormat.apf, SearchOption.TopDirectoryOnly)[0];

            using (StreamWriter sw = new StreamWriter(filePath, false, Encoding.ASCII))
            {
                foreach (var adduct in adducts)
                {
                    sw.WriteLine(adduct.AdductIonName);
                }
            }
        }

        public static void WriteAdductNegativeResources(List<AdductIon> adducts)
        {
            //var mainDirectory = Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath);
            var mainDirectory = System.AppDomain.CurrentDomain.BaseDirectory;
            var filePath = Directory.GetFiles(mainDirectory + "Resources", "*." + SaveFileFormat.anf, SearchOption.TopDirectoryOnly)[0];

            using (StreamWriter sw = new StreamWriter(filePath, false, Encoding.ASCII))
            {
                foreach (var adduct in adducts)
                {
                    sw.WriteLine(adduct.AdductIonName);
                }
            }
        }
    }
}
