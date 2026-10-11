using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.DataObj.Tests;
using CompMs.Common.Components;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Loader.Tests
{
    [TestClass()]
    public class MsDecSpectrumFromFileLoaderTests
    {
        private string _folder = string.Empty;

        [TestInitialize()]
        public void Setup() {
            _folder = Path.Combine(Path.GetTempPath(), $"msdial-dcl-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_folder);
        }

        [TestCleanup()]
        public void Cleanup() {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Directory.Delete(_folder, recursive: true);
        }

        [TestMethod()]
        public void ASampleWithoutItsDclGivesNoSpectrumUntilTheFileIsBack() {
            var file = CreateFile();
            var loader = new MsDecSpectrumFromFileLoader(file);
            var peak = new ChromatogramPeakFeatureModel(new ChromatogramPeakFeature { PeakID = 0, MasterPeakID = 0, });

            Assert.IsNull(loader.LoadMSDecResult(peak));

            WriteDcl(file);
            var result = loader.LoadMSDecResult(peak);
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result!.Spectrum.Count);
            file.ReleaseMSDecLoader();
        }

        [TestMethod()]
        public async Task AnAlignedSampleWithoutItsDclGivesNoSpectrum() {
            var file = CreateFile();
            IMsSpectrumLoader<AlignmentSpotPropertyModel> loader = new MsDecSpectrumFromFileLoader(file);
            var spot = new AlignmentSpotPropertyModel(new AlignmentSpotProperty
            {
                AlignedPeakProperties = new List<AlignmentChromPeakFeature> { new AlignmentChromPeakFeature { FileID = file.AnalysisFileId, MasterPeakID = 0, }, },
            });

            var scan = await loader.LoadScanAsObservable(spot).FirstAsync().Timeout(TimeSpan.FromSeconds(10));

            Assert.IsNull(scan);
        }

        private AnalysisFileBeanModel CreateFile() {
            var file = AnalysisFileBeanModelTestHelper.Create();
            file.AnalysisFileId = 3;
            file.File.DeconvolutionFilePath = Path.Combine(_folder, "sample.dcl");
            return file;
        }

        private static void WriteDcl(AnalysisFileBeanModel file) {
            var msdec = new MSDecResult { ScanID = 0, };
            msdec.Spectrum.Add(new SpectrumPeak(100.05, 1000d));
            MsdecResultsWriter.Write(file.File.DeconvolutionFilePath, new List<MSDecResult> { msdec, });
        }
    }
}
