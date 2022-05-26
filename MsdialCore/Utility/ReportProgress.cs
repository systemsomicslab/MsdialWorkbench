using System;

namespace CompMs.MsdialCore.Utility {
    public sealed class ReportProgress {
        private readonly double _initialProgress;
        private readonly double _progressMax;
        private readonly Action<int> _reportAction;
        private readonly object _syncObject;

        public ReportProgress(double initialProgress, double progressMax, Action<int> reportAction) {
            _initialProgress = initialProgress;
            _progressMax = progressMax;
            _reportAction = reportAction;
            _syncObject = new object();
        }

        public double InitialProgress => _initialProgress;
        public double ProgressMax => _progressMax;
        public Action<int> ReportAction => _reportAction;

        public void Show(double current, double localMax) {
            lock (_syncObject) {
                Show(_initialProgress, _progressMax, current, localMax, _reportAction);
            }
        }

        public static void Show(double initial, double totalMax, double current, double localMax, Action<int> reportAction) {
            var progress = initial + current / localMax * totalMax;
            reportAction?.Invoke(((int)progress));
        }
    }

    public static class ReportProgressExtensions {
        public static ReportProgress FromLength(this Action<int> reportAction, double initialProgress, double progressMax) {
            return new ReportProgress(initialProgress, progressMax, reportAction);
        }

        public static ReportProgress FromRange(this Action<int> reportAction, double initialProgress, double endProgress) {
            return new ReportProgress(initialProgress, endProgress - initialProgress, reportAction);
        }
    }
}
