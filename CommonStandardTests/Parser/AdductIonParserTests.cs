using Microsoft.VisualStudio.TestTools.UnitTesting;
using CompMs.Common.Parser;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.Common.Parser.Tests
{
    [TestClass()]
    public class AdductIonParserTests
    {
        [TestMethod()]
        public void IonTypeFormatCheckerTest() {
            Assert.IsTrue(AdductIonParser.IonTypeFormatChecker("[M+H]+"));

            Assert.IsFalse(AdductIonParser.IonTypeFormatChecker("M+H+"));
            Assert.IsFalse(AdductIonParser.IonTypeFormatChecker("[+"));
            Assert.IsFalse(AdductIonParser.IonTypeFormatChecker("[[]]+"));
            Assert.IsFalse(AdductIonParser.IonTypeFormatChecker("[M+H]"));

            // Assert.IsFalse(AdductIonParser.IonTypeFormatChecker("[[]+"));
            // Assert.IsFalse(AdductIonParser.IonTypeFormatChecker("[]+"));
        }
    }
}