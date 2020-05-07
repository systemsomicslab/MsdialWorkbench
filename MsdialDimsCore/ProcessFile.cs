using CompMs.MsdialCore.Utility;
using CompMs.MsdialDimsCore.MsmsAll;
using CompMs.MsdialDimsCore.Parameter;
using System;

namespace CompMs.MsdialDimsCore {

    public enum ProcessType { MSMSALL }
    public class ProcessError {
        public string Messeage { get; set; } = string.Empty;
        public bool IsErrorOccured { get; set; } = false;
        public ProcessError() { }
        public ProcessError(bool isError, string message) {
            this.Messeage = message;
            this.IsErrorOccured = isError;
        }
    }
    public class ProcessFile {
        public void Run(string filepath, MsdialDimsParameter param, ProcessType type = ProcessType.MSMSALL) {
            switch (type) {
                case ProcessType.MSMSALL:
                    var msmsAllProcess = new MsmsAllProcess(filepath, param);
                    var error = msmsAllProcess.Run();
                    break;
            }
        }
    }
}
