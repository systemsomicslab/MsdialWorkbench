using CompMs.App.Msdial.UiTests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[assembly: DoNotParallelize]

namespace CompMs.App.Msdial.UiTests
{
    [TestClass]
    [TestCategory("UI")]
    public class StartupTests
    {
        public TestContext TestContext { get; set; } = null!;

        [TestMethod]
        public void StartsAndClosesWithoutCrashing() {
            var exe = RunParameters.RequireMsdialExe(TestContext);

            using var session = MsdialSession.Launch(TestContext, exe);
            Assert.AreEqual("MSDIAL", session.MainWindow.Title);

            session.Close();
        }
    }
}
