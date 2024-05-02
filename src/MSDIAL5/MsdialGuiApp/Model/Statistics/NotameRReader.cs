using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;


namespace CompMs.App.Msdial.Model.Statistics {
    internal class NotameRReader {
        public string rScript;

        public void Read(string filePath) {
            rScript = File.ReadAllText(filePath);
        }

        public void Read() {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "CompMs.App.Msdial.Resources.Notame.R";
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream)) {
                rScript = reader.ReadToEnd();
            }
        }
    }
}
