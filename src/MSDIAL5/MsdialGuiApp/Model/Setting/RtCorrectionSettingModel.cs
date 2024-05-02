using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompMs.App.Msdial.Model.Setting
{
    internal sealed class RtCorrectionSettingModel : BindableBase, IDisposable
    {
        private readonly IReadOnlyList<AnalysisFileBean> _files;
        private readonly ParameterBase _parameter;
        private readonly RetentionTimeCorrectionBean[] _temporaryRtcs;

        public RtCorrectionSettingModel(IReadOnlyList<AnalysisFileBean> files, ParameterBase parameter)
        {
            _files = files;
            _parameter = parameter;
            _temporaryRtcs = files.Select(_ => new RetentionTimeCorrectionBean(Path.GetTempFileName())).ToArray();
            for (int i = 0; i < _files.Count; i++) {
                if (_files[i].RetentionTimeCorrectionBean is { OriginalRt: { } originalRt, RtDiff: { } rtDiff, PredictedRt: { } predictedRt }) {
                    _temporaryRtcs[i].OriginalRt = originalRt.ToList();
                    _temporaryRtcs[i].RtDiff = rtDiff.ToList();
                    _temporaryRtcs[i].PredictedRt = predictedRt.ToList();
                }
                if (_files[i].RetentionTimeCorrectionBean is { StandardList: { } std }) {
                    _temporaryRtcs[i].StandardList = std;
                }
            }
            ExecuteRtCorrection = parameter.AdvancedProcessOptionBaseParam.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.ExcuteRtCorrection;
        }

        public bool ExecuteRtCorrection {
            get => _executeRtCorrection;
            set => SetProperty(ref _executeRtCorrection, value);
        }
        private bool _executeRtCorrection;

        internal IReadOnlyList<AnalysisFileBean> Files => _files;
        internal ParameterBase Parameter => _parameter;
        internal RetentionTimeCorrectionBean[] TemporaryRtcs => _temporaryRtcs;

        public void Determine() {
            System.Diagnostics.Debug.Assert(_files.Count == _temporaryRtcs.Length);
            for (int i = 0; i < _files.Count; i++) {
                if (File.Exists(_temporaryRtcs[i].RetentionTimeCorrectionResultFilePath)) {
                    File.Move(_temporaryRtcs[i].RetentionTimeCorrectionResultFilePath, _files[i].RetentionTimeCorrectionBean.RetentionTimeCorrectionResultFilePath);
                    _files[i].RetentionTimeCorrectionBean.StandardList = _temporaryRtcs[i].StandardList;
                    _files[i].RetentionTimeCorrectionBean.Restore();
                }
            }
            _parameter.AdvancedProcessOptionBaseParam.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.ExcuteRtCorrection = ExecuteRtCorrection;
        }

        public void Discard() {
            for (int i = 0; i < _temporaryRtcs.Length; i++) {
                if (File.Exists(_temporaryRtcs[i].RetentionTimeCorrectionResultFilePath)) {
                    File.Delete(_temporaryRtcs[i].RetentionTimeCorrectionResultFilePath);
                }
            }
        }

        public void Dispose() {
            Discard();
        }

        ~RtCorrectionSettingModel() {
            Dispose();
        }
    }
}
