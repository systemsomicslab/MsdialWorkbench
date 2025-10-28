using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
#if NETSTANDARD || NETFRAMEWORK
using CompMs.Common.Extension;
#endif
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Lipidomics.Tests
{
    [TestClass()]
    public class SpectrumPeakGeneratorTests
    {
        [TestMethod()]
        [DynamicData(nameof(GetAcylDoubleBondSpectrumTestDatas), DynamicDataSourceType.Method)]
        public void GetAcylDoubleBondSpectrumTest(Lipid lipid, AcylChain acylChain, AdductIon adduct, double nlMass, double abundance, SpectrumPeak[] expected) {
            var spectrumGenerator = new SpectrumPeakGenerator();

            var actual = spectrumGenerator.GetAcylDoubleBondSpectrum(lipid, acylChain, adduct, nlMass, abundance);

            if (expected.Length == 0) {
                Assert.AreEqual(0, actual.Count());
                return;
            }

            foreach ((var e, var a) in expected.ZipInternal(actual)) {
                Assert.AreEqual(e.Mass, a.Mass, 0.001);
                //Assert.AreEqual(e.Intensity, a.Intensity, 0.1);
            }
        }

        public static IEnumerable<object[]> GetAcylDoubleBondSpectrumTestDatas() {
            var acyl1 = new AcylChain(18, DoubleBond.CreateFromPosition(), Oxidized.CreateFromPosition());
            var acyl2 = new AcylChain(18, DoubleBond.CreateFromPosition(9, 12), Oxidized.CreateFromPosition());
            var acyl3 = new AcylChain(18, new DoubleBond(2), Oxidized.CreateFromPosition());

            yield return new object[]
            {
                new Lipid(LbmClass.PC, 785.593457, new PositionLevelChains(acyl1, acyl2)),
                acyl1,
                AdductIon.GetAdductIon("[M+H]+"),
                0d,
                100d,
                new SpectrumPeak[]
                {
                    new SpectrumPeak(546.319030,  50d), // C1 -H
                    new SpectrumPeak(547.326856, 100d), // C1
                    new SpectrumPeak(548.334681,  50d), // C1 +H
                    new SpectrumPeak(560.334681,  50d), // C2 -H
                    new SpectrumPeak(561.342506, 100d), // C2
                    new SpectrumPeak(562.350331,  50d), // C2 +H
                    new SpectrumPeak(574.350331,  50d), // C3 -H
                    new SpectrumPeak(575.358156, 100d), // C3
                    new SpectrumPeak(576.365981,  50d), // C3 +H
                    new SpectrumPeak(588.365981,  50d), // C4 -H
                    new SpectrumPeak(589.373806, 100d), // C4
                    new SpectrumPeak(590.381631,  50d), // C4 +H
                    new SpectrumPeak(602.381631,  50d), // C5 -H
                    new SpectrumPeak(603.389456, 100d), // C5
                    new SpectrumPeak(604.397281,  50d), // C5 +H
                    new SpectrumPeak(616.397281,  50d), // C6 -H
                    new SpectrumPeak(617.405106, 100d), // C6
                    new SpectrumPeak(618.412931,  50d), // C6 +H
                    new SpectrumPeak(630.412931,  50d), // C7 -H
                    new SpectrumPeak(631.420756, 100d), // C7
                    new SpectrumPeak(632.428581,  50d), // C7 +H
                    new SpectrumPeak(644.428581,  50d), // C8 -H
                    new SpectrumPeak(645.436406, 100d), // C8
                    new SpectrumPeak(646.444231,  50d), // C8 +H
                    new SpectrumPeak(658.444231,  50d), // C9 -H
                    new SpectrumPeak(659.452056, 100d), // C9
                    new SpectrumPeak(660.459881,  50d), // C9 +H
                    new SpectrumPeak(672.459881,  50d), // C10 -H
                    new SpectrumPeak(673.467706, 100d), // C10
                    new SpectrumPeak(674.475531,  50d), // C10 +H
                    new SpectrumPeak(686.475531,  50d), // C11 -H
                    new SpectrumPeak(687.483356, 100d), // C11
                    new SpectrumPeak(688.491181,  50d), // C11 +H
                    new SpectrumPeak(700.491181,  50d), // C12 -H
                    new SpectrumPeak(701.499006, 100d), // C12
                    new SpectrumPeak(702.506831,  50d), // C12 +H
                    new SpectrumPeak(714.506831,  50d), // C13 -H
                    new SpectrumPeak(715.514656, 100d), // C13
                    new SpectrumPeak(716.522481,  50d), // C13 +H
                    new SpectrumPeak(728.522481,  50d), // C14 -H
                    new SpectrumPeak(729.530306, 100d), // C14
                    new SpectrumPeak(730.538131,  50d), // C14 +H
                    new SpectrumPeak(742.538131,  50d), // C15 -H
                    new SpectrumPeak(743.545956, 100d), // C15
                    new SpectrumPeak(744.553781,  50d), // C15 +H
                    new SpectrumPeak(756.553781,  50d), // C16 -H
                    new SpectrumPeak(757.561606, 100d), // C16
                    new SpectrumPeak(758.569431,  50d), // C16 +H
                    new SpectrumPeak(770.569431,  50d), // C17 -H
                    new SpectrumPeak(771.577257, 100d), // C17
                    new SpectrumPeak(772.585082,  50d), // C17 +H
                }
            };

            yield return new object[]
            {
                new Lipid(LbmClass.PC, 785.593457, new PositionLevelChains(acyl1, acyl2)),
                acyl2,
                AdductIon.GetAdductIon("[M+H]+"),
                0d,
                100d,
                new SpectrumPeak[]
                {
                    new SpectrumPeak(550.350331,  50d), // C1 -H
                    new SpectrumPeak(551.358156, 100d), // C1
                    new SpectrumPeak(552.365981,  50d), // C1 +H
                    new SpectrumPeak(564.365981,  50d), // C2 -H
                    new SpectrumPeak(565.373806, 100d), // C2
                    new SpectrumPeak(566.381631,  50d), // C2 +H
                    new SpectrumPeak(578.381631,  50d), // C3 -H
                    new SpectrumPeak(579.389456, 100d), // C3
                    new SpectrumPeak(580.397281,  50d), // C3 +H
                    new SpectrumPeak(592.397281,  50d), // C4 -H
                    new SpectrumPeak(593.405106, 100d), // C4
                    new SpectrumPeak(594.412931,  50d), // C4 +H
                    new SpectrumPeak(606.412931,  50d), // C5 -H
                    new SpectrumPeak(607.420756, 100d), // C5
                    new SpectrumPeak(608.428581,  50d), // C5 +H
                    new SpectrumPeak(620.428581,  50d), // C6 -H
                    new SpectrumPeak(621.436406, 100d), // C6
                    new SpectrumPeak(622.444231,  50d), // C6 +H
                    new SpectrumPeak(634.444231,  50d), // C7 -H
                    new SpectrumPeak(635.452056, 100d), // C7
                    new SpectrumPeak(636.459881,  50d), // C7 +H
                    new SpectrumPeak(648.459881,  50d), // C8 -H
                    new SpectrumPeak(649.467706, 100d), // C8
                    new SpectrumPeak(650.475531,  50d), // C8 +H
                    new SpectrumPeak(661.467706,  25d), // C9 -H
                    new SpectrumPeak(662.475531,  50d), // C9
                    new SpectrumPeak(663.483356,  25d), // C9 +H
                    new SpectrumPeak(674.475531,  50d), // C10 -H
                    new SpectrumPeak(675.483356, 100d), // C10
                    new SpectrumPeak(676.491181,  50d), // C10 +H
                    new SpectrumPeak(688.491181, 150d), // C11 -H
                    new SpectrumPeak(689.499006, 300d), // C11
                    new SpectrumPeak(690.506831, 150d), // C11 +H
                    new SpectrumPeak(701.499006,  25d), // C12 -H
                    new SpectrumPeak(702.506831,  50d), // C12
                    new SpectrumPeak(703.514656,  25d), // C12 +H
                    new SpectrumPeak(714.506831,  50d), // C13 -H
                    new SpectrumPeak(715.514656, 100d), // C13
                    new SpectrumPeak(716.522481,  50d), // C13 +H
                    new SpectrumPeak(728.522481, 150d), // C14 -H
                    new SpectrumPeak(729.530306, 300d), // C14
                    new SpectrumPeak(730.538131, 150d), // C14 +H
                    new SpectrumPeak(742.538131,  50d), // C15 -H
                    new SpectrumPeak(743.545956, 100d), // C15
                    new SpectrumPeak(744.553781,  50d), // C15 +H
                    new SpectrumPeak(756.553781,  50d), // C16 -H
                    new SpectrumPeak(757.561606, 100d), // C16
                    new SpectrumPeak(758.569431,  50d), // C16 +H
                    new SpectrumPeak(770.569431,  50d), // C17 -H
                    new SpectrumPeak(771.577257, 100d), // C17
                    new SpectrumPeak(772.585082,  50d), // C17 +H
                }
            };

            yield return new object[]
            {
                new Lipid(LbmClass.PC, 785.593457, new PositionLevelChains(acyl1, acyl3)),
                acyl3,
                AdductIon.GetAdductIon("[M+H]+"),
                0d,
                100d,
                new SpectrumPeak[] { }
            };
        } 

        [TestMethod()]
        [DynamicData(nameof(GetAlkylDoubleBondSpectrumTestDatas), DynamicDataSourceType.Method)]
        public void GetAlkylDoubleBondSpectrumTest(Lipid lipid, AlkylChain alkylChain, AdductIon adduct, double nlMass, double abundance, SpectrumPeak[] expected) {
            var spectrumGenerator = new SpectrumPeakGenerator();

            var actual = spectrumGenerator.GetAlkylDoubleBondSpectrum(lipid, alkylChain, adduct, nlMass, abundance);

            if (expected.Length == 0) {
                Assert.AreEqual(0, actual.Count());
                return;
            }

            foreach ((var e, var a) in expected.ZipInternal(actual)) {
                Assert.AreEqual(e.Mass, a.Mass, 0.001);
                //Assert.AreEqual(e.Intensity, a.Intensity, 0.1);
            }
        }

        public static IEnumerable<object[]> GetAlkylDoubleBondSpectrumTestDatas() {
            var alkyl1 = new AlkylChain(18, DoubleBond.CreateFromPosition(), Oxidized.CreateFromPosition());
            var alkyl2 = new AlkylChain(18, DoubleBond.CreateFromPosition(1), Oxidized.CreateFromPosition());
            var alkyl3 = new AlkylChain(18, DoubleBond.CreateFromPosition(9), Oxidized.CreateFromPosition());
            var alkyl4 = new AlkylChain(18, new DoubleBond(1), Oxidized.CreateFromPosition());
            var acyl1 = new AcylChain(18, DoubleBond.CreateFromPosition(12), Oxidized.CreateFromPosition());

            yield return new object[]
            {
                new Lipid(LbmClass.EtherPE, 729.567242, new PositionLevelChains(alkyl1, acyl1)),
                alkyl1,
                AdductIon.GetAdductIon("[M+H]+"),
                0d,
                100d,
                new SpectrumPeak[]
                {
                    new SpectrumPeak(490.292816,  50d), // C1 -H
                    new SpectrumPeak(491.300641, 100d), // C1
                    new SpectrumPeak(492.308466,  50d), // C1 +H
                    new SpectrumPeak(504.308466,  50d), // C2 -H
                    new SpectrumPeak(505.316291, 100d), // C2
                    new SpectrumPeak(506.324116,  50d), // C2 +H
                    new SpectrumPeak(518.324116,  50d), // C3 -H
                    new SpectrumPeak(519.331941, 100d), // C3
                    new SpectrumPeak(520.339766,  50d), // C3 +H
                    new SpectrumPeak(532.339766,  50d), // C4 -H
                    new SpectrumPeak(533.347591, 100d), // C4
                    new SpectrumPeak(534.355416,  50d), // C4 +H
                    new SpectrumPeak(546.355416,  50d), // C5 -H
                    new SpectrumPeak(547.363241, 100d), // C5
                    new SpectrumPeak(548.371066,  50d), // C5 +H
                    new SpectrumPeak(560.371066,  50d), // C6 -H
                    new SpectrumPeak(561.378891, 100d), // C6
                    new SpectrumPeak(562.386716,  50d), // C6 +H
                    new SpectrumPeak(574.386716,  50d), // C7 -H
                    new SpectrumPeak(575.394541, 100d), // C7
                    new SpectrumPeak(576.402366,  50d), // C7 +H
                    new SpectrumPeak(588.402366,  50d), // C8 -H
                    new SpectrumPeak(589.410191, 100d), // C8
                    new SpectrumPeak(590.418016,  50d), // C8 +H
                    new SpectrumPeak(602.418016,  50d), // C9 -H
                    new SpectrumPeak(603.425841, 100d), // C9
                    new SpectrumPeak(604.433666,  50d), // C9 +H
                    new SpectrumPeak(616.433666,  50d), // C10 -H
                    new SpectrumPeak(617.441491, 100d), // C10
                    new SpectrumPeak(618.449316,  50d), // C10 +H
                    new SpectrumPeak(630.449316,  50d), // C11 -H
                    new SpectrumPeak(631.457141, 100d), // C11
                    new SpectrumPeak(632.464966,  50d), // C11 +H
                    new SpectrumPeak(644.464966,  50d), // C12 -H
                    new SpectrumPeak(645.472791, 100d), // C12
                    new SpectrumPeak(646.480616,  50d), // C12 +H
                    new SpectrumPeak(658.480616,  50d), // C13 -H
                    new SpectrumPeak(659.488442, 100d), // C13
                    new SpectrumPeak(660.496267,  50d), // C13 +H
                    new SpectrumPeak(672.496267,  50d), // C14 -H
                    new SpectrumPeak(673.504092, 100d), // C14
                    new SpectrumPeak(674.511917,  50d), // C14 +H
                    new SpectrumPeak(686.511917,  50d), // C15 -H
                    new SpectrumPeak(687.519742, 100d), // C15
                    new SpectrumPeak(688.527567,  50d), // C15 +H
                    new SpectrumPeak(700.527567,  50d), // C16 -H
                    new SpectrumPeak(701.535392, 100d), // C16
                    new SpectrumPeak(702.543217,  50d), // C16 +H
                    new SpectrumPeak(714.543217,  50d), // C17 -H
                    new SpectrumPeak(715.551042, 100d), // C17
                    new SpectrumPeak(716.558867,  50d), // C17 +H
                }
            };

            yield return new object[]
            {
                new Lipid(LbmClass.EtherPE, 727.551592, new PositionLevelChains(alkyl2, acyl1)),
                alkyl2,
                AdductIon.GetAdductIon("[M+H]+"),
                0d,
                100d,
                new SpectrumPeak[] {
                    new SpectrumPeak(489.284991,  25d), // C1 -H
                    new SpectrumPeak(490.292816,  50d), // C1
                    new SpectrumPeak(491.300641,  25d), // C1 +H
                    new SpectrumPeak(502.292816,  50d), // C2 -H
                    new SpectrumPeak(503.300641, 100d), // C2
                    new SpectrumPeak(504.308466,  50d), // C2 +H
                    new SpectrumPeak(516.308466, 150d), // C3 -H
                    new SpectrumPeak(517.316291, 300d), // C3
                    new SpectrumPeak(518.324116, 150d), // C3 +H
                    new SpectrumPeak(530.324116,  50d), // C4 -H
                    new SpectrumPeak(531.331941, 100d), // C4
                    new SpectrumPeak(532.339766,  50d), // C4 +H
                    new SpectrumPeak(544.339766,  50d), // C5 -H
                    new SpectrumPeak(545.347591, 100d), // C5
                    new SpectrumPeak(546.355416,  50d), // C5 +H
                    new SpectrumPeak(558.355416,  50d), // C6 -H
                    new SpectrumPeak(559.363241, 100d), // C6
                    new SpectrumPeak(560.371066,  50d), // C6 +H
                    new SpectrumPeak(572.371066,  50d), // C7 -H
                    new SpectrumPeak(573.378891, 100d), // C7
                    new SpectrumPeak(574.386716,  50d), // C7 +H
                    new SpectrumPeak(586.386716,  50d), // C8 -H
                    new SpectrumPeak(587.394541, 100d), // C8
                    new SpectrumPeak(588.402366,  50d), // C8 +H
                    new SpectrumPeak(600.402366,  50d), // C9 -H
                    new SpectrumPeak(601.410191, 100d), // C9
                    new SpectrumPeak(602.418016,  50d), // C9 +H
                    new SpectrumPeak(614.418016,  50d), // C10 -H
                    new SpectrumPeak(615.425841, 100d), // C10
                    new SpectrumPeak(616.433666,  50d), // C10 +H
                    new SpectrumPeak(628.433666,  50d), // C11 -H
                    new SpectrumPeak(629.441491, 100d), // C11
                    new SpectrumPeak(630.449316,  50d), // C11 +H
                    new SpectrumPeak(642.449316,  50d), // C12 -H
                    new SpectrumPeak(643.457141, 100d), // C12
                    new SpectrumPeak(644.464966,  50d), // C12 +H
                    new SpectrumPeak(656.464966,  50d), // C13 -H
                    new SpectrumPeak(657.472791, 100d), // C13
                    new SpectrumPeak(658.480616,  50d), // C13 +H
                    new SpectrumPeak(670.480616,  50d), // C14 -H
                    new SpectrumPeak(671.488442, 100d), // C14
                    new SpectrumPeak(672.496267,  50d), // C14 +H
                    new SpectrumPeak(684.496267,  50d), // C15 -H
                    new SpectrumPeak(685.504092, 100d), // C15
                    new SpectrumPeak(686.511917,  50d), // C15 +H
                    new SpectrumPeak(698.511917,  50d), // C16 -H
                    new SpectrumPeak(699.519742, 100d), // C16
                    new SpectrumPeak(700.527567,  50d), // C16 +H
                    new SpectrumPeak(712.527567,  50d), // C17 -H
                    new SpectrumPeak(713.535392, 100d), // C17
                    new SpectrumPeak(714.543217,  50d), // C17 +H
                }
            };

            yield return new object[]
            {
                new Lipid(LbmClass.EtherPE, 727.551592, new PositionLevelChains(alkyl3, acyl1)),
                alkyl3,
                AdductIon.GetAdductIon("[M+H]+"),
                0d,
                100d,
                new SpectrumPeak[]
                {
                    new SpectrumPeak(490.292816,  50d), // C1 -H
                    new SpectrumPeak(491.300641, 100d), // C1
                    new SpectrumPeak(492.308466,  50d), // C1 +H
                    new SpectrumPeak(504.308466,  50d), // C2 -H
                    new SpectrumPeak(505.316291, 100d), // C2
                    new SpectrumPeak(506.324116,  50d), // C2 +H
                    new SpectrumPeak(518.324116,  50d), // C3 -H
                    new SpectrumPeak(519.331941, 100d), // C3
                    new SpectrumPeak(520.339766,  50d), // C3 +H
                    new SpectrumPeak(532.339766,  50d), // C4 -H
                    new SpectrumPeak(533.347591, 100d), // C4
                    new SpectrumPeak(534.355416,  50d), // C4 +H
                    new SpectrumPeak(546.355416,  50d), // C5 -H
                    new SpectrumPeak(547.363241, 100d), // C5
                    new SpectrumPeak(548.371066,  50d), // C5 +H
                    new SpectrumPeak(560.371066,  50d), // C6 -H
                    new SpectrumPeak(561.378891, 100d), // C6
                    new SpectrumPeak(562.386716,  50d), // C6 +H
                    new SpectrumPeak(574.386716,  50d), // C7 -H
                    new SpectrumPeak(575.394541, 100d), // C7
                    new SpectrumPeak(576.402366,  50d), // C7 +H
                    new SpectrumPeak(588.402366,  50d), // C8 -H
                    new SpectrumPeak(589.410191, 100d), // C8
                    new SpectrumPeak(590.418016,  50d), // C8 +H
                    new SpectrumPeak(601.410191,  25d), // C9 -H
                    new SpectrumPeak(602.418016,  50d), // C9
                    new SpectrumPeak(603.425841,  25d), // C9 +H
                    new SpectrumPeak(614.418016,  50d), // C10 -H
                    new SpectrumPeak(615.425841, 100d), // C10
                    new SpectrumPeak(616.433666,  50d), // C10 +H
                    new SpectrumPeak(628.433666, 150d), // C11 -H
                    new SpectrumPeak(629.441491, 300d), // C11
                    new SpectrumPeak(630.449316, 150d), // C11 +H
                    new SpectrumPeak(642.449316,  50d), // C12 -H
                    new SpectrumPeak(643.457141, 100d), // C12
                    new SpectrumPeak(644.464966,  50d), // C12 +H
                    new SpectrumPeak(656.464966,  50d), // C13 -H
                    new SpectrumPeak(657.472791, 100d), // C13
                    new SpectrumPeak(658.480616,  50d), // C13 +H
                    new SpectrumPeak(670.480616,  50d), // C14 -H
                    new SpectrumPeak(671.488442, 100d), // C14
                    new SpectrumPeak(672.496267,  50d), // C14 +H
                    new SpectrumPeak(684.496267,  50d), // C15 -H
                    new SpectrumPeak(685.504092, 100d), // C15
                    new SpectrumPeak(686.511917,  50d), // C15 +H
                    new SpectrumPeak(698.511917,  50d), // C16 -H
                    new SpectrumPeak(699.519742, 100d), // C16
                    new SpectrumPeak(700.527567,  50d), // C16 +H
                    new SpectrumPeak(712.527567,  50d), // C17 -H
                    new SpectrumPeak(713.535392, 100d), // C17
                    new SpectrumPeak(714.543217,  50d), // C17 +H
                }
            };

            yield return new object[]
            {
                new Lipid(LbmClass.EtherPE, 730.574517, new PositionLevelChains(alkyl4, acyl1)),
                alkyl4,
                AdductIon.GetAdductIon("[M+H]+"),
                0d,
                100d,
                new SpectrumPeak[] { }
            };
        } 
    }
}