using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.Algorithm.Tests;

[TestClass()]
public class RawDataUnavailableProviderTests
{
    [TestMethod()]
    public void EveryLoadIsEmpty() {
        var provider = RawDataUnavailableProvider.Instance;

        Assert.AreEqual(0, provider.LoadMsSpectrums().Count);
        Assert.AreEqual(0, provider.LoadMs1Spectrums().Count);
        Assert.AreEqual(0, provider.LoadMsNSpectrums(2).Count);
    }

    [TestMethod()]
    public void EveryAsyncLoadHasAlreadyCompleted() {
        var provider = RawDataUnavailableProvider.Instance;

        var tasks = new[]
        {
            provider.LoadMsSpectrumsAsync(default),
            provider.LoadMs1SpectrumsAsync(default),
            provider.LoadMsNSpectrumsAsync(2, default),
        };

        foreach (var task in tasks) {
            Assert.AreEqual(TaskStatus.RanToCompletion, task.Status);
            Assert.AreEqual(0, task.Result.Count);
        }
    }
}
