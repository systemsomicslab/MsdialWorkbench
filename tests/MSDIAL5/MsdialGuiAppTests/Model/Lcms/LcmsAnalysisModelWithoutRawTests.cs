using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.DataObj.Tests;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Model.Service;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialLcmsApi.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Lcms.Tests
{
    [TestClass()]
    public class LcmsAnalysisModelWithoutRawTests
    {
        private string _folder = string.Empty;

        [TestInitialize()]
        public void Setup() {
            _folder = Path.Combine(Path.GetTempPath(), $"msdial-lcms-without-raw-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_folder);
            EnsureApplication();
        }

        // The peak spot filters are attached through Application.Current.Dispatcher, so the model needs an application
        // whose dispatcher runs; it gets one on its own STA thread, as the application's UI thread would be.
        private static void EnsureApplication() {
            if (System.Windows.Application.Current is not null) {
                return;
            }
            using var ready = new System.Threading.ManualResetEventSlim();
            var thread = new System.Threading.Thread(() => {
                _ = new System.Windows.Application { ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown, };
                ready.Set();
                System.Windows.Threading.Dispatcher.Run();
            })
            {
                IsBackground = true,
            };
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            ready.Wait();
        }

        [TestCleanup()]
        public void Cleanup() {
            Directory.Delete(_folder, recursive: true);
        }

        [TestMethod()]
        public void ASampleWithoutRawDataOpensFromWhatTheProjectStores() {
            var file = AnalysisFileBeanModelTestHelper.Create();
            file.AnalysisFileName = "sample";
            file.AnalysisFilePath = Path.Combine(_folder, "sample.mzML"); // not written: the raw data are missing
            file.AcquisitionType = AcquisitionType.DDA;
            file.File.PeakAreaBeanInformationFilePath = Path.Combine(_folder, "sample.pai");
            file.File.DeconvolutionFilePath = Path.Combine(_folder, "sample.dcl");

            var feature = new ChromatogramPeakFeature
            {
                PeakID = 0,
                MasterPeakID = 0,
                MS1RawSpectrumIdTop = 12,
                MS2RawSpectrumID = 13,
                PrecursorMz = 300.1,
                ChromXs = new ChromXs(5.0, ChromXType.RT, ChromXUnit.Min),
                IonMode = IonMode.Positive,
            };
            feature.MS2RawSpectrumID2CE[13] = 20d;
            MsdialPeakSerializer.SaveChromatogramPeakFeatures(file.PeakListFilePath, new List<ChromatogramPeakFeature> { feature, });
            var msdec = new MSDecResult { ScanID = 0, PrecursorMz = 300.1, RawSpectrumID = 13, };
            msdec.Spectrum.Add(new SpectrumPeak(100.05, 1000d));
            msdec.Spectrum.Add(new SpectrumPeak(150.07, 500d));
            MsdecResultsWriter.Write(file.File.DeconvolutionFilePath, new List<MSDecResult> { msdec, });

            var parameter = new MsdialLcmsParameter();
            parameter.ProjectParam.TargetOmics = TargetOmics.Metabolomics;
            parameter.ProjectParam.ProjectFolderPath = _folder;
            var dataBases = DataBaseStorage.CreateEmpty();
            var mapper = new DataBaseMapper();
            var unobserved = 0;
            EventHandler<UnobservedTaskExceptionEventArgs> handler = (_, e) => unobserved++;
            TaskScheduler.UnobservedTaskException += handler;
            try {
                var stopwatch = Stopwatch.StartNew();
                using (var model = new LcmsAnalysisModel(
                    file,
                    RawDataUnavailableProvider.Instance,
                    dataBases,
                    mapper,
                    FacadeMatchResultEvaluator.FromDataBases(dataBases),
                    parameter,
                    new PeakFilterModel(DisplayFilter.All),
                    new PeakSpotFiltering<ChromatogramPeakFeatureModel>(FilterEnableStatus.All),
                    new FilePropertiesModel(parameter.ProjectParam),
                    new MsfinderSearcherFactory(dataBases, mapper, parameter, "MS-FINDER"),
                    new MessageBroker())) {
                    stopwatch.Stop();
                    Assert.IsTrue(stopwatch.Elapsed < TimeSpan.FromSeconds(2), $"Opening took {stopwatch.Elapsed}.");

                    Assert.IsFalse(model.IsRawDataAvailable);
                    Assert.IsNull(model.EicLoader);
                    Assert.AreEqual(RawDataGuard.EicTitle, model.EicModel.GraphTitle);
                    Assert.AreEqual(RawDataGuard.Ms1SpectrumTitle, model.SurveyScanModel.Elements.GraphTitle);
                    Assert.AreEqual(RawDataGuard.MeasureVsReferenceTitle, model.Ms2SpectrumModel.RawRefSpectrumModels.GraphTitle);

                    model.Target.Value = model.Ms1Peaks[0];

                    Assert.IsNotNull(model.MsdecResult.Value);
                    Assert.AreEqual(2, model.MsdecResult.Value!.Spectrum.Count);
                    StringAssert.Contains(model.PlotModel.GraphTitle, RawDataGuard.FileTitleSuffix);
                    Assert.AreEqual(RawDataGuard.EicTitle, model.EicModel.GraphTitle);
                    Assert.AreEqual(RawDataGuard.Ms1SpectrumTitle, model.SurveyScanModel.Elements.GraphTitle);
                }
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                Assert.AreEqual(0, unobserved);
            }
            finally {
                TaskScheduler.UnobservedTaskException -= handler;
            }
        }
    }
}
