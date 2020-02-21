using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Rfx.Riken.OsakaUniv;

namespace Msdial.StatisticsTests
{
    [TestClass]
    public class HCATest
    {
        [TestMethod]
        public void ValidCaseTest()
        {
            var statObject = new StatisticsObject();
            statObject.XDataMatrix = new double[,]
            {
                {0.28930059, 0.38002444},
                {0.36180131, 0.19259621},
                {0.14719256, 0.15078414},
                {0.45727788, 0.30089984},
                {0.182489  , 0.87303251},
                {0.32148578, 0.88993592},
                {0.01473583, 0.62397616},
                {0.02729128, 0.38599099},
                {0.10430449, 0.67930607},
                {0.55204816, 0.12145724}
            };
            StatisticsMathematics.HierarchicalClusterAnalysis(statObject);
            Assert.Fail("This test is not implemented.");
        }
    }
}
