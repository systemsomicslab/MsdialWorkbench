using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.App.Msdial.ExternalApp {
    public class MsDialIniField
    {
        public MsDialIniField()
        {
            msfinderFilePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath), "MSFINDER.exe");
        }

        private string msfinderFilePath;

        public string MsfinderFilePath
        {
            get { return msfinderFilePath; }
            set { msfinderFilePath = value; }
        }
    }
}
