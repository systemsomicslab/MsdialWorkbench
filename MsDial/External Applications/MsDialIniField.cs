using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rfx.Riken.OsakaUniv
{
    public class MsDialIniField
    {
        public MsDialIniField()
        {
            msfinderFilePath = System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath) + "\\MSFINDER.exe";
        }

        private string msfinderFilePath;

        public string MsfinderFilePath
        {
            get { return msfinderFilePath; }
            set { msfinderFilePath = value; }
        }
    }
}
