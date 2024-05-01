using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;


namespace CompMs.App.Msdial.Model.Statistics
{
    internal class MuvrRReader
    {
        public string muvrRScript;

        public void Read(string filePath)
        {
            muvrRScript = File.ReadAllText(filePath);
        }

        public void Read()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "CompMs.App.Msdial.Resources.MUVR.R";
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream))
            {
                muvrRScript = reader.ReadToEnd();
            }
        }
    }
}
