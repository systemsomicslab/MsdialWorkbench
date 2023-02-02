using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Rfx.Riken.OsakaUniv
{
    public sealed class MsDialIniParcer
    {
        private MsDialIniParcer() { }

        public static MsDialIniField Read()
        {
            var iniField = new MsDialIniField();
            var iniPath = System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath) + "\\MSDIAL.INI";

            if (!System.IO.File.Exists(iniPath)) { Write(iniField); return iniField; }

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
            var iniPath = System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath) + "\\MSDIAL.INI";

            using (StreamWriter sw = new StreamWriter(iniPath, false, Encoding.ASCII))
            {
                sw.WriteLine("MSFINDER_FILEPATH=" + iniField.MsfinderFilePath);
            }
        }
    }
}
