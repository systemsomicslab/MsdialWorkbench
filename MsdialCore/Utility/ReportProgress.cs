using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.MsdialCore.Utility {
    public sealed class ReportProgress {
        private ReportProgress() { }

        public static void Show(double initial, double totalMax, double current, double localMax, Action<int> reportAction) {
            var progress = initial + current / localMax * totalMax;
            reportAction?.Invoke(((int)progress));
        }
    }
}
