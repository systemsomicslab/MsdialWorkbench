using Microsoft.VisualStudio.TestTools.UnitTesting;
using CompMs.Common.Extension;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.Common.Extension.Tests
{
    [TestClass()]
    public class ReadOnlyListExtensionTests
    {
        [TestMethod()]
        public void IndexOfTest() {
            List<string> actuals = new List<string>();
            IReadOnlyList<string> expected = actuals;

            actuals.Add("Tyrannosaurus");
            actuals.Add("Amargasaurus");
            actuals.Add("Mamenchisaurus");
            actuals.Add("Brachiosaurus");
            actuals.Add("Deinonychus");
            actuals.Add("Tyrannosaurus");
            actuals.Add("Compsognathus");

            Assert.AreEqual(actuals.IndexOf("Tyrannosaurus"), expected.IndexOf("Tyrannosaurus"));
            Assert.AreEqual(actuals.IndexOf("Tyrannosaurus", 3), expected.IndexOf("Tyrannosaurus", 3));
            Assert.AreEqual(actuals.IndexOf("Tyrannosaurus", 2, 2), expected.IndexOf("Tyrannosaurus", 2, 2));
        }
    }
}