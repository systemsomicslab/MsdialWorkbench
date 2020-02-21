using Riken.Metabolomics.AbfDataHandler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbfDataHandlerConsoleApp {
    class Program {
        static void Main(string[] args) {
            new ObjectConverter().ReadAbf(@"D:\20190228-Fiehn lab QTOF\Zhou_PoolMSMS_004_Original_MX438208_Muscle_negCSH_1200_1700_InclusionList.abf", 0);
        }
    }
}
