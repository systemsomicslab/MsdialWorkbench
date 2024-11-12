using System;

namespace CompMs.MsdialCore.Utility; 
public sealed class ReportProgress(IProgress<int>? progress, double initialProgress, double progressMax) : IProgress<double>
{
    private readonly IProgress<int>? _progress = progress;
    private readonly object _syncObject = new();

    public double InitialProgress { get; } = initialProgress;
    public double ProgressMax { get; } = progressMax;

    public void Report(double current, double localMax) {
        if (_progress is null || localMax == 0d) {
            return;
        }
        ReportCore(current / localMax);
    }

    public void Report(double value) {
        if (_progress is null) {
            return;
        }
        ReportCore(value);
    }

    private void ReportCore(double value) {
        System.Diagnostics.Debug.Assert(_progress is not null, "Progress is null");
        var progress = InitialProgress + value * ProgressMax;
        lock (_syncObject) {
            _progress.Report((int)progress);
        }
    }

    public static ReportProgress FromLength(Action<int>? reportAction, double initialProgress, double progressLength) {
        return new ReportProgress(reportAction is not null ? new Progress<int>(reportAction) : null, initialProgress, progressLength);
    }

    public static ReportProgress FromRange(Action<int>? reportAction, double initialProgress, double endProgress) {
        return new ReportProgress(reportAction is not null ? new Progress<int>(reportAction) : null, initialProgress, endProgress - initialProgress);
    }

    public static ReportProgress FromLength(IProgress<int>? progress, double initialProgress, double progressLength) {
        return new ReportProgress(progress, initialProgress, progressLength);
    }

    public static ReportProgress FromRange(IProgress<int>? progress, double initialProgress, double endProgress) {
        return new ReportProgress(progress, initialProgress, endProgress - initialProgress);
    }

    public static ReportProgress FromLength(Action<double>? reportAction, double initialProgress, double progressLength) {
        return new ReportProgress(reportAction is not null ? new Progress<int>(v => reportAction.Invoke(v)) : null, initialProgress, progressLength);
    }

    public static ReportProgress FromRange(Action<double>? reportAction, double initialProgress, double endProgress) {
        return new ReportProgress(reportAction is not null ? new Progress<int>(v => reportAction.Invoke(v)) : null, initialProgress, endProgress - initialProgress);
    }
}
