using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
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

            foreach ((var e, var a) in expected.Zip(actual)) {
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
                    new SpectrumPeak(546.319030f,  50f), // C1 -H
                    new SpectrumPeak(547.326856f, 100f), // C1
                    new SpectrumPeak(548.334681f,  50f), // C1 +H
                    new SpectrumPeak(560.334681f,  50f), // C2 -H
                    new SpectrumPeak(561.342506f, 100f), // C2
                    new SpectrumPeak(562.350331f,  50f), // C2 +H
                    new SpectrumPeak(574.350331f,  50f), // C3 -H
                    new SpectrumPeak(575.358156f, 100f), // C3
                    new SpectrumPeak(576.365981f,  50f), // C3 +H
                    new SpectrumPeak(588.365981f,  50f), // C4 -H
                    new SpectrumPeak(589.373806f, 100f), // C4
                    new SpectrumPeak(590.381631f,  50f), // C4 +H
                    new SpectrumPeak(602.381631f,  50f), // C5 -H
                    new SpectrumPeak(603.389456f, 100f), // C5
                    new SpectrumPeak(604.397281f,  50f), // C5 +H
                    new SpectrumPeak(616.397281f,  50f), // C6 -H
                    new SpectrumPeak(617.405106f, 100f), // C6
                    new SpectrumPeak(618.412931f,  50f), // C6 +H
                    new SpectrumPeak(630.412931f,  50f), // C7 -H
                    new SpectrumPeak(631.420756f, 100f), // C7
                    new SpectrumPeak(632.428581f,  50f), // C7 +H
                    new SpectrumPeak(644.428581f,  50f), // C8 -H
                    new SpectrumPeak(645.436406f, 100f), // C8
                    new SpectrumPeak(646.444231f,  50f), // C8 +H
                    new SpectrumPeak(658.444231f,  50f), // C9 -H
                    new SpectrumPeak(659.452056f, 100f), // C9
                    new SpectrumPeak(660.459881f,  50f), // C9 +H
                    new SpectrumPeak(672.459881f,  50f), // C10 -H
                    new SpectrumPeak(673.467706f, 100f), // C10
                    new SpectrumPeak(674.475531f,  50f), // C10 +H
                    new SpectrumPeak(686.475531f,  50f), // C11 -H
                    new SpectrumPeak(687.483356f, 100f), // C11
                    new SpectrumPeak(688.491181f,  50f), // C11 +H
                    new SpectrumPeak(700.491181f,  50f), // C12 -H
                    new SpectrumPeak(701.499006f, 100f), // C12
                    new SpectrumPeak(702.506831f,  50f), // C12 +H
                    new SpectrumPeak(714.506831f,  50f), // C13 -H
                    new SpectrumPeak(715.514656f, 100f), // C13
                    new SpectrumPeak(716.522481f,  50f), // C13 +H
                    new SpectrumPeak(728.522481f,  50f), // C14 -H
                    new SpectrumPeak(729.530306f, 100f), // C14
                    new SpectrumPeak(730.538131f,  50f), // C14 +H
                    new SpectrumPeak(742.538131f,  50f), // C15 -H
                    new SpectrumPeak(743.545956f, 100f), // C15
                    new SpectrumPeak(744.553781f,  50f), // C15 +H
                    new SpectrumPeak(756.553781f,  50f), // C16 -H
                    new SpectrumPeak(757.561606f, 100f), // C16
                    new SpectrumPeak(758.569431f,  50f), // C16 +H
                    new SpectrumPeak(770.569431f,  50f), // C17 -H
                    new SpectrumPeak(771.577257f, 100f), // C17
                    new SpectrumPeak(772.585082f,  50f), // C17 +H
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
                    new SpectrumPeak(550.350331f,  50f), // C1 -H
                    new SpectrumPeak(551.358156f, 100f), // C1
                    new SpectrumPeak(552.365981f,  50f), // C1 +H
                    new SpectrumPeak(564.365981f,  50f), // C2 -H
                    new SpectrumPeak(565.373806f, 100f), // C2
                    new SpectrumPeak(566.381631f,  50f), // C2 +H
                    new SpectrumPeak(578.381631f,  50f), // C3 -H
                    new SpectrumPeak(579.389456f, 100f), // C3
                    new SpectrumPeak(580.397281f,  50f), // C3 +H
                    new SpectrumPeak(592.397281f,  50f), // C4 -H
                    new SpectrumPeak(593.405106f, 100f), // C4
                    new SpectrumPeak(594.412931f,  50f), // C4 +H
                    new SpectrumPeak(606.412931f,  50f), // C5 -H
                    new SpectrumPeak(607.420756f, 100f), // C5
                    new SpectrumPeak(608.428581f,  50f), // C5 +H
                    new SpectrumPeak(620.428581f,  50f), // C6 -H
                    new SpectrumPeak(621.436406f, 100f), // C6
                    new SpectrumPeak(622.444231f,  50f), // C6 +H
                    new SpectrumPeak(634.444231f,  50f), // C7 -H
                    new SpectrumPeak(635.452056f, 100f), // C7
                    new SpectrumPeak(636.459881f,  50f), // C7 +H
                    new SpectrumPeak(648.459881f,  50f), // C8 -H
                    new SpectrumPeak(649.467706f, 100f), // C8
                    new SpectrumPeak(650.475531f,  50f), // C8 +H
                    new SpectrumPeak(661.467706f,  25f), // C9 -H
                    new SpectrumPeak(662.475531f,  50f), // C9
                    new SpectrumPeak(663.483356f,  25f), // C9 +H
                    new SpectrumPeak(674.475531f,  50f), // C10 -H
                    new SpectrumPeak(675.483356f, 100f), // C10
                    new SpectrumPeak(676.491181f,  50f), // C10 +H
                    new SpectrumPeak(688.491181f, 150f), // C11 -H
                    new SpectrumPeak(689.499006f, 300f), // C11
                    new SpectrumPeak(690.506831f, 150f), // C11 +H
                    new SpectrumPeak(701.499006f,  25f), // C12 -H
                    new SpectrumPeak(702.506831f,  50f), // C12
                    new SpectrumPeak(703.514656f,  25f), // C12 +H
                    new SpectrumPeak(714.506831f,  50f), // C13 -H
                    new SpectrumPeak(715.514656f, 100f), // C13
                    new SpectrumPeak(716.522481f,  50f), // C13 +H
                    new SpectrumPeak(728.522481f, 150f), // C14 -H
                    new SpectrumPeak(729.530306f, 300f), // C14
                    new SpectrumPeak(730.538131f, 150f), // C14 +H
                    new SpectrumPeak(742.538131f,  50f), // C15 -H
                    new SpectrumPeak(743.545956f, 100f), // C15
                    new SpectrumPeak(744.553781f,  50f), // C15 +H
                    new SpectrumPeak(756.553781f,  50f), // C16 -H
                    new SpectrumPeak(757.561606f, 100f), // C16
                    new SpectrumPeak(758.569431f,  50f), // C16 +H
                    new SpectrumPeak(770.569431f,  50f), // C17 -H
                    new SpectrumPeak(771.577257f, 100f), // C17
                    new SpectrumPeak(772.585082f,  50f), // C17 +H
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

            foreach ((var e, var a) in expected.Zip(actual)) {
                Assert.AreEqual(e.Mass, a.Mass, 0.001);
                Assert.AreEqual(e.Intensity, a.Intensity, 0.1);
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
                    new SpectrumPeak(490.292816f,  50f), // C1 -H
                    new SpectrumPeak(491.300641f, 100f), // C1
                    new SpectrumPeak(492.308466f,  50f), // C1 +H
                    new SpectrumPeak(504.308466f,  50f), // C2 -H
                    new SpectrumPeak(505.316291f, 100f), // C2
                    new SpectrumPeak(506.324116f,  50f), // C2 +H
                    new SpectrumPeak(518.324116f,  50f), // C3 -H
                    new SpectrumPeak(519.331941f, 100f), // C3
                    new SpectrumPeak(520.339766f,  50f), // C3 +H
                    new SpectrumPeak(532.339766f,  50f), // C4 -H
                    new SpectrumPeak(533.347591f, 100f), // C4
                    new SpectrumPeak(534.355416f,  50f), // C4 +H
                    new SpectrumPeak(546.355416f,  50f), // C5 -H
                    new SpectrumPeak(547.363241f, 100f), // C5
                    new SpectrumPeak(548.371066f,  50f), // C5 +H
                    new SpectrumPeak(560.371066f,  50f), // C6 -H
                    new SpectrumPeak(561.378891f, 100f), // C6
                    new SpectrumPeak(562.386716f,  50f), // C6 +H
                    new SpectrumPeak(574.386716f,  50f), // C7 -H
                    new SpectrumPeak(575.394541f, 100f), // C7
                    new SpectrumPeak(576.402366f,  50f), // C7 +H
                    new SpectrumPeak(588.402366f,  50f), // C8 -H
                    new SpectrumPeak(589.410191f, 100f), // C8
                    new SpectrumPeak(590.418016f,  50f), // C8 +H
                    new SpectrumPeak(602.418016f,  50f), // C9 -H
                    new SpectrumPeak(603.425841f, 100f), // C9
                    new SpectrumPeak(604.433666f,  50f), // C9 +H
                    new SpectrumPeak(616.433666f,  50f), // C10 -H
                    new SpectrumPeak(617.441491f, 100f), // C10
                    new SpectrumPeak(618.449316f,  50f), // C10 +H
                    new SpectrumPeak(630.449316f,  50f), // C11 -H
                    new SpectrumPeak(631.457141f, 100f), // C11
                    new SpectrumPeak(632.464966f,  50f), // C11 +H
                    new SpectrumPeak(644.464966f,  50f), // C12 -H
                    new SpectrumPeak(645.472791f, 100f), // C12
                    new SpectrumPeak(646.480616f,  50f), // C12 +H
                    new SpectrumPeak(658.480616f,  50f), // C13 -H
                    new SpectrumPeak(659.488442f, 100f), // C13
                    new SpectrumPeak(660.496267f,  50f), // C13 +H
                    new SpectrumPeak(672.496267f,  50f), // C14 -H
                    new SpectrumPeak(673.504092f, 100f), // C14
                    new SpectrumPeak(674.511917f,  50f), // C14 +H
                    new SpectrumPeak(686.511917f,  50f), // C15 -H
                    new SpectrumPeak(687.519742f, 100f), // C15
                    new SpectrumPeak(688.527567f,  50f), // C15 +H
                    new SpectrumPeak(700.527567f,  50f), // C16 -H
                    new SpectrumPeak(701.535392f, 100f), // C16
                    new SpectrumPeak(702.543217f,  50f), // C16 +H
                    new SpectrumPeak(714.543217f,  50f), // C17 -H
                    new SpectrumPeak(715.551042f, 100f), // C17
                    new SpectrumPeak(716.558867f,  50f), // C17 +H
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
                    new SpectrumPeak(489.284991f,  25f), // C1 -H
                    new SpectrumPeak(490.292816f,  50f), // C1
                    new SpectrumPeak(491.300641f,  25f), // C1 +H
                    new SpectrumPeak(502.292816f,  50f), // C2 -H
                    new SpectrumPeak(503.300641f, 100f), // C2
                    new SpectrumPeak(504.308466f,  50f), // C2 +H
                    new SpectrumPeak(516.308466f, 150f), // C3 -H
                    new SpectrumPeak(517.316291f, 300f), // C3
                    new SpectrumPeak(518.324116f, 150f), // C3 +H
                    new SpectrumPeak(530.324116f,  50f), // C4 -H
                    new SpectrumPeak(531.331941f, 100f), // C4
                    new SpectrumPeak(532.339766f,  50f), // C4 +H
                    new SpectrumPeak(544.339766f,  50f), // C5 -H
                    new SpectrumPeak(545.347591f, 100f), // C5
                    new SpectrumPeak(546.355416f,  50f), // C5 +H
                    new SpectrumPeak(558.355416f,  50f), // C6 -H
                    new SpectrumPeak(559.363241f, 100f), // C6
                    new SpectrumPeak(560.371066f,  50f), // C6 +H
                    new SpectrumPeak(572.371066f,  50f), // C7 -H
                    new SpectrumPeak(573.378891f, 100f), // C7
                    new SpectrumPeak(574.386716f,  50f), // C7 +H
                    new SpectrumPeak(586.386716f,  50f), // C8 -H
                    new SpectrumPeak(587.394541f, 100f), // C8
                    new SpectrumPeak(588.402366f,  50f), // C8 +H
                    new SpectrumPeak(600.402366f,  50f), // C9 -H
                    new SpectrumPeak(601.410191f, 100f), // C9
                    new SpectrumPeak(602.418016f,  50f), // C9 +H
                    new SpectrumPeak(614.418016f,  50f), // C10 -H
                    new SpectrumPeak(615.425841f, 100f), // C10
                    new SpectrumPeak(616.433666f,  50f), // C10 +H
                    new SpectrumPeak(628.433666f,  50f), // C11 -H
                    new SpectrumPeak(629.441491f, 100f), // C11
                    new SpectrumPeak(630.449316f,  50f), // C11 +H
                    new SpectrumPeak(642.449316f,  50f), // C12 -H
                    new SpectrumPeak(643.457141f, 100f), // C12
                    new SpectrumPeak(644.464966f,  50f), // C12 +H
                    new SpectrumPeak(656.464966f,  50f), // C13 -H
                    new SpectrumPeak(657.472791f, 100f), // C13
                    new SpectrumPeak(658.480616f,  50f), // C13 +H
                    new SpectrumPeak(670.480616f,  50f), // C14 -H
                    new SpectrumPeak(671.488442f, 100f), // C14
                    new SpectrumPeak(672.496267f,  50f), // C14 +H
                    new SpectrumPeak(684.496267f,  50f), // C15 -H
                    new SpectrumPeak(685.504092f, 100f), // C15
                    new SpectrumPeak(686.511917f,  50f), // C15 +H
                    new SpectrumPeak(698.511917f,  50f), // C16 -H
                    new SpectrumPeak(699.519742f, 100f), // C16
                    new SpectrumPeak(700.527567f,  50f), // C16 +H
                    new SpectrumPeak(712.527567f,  50f), // C17 -H
                    new SpectrumPeak(713.535392f, 100f), // C17
                    new SpectrumPeak(714.543217f,  50f), // C17 +H
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
                    new SpectrumPeak(490.292816f,  50f), // C1 -H
                    new SpectrumPeak(491.300641f, 100f), // C1
                    new SpectrumPeak(492.308466f,  50f), // C1 +H
                    new SpectrumPeak(504.308466f,  50f), // C2 -H
                    new SpectrumPeak(505.316291f, 100f), // C2
                    new SpectrumPeak(506.324116f,  50f), // C2 +H
                    new SpectrumPeak(518.324116f,  50f), // C3 -H
                    new SpectrumPeak(519.331941f, 100f), // C3
                    new SpectrumPeak(520.339766f,  50f), // C3 +H
                    new SpectrumPeak(532.339766f,  50f), // C4 -H
                    new SpectrumPeak(533.347591f, 100f), // C4
                    new SpectrumPeak(534.355416f,  50f), // C4 +H
                    new SpectrumPeak(546.355416f,  50f), // C5 -H
                    new SpectrumPeak(547.363241f, 100f), // C5
                    new SpectrumPeak(548.371066f,  50f), // C5 +H
                    new SpectrumPeak(560.371066f,  50f), // C6 -H
                    new SpectrumPeak(561.378891f, 100f), // C6
                    new SpectrumPeak(562.386716f,  50f), // C6 +H
                    new SpectrumPeak(574.386716f,  50f), // C7 -H
                    new SpectrumPeak(575.394541f, 100f), // C7
                    new SpectrumPeak(576.402366f,  50f), // C7 +H
                    new SpectrumPeak(588.402366f,  50f), // C8 -H
                    new SpectrumPeak(589.410191f, 100f), // C8
                    new SpectrumPeak(590.418016f,  50f), // C8 +H
                    new SpectrumPeak(601.410191f,  25f), // C9 -H
                    new SpectrumPeak(602.418016f,  50f), // C9
                    new SpectrumPeak(603.425841f,  25f), // C9 +H
                    new SpectrumPeak(614.418016f,  50f), // C10 -H
                    new SpectrumPeak(615.425841f, 100f), // C10
                    new SpectrumPeak(616.433666f,  50f), // C10 +H
                    new SpectrumPeak(628.433666f, 150f), // C11 -H
                    new SpectrumPeak(629.441491f, 300f), // C11
                    new SpectrumPeak(630.449316f, 150f), // C11 +H
                    new SpectrumPeak(642.449316f,  50f), // C12 -H
                    new SpectrumPeak(643.457141f, 100f), // C12
                    new SpectrumPeak(644.464966f,  50f), // C12 +H
                    new SpectrumPeak(656.464966f,  50f), // C13 -H
                    new SpectrumPeak(657.472791f, 100f), // C13
                    new SpectrumPeak(658.480616f,  50f), // C13 +H
                    new SpectrumPeak(670.480616f,  50f), // C14 -H
                    new SpectrumPeak(671.488442f, 100f), // C14
                    new SpectrumPeak(672.496267f,  50f), // C14 +H
                    new SpectrumPeak(684.496267f,  50f), // C15 -H
                    new SpectrumPeak(685.504092f, 100f), // C15
                    new SpectrumPeak(686.511917f,  50f), // C15 +H
                    new SpectrumPeak(698.511917f,  50f), // C16 -H
                    new SpectrumPeak(699.519742f, 100f), // C16
                    new SpectrumPeak(700.527567f,  50f), // C16 +H
                    new SpectrumPeak(712.527567f,  50f), // C17 -H
                    new SpectrumPeak(713.535392f, 100f), // C17
                    new SpectrumPeak(714.543217f,  50f), // C17 +H
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