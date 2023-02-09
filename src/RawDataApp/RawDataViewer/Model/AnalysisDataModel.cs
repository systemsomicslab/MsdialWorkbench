using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.RawDataHandler.Core;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.RawDataViewer.Model
{
    public class AnalysisDataModel : DisposableModelBase
    {
        public AnalysisDataModel(AnalysisFileBean analysisFile, MachineCategory machineCategory, IonMode ionMode, bool useProfile) {
            AnalysisFile = analysisFile;
            MachineCategory = machineCategory;
            IonMode = ionMode;
            rawMeasurementTask = Task.Run(() => LoadData(AnalysisFile, useProfile));
        }

        public AnalysisFileBean AnalysisFile { get; }
        public MachineCategory MachineCategory { get; }
        public IonMode IonMode { get; }

        private Task<RawMeasurement> rawMeasurementTask;

        public Task<IDataProvider> CreateDataProvider(CancellationToken token = default) {
            if (dataProvider is null) {
                dataProvider = Task.Run(async () => (IDataProvider)new StandardDataProvider(await rawMeasurementTask.ConfigureAwait(false)), token);
            }
            return dataProvider;
        }
        private Task<IDataProvider> dataProvider = null;

        public Task<IDataProvider> CreateAccumulatedDataProvider(CancellationToken token = default) {
            if (accumulatedDataProvider is null) {
                accumulatedDataProvider = Task.Run(async () => (IDataProvider)new MsdialLcImMsApi.Algorithm.LcimmsAccumulateDataProvider(await rawMeasurementTask.ConfigureAwait(false)), token);
            }
            return accumulatedDataProvider;
        }
        private Task<IDataProvider> accumulatedDataProvider = null;

        public async Task<IDataProvider> CreateDataProviderByFactory(IDataProviderFactory<RawMeasurement> factory, CancellationToken token = default) {
            var measurement = await rawMeasurementTask.ConfigureAwait(false);
            return factory.Create(measurement);
        }

        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);
            if (disposing) {
                rawMeasurementTask = null;
                dataProvider = null;
            }
        }

        private static RawMeasurement LoadData(AnalysisFileBean analysisFile, bool useProfile) {
            RawMeasurement result = null;
            for (int i = 0; i < 5; i++) {
                result = new RawDataAccess(analysisFile.AnalysisFilePath, analysisFile.AnalysisFileId, useProfile, false, isGuiProcess: true, correctedRts: analysisFile.RetentionTimeCorrectionBean.PredictedRt)?.GetMeasurement();
                if (!(result is null)) {
                    break;
                }
                Thread.Sleep(500);
            }
            if (result is null) {
                throw new Exception($"Loading {analysisFile.AnalysisFilePath} is failed.");
            }
            return result;
        }
    }
}
