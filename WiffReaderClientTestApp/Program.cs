using CompMs.RawDataHandler.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace WiffReaderClientTestApp
{
    class Program
    {
        static void Main(string[] args) {

            using (var rawDataAccess = new RawDataAccess(@"\\mtbdt\Mtb_info\data\MS-DIAL demo files\Raw\HILIC-Pos-SWATH-25Da.wiff", 0, false)) {
                rawDataAccess.DataDump(@"\\mtbdt\Mtb_info\data\MS-DIAL demo files\Raw\HILIC-Pos-SWATH-25Da.wiff");
            }

            Console.ReadKey();
        }
    }
}
