using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Service;
using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Loader.Tests
{
    [TestClass()]
    public class RawDataUnavailableLoaderTests
    {
        [TestMethod()]
        public async Task AScanWithoutRawDataIsNullWhileItsListComesFromThePeakList() {
            using var loader = new MultiMsmsRawSpectrumLoader(RawDataUnavailableProvider.Instance, new ParameterBase());
            var lists = new List<List<MsSelectionItem>>();
            using var subscription = loader.Ms2List.Subscribe(lists.Add);
            var feature = new ChromatogramPeakFeature { MS2RawSpectrumID = 10, };
            feature.MS2RawSpectrumID2CE[10] = 20d;
            feature.MS2RawSpectrumID2CE[11] = 40d;
            var peak = new ChromatogramPeakFeatureModel(feature);

            Exception? error = null;
            var scan = await loader.LoadScanAsObservable(peak).Do(_ => { }, ex => error = ex).FirstAsync();

            Assert.IsNull(scan);
            Assert.IsNull(error);
            Assert.AreEqual(2, lists.Single().Count);
        }

        [TestMethod()]
        public async Task TheEicWithoutRawDataIsEmptyAndSaysWhy() {
            var loader = new RawDataUnavailableChromatogramLoader(ChromXType.RT, ChromXUnit.Min);

            var chromatogram = await loader.LoadChromatogramAsync(new ChromatogramPeakFeatureModel(new ChromatogramPeakFeature()), default);

            Assert.AreEqual(RawDataGuard.EicTitle, chromatogram.Description);
            Assert.AreEqual(0, chromatogram.Peaks.Count);
            Assert.AreEqual(RawDataGuard.EicTitle, loader.EmptyChromatogram.Description);
        }
    }
}
