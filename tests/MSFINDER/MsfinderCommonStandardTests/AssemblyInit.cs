using CompMs.Common.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Riken.Metabolomics.MsfinderCommon.Tests
{
    [TestClass]
    public static class AssemblyInit
    {
        /// <summary>
        /// Both MS-FINDER processes fix the culture of the process at start-up, and the settings file is
        /// written with it, so the tests run under the same culture.
        /// </summary>
        [AssemblyInitialize]
        public static void UseInvariantCulture(TestContext context) {
            ProcessCulture.UseInvariantCulture();
        }
    }
}
