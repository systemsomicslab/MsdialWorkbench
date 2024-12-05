using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace CompMs.App.Msdial.ExternalApp
{
    internal static class MsDialIniParcer
    {
        public static MsDialIniField Read()
        {
            var iniField = new MsDialIniField();

            var iniPath = Path.Combine(Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath), "MSDIAL.INI");

            if (!File.Exists(iniPath)) { Write(iniField); return iniField; }

            using (var sr = new StreamReader(iniPath, Encoding.ASCII))
            {
                while (sr.Peek() > -1)
                {
                    var line = sr.ReadLine();

                    if (Regex.IsMatch(line, "MSFINDER_FILEPATH=", RegexOptions.IgnoreCase))
                    {
                        iniField.MsfinderFilePath = line.Substring(line.Split('=')[0].Length + 1).Trim();
                    }
                }
            }

            return iniField;
        }

        public static void Write(MsDialIniField iniField)
        {
            var iniPath = Path.Combine(Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath), "MSDIAL.INI");

            using (StreamWriter sw = new StreamWriter(iniPath, false, Encoding.ASCII))
            {
                sw.WriteLine("MSFINDER_FILEPATH=" + iniField.MsfinderFilePath);
            }
        }
    }
}
