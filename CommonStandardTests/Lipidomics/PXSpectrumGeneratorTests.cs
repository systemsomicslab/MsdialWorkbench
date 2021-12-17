using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public class PSSpectrumGeneratorTests
    {
        [TestMethod()]
        public void GenerateTest()
        {
            var acyl1 = new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));
            var acyl2 = new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));
            var lipid = new Lipid(LbmClass.PS,787.53633471, new PositionLevelChains(acyl1, acyl2));

            var generator = new PSSpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIonParser.GetAdductIonBean("[M+H]+"));

            var expects = new[]
            {
                186.01749   ,	//	Header
                226.05066   ,	//	Gly-C
                228.0253    ,	//	Gly-O
                307.2637   ,	//	"-Header -CH2(Sn1) calc
                321.27924   ,	//	"-Header -acylChain -O
                339.29147   ,	//	"-Header -acylChain
                493.27316   ,	//	"-CH2(Sn1)
                507.30017   ,	//	" -acylChain -O
                524.29663   ,	//	" -acylChain
                551.2854    ,	//	Sn1 ,Sn2 1 calc
                565.301     ,	//	Sn1 ,Sn2 2 calc
                579.3167    ,	//	Sn1 ,Sn2 3 calc
                593.3323    ,	//	Sn1 ,Sn2 4 calc
                603.54462   ,	//	Precursor -C3H8NO6P
                607.348     ,	//	Sn1 ,Sn2 5 calc
                621.3636    ,	//	Sn1 ,Sn2 6 calc
                635.3793    ,	//	Sn1 ,Sn2 7 calc
                649.3949    ,	//	Sn1 ,Sn2 8 calc
                662.4027    ,	//	Sn1 ,Sn2 9 calc
                675.4106    ,	//	Sn1 ,Sn2 10 calc
                689.4262    ,	//	Sn1 ,Sn2 11 calc
                703.4419    ,	//	Sn1 ,Sn2 12 calc
                717.4575    ,	//	Sn1 ,Sn2 13 calc
                731.4732    ,	//	Sn1 ,Sn2 14 calc
                743.54816   ,	//	Precursor -CHO2
                745.4888    ,	//	Sn1 ,Sn2 15 calc
                759.5045    ,	//	Sn1 ,Sn2 16 calc
                770.53375   ,	//	Precursor -H2O
                773.5201    ,	//	Sn1 ,Sn2 17 calc
                788.54663   ,	//	Precursor
            };

            scan.Spectrum.ForEach(spec => System.Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.Zip(scan.Spectrum.Select(spec => spec.Mass)))
            {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }
    }

    [TestClass()]

    public class PASpectrumGeneratorTests
    {
        [TestMethod()]
        public void GenerateTest()
        {
            //standard PA 18:1(n-9)/18:1(n-9)
            var acyl1 = new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));
            var acyl2 = new AcylChain(18, DoubleBond.CreateFromPosition(9), new Oxidized(0));
            var lipid = new Lipid(LbmClass.PA,700.504306309, new PositionLevelChains(acyl1, acyl2));

            var generator = new PASpectrumGenerator();
            var scan = lipid.GenerateSpectrum(generator, AdductIonParser.GetAdductIonBean("[M+NH4]+"));

            var expects = new[]
            {
                98.98036   ,	//	Header
                139.01358   ,	//	Gly-C
                140.993    ,	//	Gly-O
                307.2632696   ,	//	"-Header -CH2(Sn1) calc
                321.27924   ,	//	"-Header -acylChain -O
                339.29147   ,	//	"-Header -acylChain
                405.2400696   ,	//	"-CH2(Sn1) calc
                419.2556   ,	//	" -acylChain -O
                437.2662844   ,	//	" -acylChain calc
                464.2534   ,	//	Sn1 ,Sn2 1 calc
                478.269 ,	//	Sn1 ,Sn2 2 calc
                492.2847    ,	//	Sn1 ,Sn2 3 calc
                506.3003    ,	//	Sn1 ,Sn2 4 calc
                520.316 ,	//	Sn1 ,Sn2 5 calc
                534.3316    ,	//	Sn1 ,Sn2 6 calc
                548.3473    ,	//	Sn1 ,Sn2 7 calc
                562.3629    ,	//	Sn1 ,Sn2 8 calc
                575.3707    ,	//	Sn1 ,Sn2 9 calc
                588.3786    ,	//	Sn1 ,Sn2 10 calc
                602.3942    ,	//	Sn1 ,Sn2 11 calc
                603.5349    ,	//	Header loss
                616.4099    ,	//	Sn1 ,Sn2 12 calc
                630.4255    ,	//	Sn1 ,Sn2 13 calc
                644.4412    ,	//	Sn1 ,Sn2 14 calc
                658.4568    ,	//	Sn1 ,Sn2 15 calc
                672.4725    ,	//	Sn1 ,Sn2 16 calc
                686.4881    ,	//	Sn1 ,Sn2 17 calc
                701.5116    ,	//	[M+H]+
                718.5339   ,	//	Precursor
            };

            scan.Spectrum.ForEach(spec => System.Console.WriteLine($"Mass {spec.Mass}, Intensity {spec.Intensity}, Comment {spec.Comment}"));
            foreach ((var expect, var actual) in expects.Zip(scan.Spectrum.Select(spec => spec.Mass)))
            {
                Assert.AreEqual(expect, actual, 0.01d);
            }
        }
    }

}