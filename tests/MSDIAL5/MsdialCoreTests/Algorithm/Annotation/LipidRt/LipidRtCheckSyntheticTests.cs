using CompMs.Common.Enum;
using CompMs.MsdialCore.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Annotation.LipidRt.Tests
{
    /// <summary>
    /// The synthetic tests of SPEC 13.1, with the numbers of the frozen Python reference (test_lipidrt.py t01-t15),
    /// plus the policies the C# default adds (ClassLevelPolicy Corroborated) and the operating points.
    /// </summary>
    /// <remarks>
    /// The reference ran these with ClassLevelPolicy Consistent, its frozen default; <see cref="LipidRtSyntheticRun.Parameter"/>
    /// sets that explicitly so the expectations carry over unchanged. Every member here is chain level except in the
    /// class-level tests, so the policy matters only there.
    /// </remarks>
    [TestClass]
    public class LipidRtCheckSyntheticTests
    {
        [TestMethod]
        public void T01_ABracketedMemberIsConfirmed() {
            var run = new LipidRtSyntheticRun();
            var sp = run.TgGrid();
            var result = LipidRtSyntheticRun.Run(sp);
            var r = result[LipidRtSyntheticRun.Find(sp, "TG", 52, 1)];
            Assert.AreEqual(LipidRtOutcome.Confirmed, r.Outcome);
            Assert.AreEqual(LipidRtReason.ConfirmedRt, r.Reason);
            Assert.AreEqual("C<C>D<D>", r.Evidence.Directions);
            Assert.AreEqual(2, r.Evidence.Axes);
            Assert.AreEqual(LipidRtFeatureStatus.Free, r.Evidence.Status);
            StringAssert.StartsWith(r.Sentence, "Confirmed: TG 52:1 elutes after ");
        }

        [TestMethod]
        public void T02_ACornerMemberLacksABracket() {
            var run = new LipidRtSyntheticRun();
            var sp = run.TgGrid();
            var result = LipidRtSyntheticRun.Run(sp);
            // 48:1 earlier (D>) and 50:0 later (C>) bracket TG 48:0 across axes
            Assert.AreEqual(LipidRtOutcome.Confirmed, result[LipidRtSyntheticRun.Find(sp, "TG", 48, 0)].Outcome);
            // lowest C, most DB: every neighbour elutes later (upper bounds only), no bracket
            var corner = result[LipidRtSyntheticRun.Find(sp, "TG", 48, 3)];
            Assert.AreEqual(LipidRtOutcome.NotEvaluable, corner.Outcome);
            Assert.AreEqual(LipidRtReason.Support, corner.Reason);
        }

        [TestMethod]
        public void T03_AShiftedMemberIsInconsistent() {
            var run = new LipidRtSyntheticRun();
            var sp = run.TgGrid();
            var id = LipidRtSyntheticRun.Find(sp, "TG", 52, 1);
            sp = sp.Select(s => s.Id == id ? LipidRtSyntheticRun.With(s, rt: s.Rt + 1.5) : s).ToList();
            var result = LipidRtSyntheticRun.Run(sp);
            Assert.AreEqual(LipidRtOutcome.Inconsistent, result[id].Outcome);
            Assert.AreEqual(LipidRtReason.InconsistentOrder, result[id].Reason);
            // the exact antichain removes the one wrong element, not its neighbours
            Assert.AreEqual(LipidRtOutcome.Confirmed, result[LipidRtSyntheticRun.Find(sp, "TG", 52, 2)].Outcome);
        }

        [TestMethod]
        public void T04_AdductDuplicatesCollapseAndEveryMemberIsConfirmed() {
            var run = new LipidRtSyntheticRun();
            var sp = run.TgGrid();
            var s0 = sp.First(s => s.Carbon == 52 && s.DoubleBondEquivalent == 1);
            var na = run.Mk("TG", 52, 1, s0.Rt + 0.01, adduct: "[M+Na]+", h: 3e4);
            sp.Add(na);
            var result = LipidRtSyntheticRun.Run(sp);
            Assert.AreEqual(LipidRtOutcome.Confirmed, result[na.Id].Outcome);
            Assert.AreEqual(LipidRtOutcome.Confirmed, result[s0.Id].Outcome);
            CollectionAssert.AreEqual(result[s0.Id].Evidence.Members.ToList(), result[na.Id].Evidence.Members.ToList());
            Assert.AreEqual(2, result[na.Id].Evidence.AdductCount);
            Assert.IsTrue(result[na.Id].CorroboratedByAdduct, "a feature with two adducts corroborates its Confirmed members");
        }

        [TestMethod]
        public void T05_IsomersAreNotOrderedAgainstEachOther() {
            var run = new LipidRtSyntheticRun();
            var sp = run.TgGrid();
            var s0 = sp.First(s => s.Carbon == 52 && s.DoubleBondEquivalent == 2);
            var isomer = run.Mk("TG", 52, 2, s0.Rt + 0.12, molecular: "TG 16:0_18:1_18:1", h: 5e4);
            sp.Add(isomer);
            var result = LipidRtSyntheticRun.Run(sp);
            Assert.AreNotEqual(result[s0.Id].Evidence.Feature, result[isomer.Id].Evidence.Feature);
            Assert.AreNotEqual(LipidRtOutcome.Inconsistent, result[isomer.Id].Outcome);
            Assert.AreNotEqual(LipidRtOutcome.Inconsistent, result[s0.Id].Outcome);
        }

        [TestMethod]
        public void T06_AnM2IsotopeReadAsAnotherSpeciesIsBlockedByTheArtefactLayer() {
            var run = new LipidRtSyntheticRun();
            var sp = run.TgGrid(skip: new[] { (52, 1) });
            var parent = sp.First(s => s.Carbon == 52 && s.DoubleBondEquivalent == 2);
            var decoy = run.Mk("TG", 52, 1, parent.Rt, mz: parent.Mz + 2.00671, h: parent.Height * 0.25);
            sp.Add(decoy);
            var result = LipidRtSyntheticRun.Run(sp);
            Assert.AreNotEqual(LipidRtOutcome.Confirmed, result[decoy.Id].Outcome);
            CollectionAssert.Contains(result[decoy.Id].Evidence.Artefacts.Select(a => a.Kind).ToList(), "iso2");
            Assert.AreEqual(LipidRtOutcome.Confirmed, result[parent.Id].Outcome);
        }

        [TestMethod]
        public void T07_ANaHIsobarIsBlocked() {
            var run = new LipidRtSyntheticRun();
            var sp = new List<LipidRtCandidate>();
            foreach (var c in new[] { 32, 34, 36, 38, 40 }) {
                foreach (var db in new[] { 0, 1, 2, 3, 4 }) {
                    sp.Add(run.Mk("PC", c, db, 2.0 + 0.30 * c - 0.45 * db, adduct: "[M+H]+"));
                }
            }
            var parent = sp.First(s => s.Carbon == 34 && s.DoubleBondEquivalent == 1);
            // [M+Na]+ of PC 34:1 has the m/z of [M+H]+ of PC 36:4 (2.4 mDa); read as PC 36:4 at the PC 34:1 RT
            sp = sp.Where(s => !(s.Carbon == 36 && s.DoubleBondEquivalent == 4)).ToList();
            var decoy = run.Mk("PC", 36, 4, parent.Rt, adduct: "[M+H]+", mz: parent.Mz + 21.98194, h: parent.Height * 0.2);
            sp.Add(decoy);
            var result = LipidRtSyntheticRun.Run(sp);
            Assert.AreNotEqual(LipidRtOutcome.Confirmed, result[decoy.Id].Outcome);
            CollectionAssert.Contains(result[decoy.Id].Evidence.Artefacts.Select(a => a.Kind).ToList(), "naH");
        }

        [TestMethod]
        public void T08_AHilicLikeRunFailsTheGate() {
            var run = new LipidRtSyntheticRun();
            var sp = new List<LipidRtCandidate>();
            foreach (var (cls, baseRt) in new[] { ("PC", 8.0), ("PE", 6.0), ("TG", 1.0) }) {
                foreach (var c in new[] { 32, 34, 36, 38, 40 }) {
                    foreach (var db in new[] { 0, 1, 2 }) {
                        sp.Add(run.Mk(cls, cls != "TG" ? c : c + 18, db, baseRt + 0.01 * ((c * 7 + db * 3) % 5), adduct: "[M+H]+"));
                    }
                }
            }
            var result = LipidRtSyntheticRun.Run(sp);
            Assert.IsTrue(result.Results.All(r => r.Outcome == LipidRtOutcome.NotEvaluable && r.Reason == LipidRtReason.Gate));
            Assert.IsFalse(result.Gate.Passed);

            var other = LipidRtSyntheticRun.Run(run.TgGrid(), configure: p => p.Chromatography = LipidRtChromatography.Other);
            Assert.IsTrue(other.Results.All(r => r.Outcome == LipidRtOutcome.NotEvaluable && r.Reason == LipidRtReason.Gate));
            Assert.IsTrue(other.Gate.ChromatographyIsOther);
        }

        [TestMethod]
        public void T09_PlausibilityB1HardIsInconsistentOnlyWithTheProfile() {
            var run = new LipidRtSyntheticRun();
            var sp = new List<LipidRtCandidate>();
            foreach (var c in new[] { 34, 36, 38, 40 }) {
                foreach (var db in new[] { 4, 5, 6, 7 }) {
                    sp.Add(run.Mk("PC", c, db, 2.0 + 0.30 * c - 0.45 * db, adduct: "[M+H]+", molecular: $"PC 16:0_{c - 16}:{db}"));
                }
            }
            var id = LipidRtSyntheticRun.Find(sp, "PC", 38, 7); // chain 22:7: no fatty chain with >= 7 DB in mammals
            var on = LipidRtSyntheticRun.Run(sp, profile: LipidRtChainRuleProfile.Mammalian);
            var off = LipidRtSyntheticRun.Run(sp);
            Assert.AreEqual(LipidRtOutcome.Inconsistent, on[id].Outcome);
            Assert.AreEqual(LipidRtReason.InconsistentPlausibility, on[id].Reason);
            Assert.AreNotEqual(LipidRtOutcome.Inconsistent, off[id].Outcome);
        }

        [TestMethod]
        public void T10_ClassLevelPolicy() {
            var run = new LipidRtSyntheticRun();
            var sp = run.TgGrid();
            var id = LipidRtSyntheticRun.Find(sp, "TG", 52, 1);
            sp = sp.Select(s => s.Id == id ? LipidRtSyntheticRun.With(s, level: LipidRtTagLevel.Class, molecular: string.Empty) : s).ToList();

            // D3: the species-level check covers class-level matches
            var consistent = LipidRtSyntheticRun.Run(sp);
            Assert.AreEqual(LipidRtOutcome.Confirmed, consistent[id].Outcome);

            var never = LipidRtSyntheticRun.Run(sp, configure: p => p.ClassLevelPolicy = LipidRtClassLevelPolicy.Never);
            Assert.AreEqual(LipidRtOutcome.NotEvaluable, never[id].Outcome);
            Assert.AreEqual(LipidRtReason.PolicyClass, never[id].Reason);
        }

        [TestMethod]
        public void ClassLevelPolicyCorroboratedNeedsASecondAdductOrTheOtherPolarity() {
            var run = new LipidRtSyntheticRun();
            var sp = run.TgGrid();
            var s0 = sp.First(s => s.Carbon == 52 && s.DoubleBondEquivalent == 1);
            sp = sp.Select(s => s.Id == s0.Id ? LipidRtSyntheticRun.With(s, level: LipidRtTagLevel.Class, molecular: string.Empty) : s).ToList();

            var alone = LipidRtSyntheticRun.Run(sp, configure: p => p.ClassLevelPolicy = LipidRtClassLevelPolicy.Corroborated);
            Assert.AreEqual(LipidRtOutcome.NotEvaluable, alone[s0.Id].Outcome);
            Assert.AreEqual(LipidRtReason.PolicyClass, alone[s0.Id].Reason);
            Assert.AreEqual(LipidRtReason.ConfirmedRt, alone[s0.Id].SingleRunReason, "the RT evidence itself confirms; the policy withholds");

            var na = run.Mk("TG", 52, 1, s0.Rt + 0.01, adduct: "[M+Na]+", h: 3e4, level: LipidRtTagLevel.Class);
            var withAdduct = LipidRtSyntheticRun.Run(sp.Concat(new[] { na }), configure: p => p.ClassLevelPolicy = LipidRtClassLevelPolicy.Corroborated);
            Assert.AreEqual(LipidRtOutcome.Confirmed, withAdduct[s0.Id].Outcome);
            Assert.AreEqual(LipidRtOutcome.Confirmed, withAdduct[na.Id].Outcome);
            Assert.IsTrue(withAdduct[s0.Id].CorroboratedByAdduct);
        }

        [TestMethod]
        public void TheDefaultParameterIsClassLevelCorroborated() {
            var parameter = new LipidRtCheckParameter();
            Assert.AreEqual(LipidRtClassLevelPolicy.Corroborated, parameter.ClassLevelPolicy);
            Assert.AreEqual(LipidRtOperatingPoint.HighPrecision, parameter.OperatingPoint);
            Assert.AreEqual(LipidRtPlausibilityProfileKind.None, parameter.PlausibilityProfile);
            Assert.AreEqual(LipidRtMinorDuplicatePolicy.Allow, parameter.MinorDuplicatePolicy);
            Assert.AreEqual(LipidRtSecondaryOnlyPolicy.Evaluate, parameter.SecondaryOnlyPolicy);
            Assert.AreEqual(LipidRtChromatography.Auto, parameter.Chromatography);
        }

        [TestMethod]
        public void T11_AnOxidisedClassConflictIsNotFlagged() {
            var run = new LipidRtSyntheticRun();
            var sp = new List<LipidRtCandidate>();
            foreach (var c in new[] { 34, 36, 38 }) {
                foreach (var o in new[] { 1, 2, 3 }) {
                    foreach (var db in new[] { 2, 3, 4 }) {
                        sp.Add(run.Mk("OxPC", c, db, 2.0 + 0.30 * c - 0.45 * db - 0.3 * o, o: o, adduct: "[M+H]+"));
                    }
                }
            }
            foreach (var c in new[] { 48, 50, 52, 54, 56 }) {
                foreach (var db in new[] { 0, 1, 2 }) {
                    sp.Add(run.Mk("TG", c, db, 2.0 + 0.15 * c - 0.25 * db));
                }
            }
            var id = LipidRtSyntheticRun.Find(sp, "OxPC", 36, 3, o: 2);
            sp = sp.Select(s => s.Id == id ? LipidRtSyntheticRun.With(s, rt: s.Rt + 2.0) : s).ToList();
            var result = LipidRtSyntheticRun.Run(sp);
            Assert.AreEqual(LipidRtOutcome.NotEvaluable, result[id].Outcome);
            Assert.AreEqual(LipidRtReason.OxNoFlag, result[id].Reason);
        }

        [TestMethod]
        public void T12_ACoelutingHeavierAdjacentCompositionBlocksByTie() {
            var run = new LipidRtSyntheticRun();
            var sp = run.TgGrid();
            var s1 = sp.First(s => s.Carbon == 52 && s.DoubleBondEquivalent == 1);
            sp.Add(run.Mk("TG", 53, 1, s1.Rt + 0.005, h: 1e6)); // adjacent composition (C+1) co-eluting, heavier
            var result = LipidRtSyntheticRun.Run(sp);
            Assert.AreEqual(LipidRtOutcome.NotEvaluable, result[s1.Id].Outcome);
            Assert.AreEqual(LipidRtReason.Tie, result[s1.Id].Reason);
        }

        [TestMethod]
        public void T13_DeterministicAndIndependentOfInputOrder() {
            var run = new LipidRtSyntheticRun();
            var sp = run.TgGrid();
            sp[5] = LipidRtSyntheticRun.With(sp[5], rt: sp[5].Rt + 1.2);
            var reference = LipidRtSyntheticRun.Run(sp);
            var random = new Random(7);
            for (int trial = 0; trial < 5; trial++) {
                var shuffled = sp.OrderBy(_ => random.Next()).ToList();
                var result = LipidRtSyntheticRun.Run(shuffled);
                foreach (var s in sp) {
                    Assert.AreEqual(reference[s.Id].Outcome, result[s.Id].Outcome);
                    Assert.AreEqual(reference[s.Id].Reason, result[s.Id].Reason);
                    Assert.AreEqual(reference[s.Id].Score, result[s.Id].Score);
                    Assert.AreEqual(reference[s.Id].Sentence, result[s.Id].Sentence);
                }
            }
            Assert.IsTrue(sp.Any(s => reference[s.Id].Outcome == LipidRtOutcome.Inconsistent), "the shifted element makes the case non-trivial");
        }

        [TestMethod]
        public void T14_ScaleInvarianceFastLc() {
            var run = new LipidRtSyntheticRun();
            var sp = run.TgGrid();
            sp[7] = LipidRtSyntheticRun.With(sp[7], rt: sp[7].Rt + 1.0);
            var slow = LipidRtSyntheticRun.Run(sp, rtMin: 0, rtMax: 20);
            var fast = LipidRtSyntheticRun.Run(sp.Select(s => LipidRtSyntheticRun.With(s, rt: s.Rt * 0.35)), rtMin: 0, rtMax: 7);
            foreach (var s in sp) {
                Assert.AreEqual(slow[s.Id].Outcome, fast[s.Id].Outcome, s.Name);
                Assert.AreEqual(slow[s.Id].Reason, fast[s.Id].Reason, s.Name);
            }
        }

        [TestMethod]
        public void T15_CrossPolarityPromotionAndCorroboration() {
            var run = new LipidRtSyntheticRun();
            var pos = run.TgGrid();
            var neg = pos.Select(s => run.Mk("TG", s.Carbon, s.DoubleBondEquivalent, s.Rt + 0.01, adduct: "[M+CH3COO]-")).ToList();
            var corner = LipidRtSyntheticRun.Find(pos, "TG", 48, 3); // NotEvaluable in pos (no bracket)
            // in neg, TG 48:3 is bracketed: TG 46:3 elutes earlier (C<), TG 50:3 and 48:2 later
            neg.Add(run.Mk("TG", 46, 3, 2.0 + 0.15 * 46 - 0.25 * 3 + 0.01, adduct: "[M+CH3COO]-"));
            var singlePos = LipidRtSyntheticRun.RunSingle(pos);
            var singleNeg = LipidRtSyntheticRun.RunSingle(neg, polarity: IonMode.Negative);
            Assert.AreEqual(LipidRtReason.Support, singlePos[corner].Reason);

            var paired = LipidRtCorroboration.CrossPolarity(singlePos, singleNeg, LipidRtSyntheticRun.Parameter());
            Assert.AreEqual(LipidRtOutcome.Confirmed, paired.Positive[corner].Outcome);
            Assert.AreEqual(LipidRtReason.ConfirmedPromotedByPolarity, paired.Positive[corner].Reason);
            var mid = LipidRtSyntheticRun.Find(pos, "TG", 52, 1);
            Assert.AreEqual(LipidRtOutcome.Confirmed, paired.Positive[mid].Outcome);
            Assert.IsTrue(paired.Positive[mid].CorroboratedByPolarity.Count > 0);
            Assert.AreEqual(singlePos[mid].SingleRunScore + 1d, paired.Positive[mid].Score, "a corroborated Confirmed gains one evidence unit");

            // the inputs are not changed
            Assert.AreEqual(LipidRtReason.Support, singlePos[corner].Reason);
            Assert.AreEqual(0, singlePos[mid].CorroboratedByPolarity.Count);
        }

        [TestMethod]
        public void StrictRequiresCorroboration() {
            var run = new LipidRtSyntheticRun();
            var sp = run.TgGrid();
            var s0 = sp.First(s => s.Carbon == 52 && s.DoubleBondEquivalent == 1);
            var na = run.Mk("TG", 52, 1, s0.Rt + 0.01, adduct: "[M+Na]+", h: 3e4);
            sp.Add(na);
            var strict = LipidRtSyntheticRun.Run(sp, configure: p => p.OperatingPoint = LipidRtOperatingPoint.Strict);
            Assert.AreEqual(LipidRtOutcome.Confirmed, strict[s0.Id].Outcome, "adduct-corroborated");
            var other = strict[LipidRtSyntheticRun.Find(sp, "TG", 52, 2)];
            Assert.AreEqual(LipidRtOutcome.NotEvaluable, other.Outcome);
            Assert.AreEqual(LipidRtReason.Uncorroborated, other.Reason);
        }

        [TestMethod]
        public void OperatingPointsLoosenTheSupportRule() {
            var run = new LipidRtSyntheticRun();
            var sp = run.TgGrid();
            var corner = LipidRtSyntheticRun.Find(sp, "TG", 48, 3); // upper bounds only on two axes
            Assert.AreEqual(LipidRtReason.Support, LipidRtSyntheticRun.Run(sp)[corner].Reason);
            Assert.AreEqual(LipidRtReason.ConfirmedRt, LipidRtSyntheticRun.Run(sp, configure: p => p.OperatingPoint = LipidRtOperatingPoint.Mid)[corner].Reason);
            Assert.AreEqual(LipidRtReason.ConfirmedRt, LipidRtSyntheticRun.Run(sp, configure: p => p.OperatingPoint = LipidRtOperatingPoint.Balanced)[corner].Reason);
        }

        [TestMethod]
        public void SecondaryOnlyAndMinorDuplicatePoliciesBlockWhenAsked() {
            var run = new LipidRtSyntheticRun();
            var sp = run.TgGrid(adduct: "[M+Na]+");
            var id = LipidRtSyntheticRun.Find(sp, "TG", 52, 1);
            Assert.AreEqual(LipidRtReason.ConfirmedRt, LipidRtSyntheticRun.Run(sp)[id].Reason, "an [M+Na]+ feature is evaluated by default");
            Assert.AreEqual(LipidRtReason.PolicySecondary, LipidRtSyntheticRun.Run(sp, configure: p => p.SecondaryOnlyPolicy = LipidRtSecondaryOnlyPolicy.Block)[id].Reason);

            var grid = run.TgGrid();
            var s0 = grid.First(s => s.Carbon == 52 && s.DoubleBondEquivalent == 2);
            var minor = run.Mk("TG", 52, 2, s0.Rt + 0.12, h: 5e4); // same species and chain names, less intense
            grid.Add(minor);
            Assert.AreNotEqual(LipidRtReason.MinorDuplicate, LipidRtSyntheticRun.Run(grid)[minor.Id].Reason);
            var blocked = LipidRtSyntheticRun.Run(grid, configure: p => p.MinorDuplicatePolicy = LipidRtMinorDuplicatePolicy.Block);
            Assert.AreNotEqual(LipidRtOutcome.Confirmed, blocked[minor.Id].Outcome);
            Assert.AreNotEqual(LipidRtReason.MinorDuplicate, blocked[s0.Id].Reason, "the dominant copy is not a minor duplicate");
        }

        [TestMethod]
        public void IneligibleCandidatesAreNotEvaluatedAndCarryNoScore() {
            var run = new LipidRtSyntheticRun();
            var sp = run.TgGrid();
            var ineligible = run.Mk("TG", 52, 1, 9.6, eligible: false);
            sp.Add(ineligible);
            var result = LipidRtSyntheticRun.Run(sp);
            Assert.AreEqual(LipidRtOutcome.NotEvaluable, result[ineligible.Id].Outcome);
            Assert.AreEqual(LipidRtReason.Ineligible, result[ineligible.Id].Reason);
            Assert.IsNull(result[ineligible.Id].Score);
            Assert.AreEqual(sp.Count, result.Results.Count);
        }

        [TestMethod]
        public void TheSentenceNamesTheBracketingNeighbours() {
            var run = new LipidRtSyntheticRun();
            var sp = run.TgGrid();
            var sentence = LipidRtSyntheticRun.Run(sp)[LipidRtSyntheticRun.Find(sp, "TG", 52, 1)].Sentence;
            StringAssert.StartsWith(sentence, "Confirmed: TG 52:1 elutes after TG 50:1 (+0.30 min) and TG 52:2 (+0.25), and before ");
            StringAssert.Contains(sentence, "TG 54:1 (-0.30");
            StringAssert.Contains(sentence, "TG 52:0 (-0.25");
            StringAssert.EndsWith(sentence, "; residual +0.00 min (+0.00 DB steps).");
        }

        [TestMethod]
        public void TheReviewListHoldsOrderConflictsAndArtefacts() {
            var run = new LipidRtSyntheticRun();
            var sp = run.TgGrid(skip: new[] { (52, 1) });
            var shifted = LipidRtSyntheticRun.Find(sp, "TG", 50, 1);
            sp = sp.Select(s => s.Id == shifted ? LipidRtSyntheticRun.With(s, rt: s.Rt + 1.5) : s).ToList();
            var parent = sp.First(s => s.Carbon == 52 && s.DoubleBondEquivalent == 2);
            var decoy = run.Mk("TG", 52, 1, parent.Rt, mz: parent.Mz + 2.00671, h: parent.Height * 0.25);
            sp.Add(decoy);
            var result = LipidRtSyntheticRun.Run(sp);
            Assert.AreEqual(LipidRtReason.InconsistentOrder, result[shifted].Reason);

            var review = LipidRtReviewList.Build(result);
            Assert.AreEqual(LipidRtReviewGroup.InconsistentOrderStrong, review.Single(i => i.Result.Id == shifted).Group);
            Assert.AreEqual(LipidRtReviewGroup.Artefact, review.Single(i => i.Result.Id == decoy.Id).Group,
                "a NotEvaluable with an artefact explanation is a review item whatever rule decided it");
            Assert.IsFalse(review.Any(i => i.Result.Outcome == LipidRtOutcome.Confirmed && !i.Result.PolarityConflict));

            var writer = new System.IO.StringWriter();
            LipidRtReviewList.Write(writer, review);
            var lines = writer.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            Assert.AreEqual(review.Count + 1, lines.Length);
            Assert.AreEqual(string.Join("\t", LipidRtReviewList.Columns), lines[0]);
            Assert.IsTrue(lines.Skip(1).All(l => l.Split('\t').Length == LipidRtReviewList.Columns.Length));
        }

        [TestMethod]
        public void TheVerdictCarriesTheRuleVersionAndTheParameterHash() {
            var run = new LipidRtSyntheticRun();
            var sp = run.TgGrid();
            var parameter = LipidRtSyntheticRun.Parameter();
            var result = new LipidRtCheck(parameter).Evaluate(LipidRtSyntheticRun.Input(sp));
            var verdict = result[LipidRtSyntheticRun.Find(sp, "TG", 52, 1)].ToVerdict(parameter.ComputeHash());
            Assert.AreEqual(LipidRtOutcome.Confirmed, verdict.Outcome);
            Assert.AreEqual("1.0", verdict.RuleVersion);
            Assert.AreEqual(16, verdict.ParameterHash.Length);
            StringAssert.StartsWith(verdict.Evidence, "Confirmed: ");
        }
    }
}
