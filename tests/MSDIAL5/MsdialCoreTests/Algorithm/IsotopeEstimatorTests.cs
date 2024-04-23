using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.Common.Parser;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Tests;

[TestClass()]
public class IsotopeEstimatorTests
{
    // basic condition
    [TestMethod()]
    public void EstimateIsotopesTest1() {
        var iupac = IupacResourceParser.GetIUPACDatabase();
        var param = new ParameterBase();
        var actuals = new List<AlignmentSpotProperty> {
            new AlignmentSpotProperty {
                AlignmentID = 1, MassCenter = 500, HeightAverage = 1000000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 2, MassCenter = 500 + MassDiffDictionary.C13_C12, HeightAverage = 999999,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 3, MassCenter = 500 + 2, HeightAverage = 1000000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 4, MassCenter = 500 + MassDiffDictionary.C13_C12 * 2, HeightAverage = 999998,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 5, MassCenter = 500 + 3, HeightAverage = 1000000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 6, MassCenter = 500 + MassDiffDictionary.C13_C12 * 3, HeightAverage = 999997,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
        };
        var expects = new List<AlignmentSpotProperty> {
            new AlignmentSpotProperty {
                AlignmentID = 1, MassCenter = 500, HeightAverage = 1000000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 0, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 2, MassCenter = 500 + MassDiffDictionary.C13_C12, HeightAverage = 999999,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 1, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 3, MassCenter = 500 + 2, HeightAverage = 1000000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 4, MassCenter = 500 + MassDiffDictionary.C13_C12 * 2, HeightAverage = 999998,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 2, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 5, MassCenter = 500 + 3, HeightAverage = 1000000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 6, MassCenter = 500 + MassDiffDictionary.C13_C12 * 3, HeightAverage = 999997,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 3, Charge = 1 },
            },
        };

        IsotopeEstimator.EstimateIsotopes(actuals, param, iupac);

        foreach ((var actual, var expect) in actuals.Zip(expects)) {
            AreEqual(expect, actual);
        }
    }

    // intensity not decrease
    [TestMethod()]
    public void EstimateIsotopesTest2() {
        var iupac = IupacResourceParser.GetIUPACDatabase();
        var param = new ParameterBase();
        var actuals = new List<AlignmentSpotProperty> {
            new AlignmentSpotProperty {
                AlignmentID = 1, MassCenter = 500, HeightAverage = 1000000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 2, MassCenter = 500 + MassDiffDictionary.C13_C12, HeightAverage = 900000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 3, MassCenter = 500 + MassDiffDictionary.C13_C12 * 2, HeightAverage = 800000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 4, MassCenter = 500 + MassDiffDictionary.C13_C12 * 3, HeightAverage = 800000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 5, MassCenter = 500 + MassDiffDictionary.C13_C12 * 3, HeightAverage = 900000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 6, MassCenter = 500 + MassDiffDictionary.C13_C12 * 4, HeightAverage = 700000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
        };
        var expects = new List<AlignmentSpotProperty> {
            new AlignmentSpotProperty {
                AlignmentID = 1, MassCenter = 500, HeightAverage = 1000000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 0, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 2, MassCenter = 500 + MassDiffDictionary.C13_C12, HeightAverage = 900000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 1, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 3, MassCenter = 500 + MassDiffDictionary.C13_C12 * 2, HeightAverage = 800000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 2, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 4, MassCenter = 500 + MassDiffDictionary.C13_C12 * 3, HeightAverage = 800000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 5, MassCenter = 500 + MassDiffDictionary.C13_C12 * 3, HeightAverage = 900000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 6, MassCenter = 500 + MassDiffDictionary.C13_C12 * 4, HeightAverage = 700000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
        };

        IsotopeEstimator.EstimateIsotopes(actuals, param, iupac);

        foreach ((var actual, var expect) in actuals.Zip(expects)) {
            AreEqual(expect, actual);
        }
    }

    // intensity not decrease, but IsBrClConsideredForIsotopes = true
    [TestMethod()]
    public void EstimateIsotopesTest3() {
        var iupac = IupacResourceParser.GetIUPACDatabase();
        var param = new ParameterBase { IsBrClConsideredForIsotopes = true };
        var actuals = new List<AlignmentSpotProperty> {
            new AlignmentSpotProperty {
                AlignmentID = 1, MassCenter = 500, HeightAverage = 1000000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 2, MassCenter = 500 + MassDiffDictionary.C13_C12, HeightAverage = 900000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 3, MassCenter = 500 + MassDiffDictionary.C13_C12 * 2, HeightAverage = 800000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 4, MassCenter = 500 + MassDiffDictionary.C13_C12 * 3, HeightAverage = 800000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 5, MassCenter = 500 + MassDiffDictionary.C13_C12 * 3, HeightAverage = 900000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 6, MassCenter = 500 + MassDiffDictionary.C13_C12 * 4, HeightAverage = 700000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
        };
        var expects = new List<AlignmentSpotProperty> {
            new AlignmentSpotProperty {
                AlignmentID = 1, MassCenter = 500, HeightAverage = 1000000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 0, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 2, MassCenter = 500 + MassDiffDictionary.C13_C12, HeightAverage = 900000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 1, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 3, MassCenter = 500 + MassDiffDictionary.C13_C12 * 2, HeightAverage = 800000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 2, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 4, MassCenter = 500 + MassDiffDictionary.C13_C12 * 3, HeightAverage = 800000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 3, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 5, MassCenter = 500 + MassDiffDictionary.C13_C12 * 3, HeightAverage = 900000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 6, MassCenter = 500 + MassDiffDictionary.C13_C12 * 4, HeightAverage = 700000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 4, Charge = 1 },
            },
        };

        IsotopeEstimator.EstimateIsotopes(actuals, param, iupac);

        foreach ((var actual, var expect) in actuals.Zip(expects)) {
            AreEqual(expect, actual);
        }
    }

    // 2 candidates for same weight number
    [TestMethod()]
    public void EstimateIsotopesTest4() {
        var iupac = IupacResourceParser.GetIUPACDatabase();
        var param = new ParameterBase();
        var actuals = new List<AlignmentSpotProperty> {
            new AlignmentSpotProperty {
                AlignmentID = 1, MassCenter = 500, HeightAverage = 1000000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 2, MassCenter = 500 + MassDiffDictionary.C13_C12, HeightAverage = 900000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 3, MassCenter = 500 + MassDiffDictionary.C13_C12 * 2, HeightAverage = 800000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 4, MassCenter = 500 + MassDiffDictionary.C13_C12 * 3 + param.CentroidMs1Tolerance * 0.001, HeightAverage = 700000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 5, MassCenter = 500 + MassDiffDictionary.C13_C12 * 3, HeightAverage = 700000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 6, MassCenter = 500 + MassDiffDictionary.C13_C12 * 4, HeightAverage = 600000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
        };
        var expects = new List<AlignmentSpotProperty> {
            new AlignmentSpotProperty {
                AlignmentID = 1, MassCenter = 500, HeightAverage = 1000000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 0, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 2, MassCenter = 500 + MassDiffDictionary.C13_C12, HeightAverage = 900000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 1, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 3, MassCenter = 500 + MassDiffDictionary.C13_C12 * 2, HeightAverage = 800000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 2, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 4, MassCenter = 500 + MassDiffDictionary.C13_C12 * 3 + param.CentroidMs1Tolerance * 0.001, HeightAverage = 700000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 5, MassCenter = 500 + MassDiffDictionary.C13_C12 * 3, HeightAverage = 700000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 3, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 6, MassCenter = 500 + MassDiffDictionary.C13_C12 * 4, HeightAverage = 600000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 4, Charge = 1 },
            },
        };

        IsotopeEstimator.EstimateIsotopes(actuals, param, iupac);

        foreach ((var actual, var expect) in actuals.Zip(expects)) {
            AreEqual(expect, actual);
        }
    }

    // tolerance check (shift +)
    [TestMethod()]
    public void EstimateIsotopesTest5() {
        var iupac = IupacResourceParser.GetIUPACDatabase();
        var param = new ParameterBase();
        var ppm = MolecularFormulaUtility.PpmCalculator(200.0, 200.0 + param.CentroidMs1Tolerance); //based on m/z 400
        var tolerance = (float)MolecularFormulaUtility.ConvertPpmToMassAccuracy(500, ppm);
        var actuals = new List<AlignmentSpotProperty> {
            new AlignmentSpotProperty {
                AlignmentID = 1, MassCenter = 500, HeightAverage = 1000000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 2, MassCenter = 500 + MassDiffDictionary.C13_C12 + tolerance * 0.99, HeightAverage = 900000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 3, MassCenter = 500 + MassDiffDictionary.C13_C12 * 2 + tolerance * 0.99 * 2, HeightAverage = 800000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 4, MassCenter = 500 + MassDiffDictionary.C13_C12 * 3 + tolerance * 0.99 * 3, HeightAverage = 700000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 5, MassCenter = 500 + MassDiffDictionary.C13_C12 * 4 + tolerance * 0.99 * 2, HeightAverage = 600000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 6, MassCenter = 500 + MassDiffDictionary.C13_C12 * 5 + tolerance * 0.979, HeightAverage = 500000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
        };
        var expects = new List<AlignmentSpotProperty> {
            new AlignmentSpotProperty {
                AlignmentID = 1, MassCenter = 500, HeightAverage = 1000000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 0, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 2, MassCenter = 500 + MassDiffDictionary.C13_C12 + tolerance * 0.99, HeightAverage = 900000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 1, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 3, MassCenter = 500 + MassDiffDictionary.C13_C12 * 2 + tolerance * 0.99 * 2, HeightAverage = 800000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 2, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 4, MassCenter = 500 + MassDiffDictionary.C13_C12 * 3 + tolerance * 0.99 * 3, HeightAverage = 700000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 3, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 5, MassCenter = 500 + MassDiffDictionary.C13_C12 * 4 + tolerance * 0.99 * 2, HeightAverage = 600000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 4, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 6, MassCenter = 500 + MassDiffDictionary.C13_C12 * 5 + tolerance * 0.979, HeightAverage = 500000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
        };

        IsotopeEstimator.EstimateIsotopes(actuals, param, iupac);

        foreach ((var actual, var expect) in actuals.Zip(expects)) {
            AreEqual(expect, actual);
        }
    }

    // tolerance check (shift -)
    [TestMethod()]
    public void EstimateIsotopesTest6() {
        var iupac = IupacResourceParser.GetIUPACDatabase();
        var param = new ParameterBase();
        var ppm = MolecularFormulaUtility.PpmCalculator(200.0, 200.0 + param.CentroidMs1Tolerance); //based on m/z 400
        var tolerance = (float)MolecularFormulaUtility.ConvertPpmToMassAccuracy(500, ppm);
        var actuals = new List<AlignmentSpotProperty> {
            new AlignmentSpotProperty {
                AlignmentID = 1, MassCenter = 500, HeightAverage = 1000000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 2, MassCenter = 500 + MassDiffDictionary.C13_C12 - tolerance * 0.99, HeightAverage = 900000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 3, MassCenter = 500 + MassDiffDictionary.C13_C12 * 2 - tolerance * 0.99 * 2, HeightAverage = 800000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 4, MassCenter = 500 + MassDiffDictionary.C13_C12 * 3 - tolerance * 0.99 * 3, HeightAverage = 700000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 5, MassCenter = 500 + MassDiffDictionary.C13_C12 * 4 - tolerance * 0.99 * 2, HeightAverage = 600000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 6, MassCenter = 500 + MassDiffDictionary.C13_C12 * 5 - tolerance * 0.979, HeightAverage = 500000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
        };
        var expects = new List<AlignmentSpotProperty> {
            new AlignmentSpotProperty {
                AlignmentID = 1, MassCenter = 500, HeightAverage = 1000000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 0, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 2, MassCenter = 500 + MassDiffDictionary.C13_C12 - tolerance * 0.99, HeightAverage = 900000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 1, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 3, MassCenter = 500 + MassDiffDictionary.C13_C12 * 2 - tolerance * 0.99 * 2, HeightAverage = 800000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 2, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 4, MassCenter = 500 + MassDiffDictionary.C13_C12 * 3 - tolerance * 0.99 * 3, HeightAverage = 700000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 3, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 5, MassCenter = 500 + MassDiffDictionary.C13_C12 * 4 - tolerance * 0.99 * 2, HeightAverage = 600000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 4, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 6, MassCenter = 500 + MassDiffDictionary.C13_C12 * 5 - tolerance * 0.979, HeightAverage = 500000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
        };

        IsotopeEstimator.EstimateIsotopes(actuals, param, iupac);

        foreach ((var actual, var expect) in actuals.Zip(expects)) {
            AreEqual(expect, actual);
        }
    }

    // mass >= 800
    [TestMethod()]
    public void EstimateIsotopesTest7() {
        var iupac = IupacResourceParser.GetIUPACDatabase();
        var param = new ParameterBase();
        var actuals = new List<AlignmentSpotProperty> {
            new AlignmentSpotProperty {
                AlignmentID = 1, MassCenter = 812, HeightAverage = 1000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 2, MassCenter = 812 + MassDiffDictionary.C13_C12, HeightAverage = 5000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 3, MassCenter = 812 + MassDiffDictionary.C13_C12 * 2, HeightAverage = 25000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 4, MassCenter = 812 + MassDiffDictionary.C13_C12 * 3, HeightAverage = 125000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 5, MassCenter = 812 + MassDiffDictionary.C13_C12 * 4, HeightAverage = 625000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 6, MassCenter = 812 + MassDiffDictionary.C13_C12 * 5, HeightAverage = 3125000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
        };
        var expects = new List<AlignmentSpotProperty> {
            new AlignmentSpotProperty {
                AlignmentID = 1, MassCenter = 812, HeightAverage = 1000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 0, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 2, MassCenter = 812 + MassDiffDictionary.C13_C12, HeightAverage = 5000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 1, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 3, MassCenter = 812 + MassDiffDictionary.C13_C12 * 2, HeightAverage = 25000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 2, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 4, MassCenter = 812 + MassDiffDictionary.C13_C12 * 3, HeightAverage = 125000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 3, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 5, MassCenter = 812 + MassDiffDictionary.C13_C12 * 4, HeightAverage = 625000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 4, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 6, MassCenter = 812 + MassDiffDictionary.C13_C12 * 5, HeightAverage = 3125000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 5, Charge = 1 },
            },
        };

        IsotopeEstimator.EstimateIsotopes(actuals, param, iupac);

        foreach ((var actual, var expect) in actuals.Zip(expects)) {
            AreEqual(expect, actual);
        }
    }

    // missing some isotopes
    [TestMethod()]
    public void EstimateIsotopesTest8() {
        var iupac = IupacResourceParser.GetIUPACDatabase();
        var param = new ParameterBase();
        var actuals = new List<AlignmentSpotProperty> {
            new AlignmentSpotProperty {
                AlignmentID = 1, MassCenter = 500, HeightAverage = 1000000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 2, MassCenter = 500 + MassDiffDictionary.C13_C12, HeightAverage = 900000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 3, MassCenter = 500 + MassDiffDictionary.C13_C12 * 3, HeightAverage = 800000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 4, MassCenter = 500 + MassDiffDictionary.C13_C12 * 5, HeightAverage = 700000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 5, MassCenter = 500 + MassDiffDictionary.C13_C12 * 7, HeightAverage = 600000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 6, MassCenter = 500 + MassDiffDictionary.C13_C12 * 10, HeightAverage = 500000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
        };
        var expects = new List<AlignmentSpotProperty> {
            new AlignmentSpotProperty {
                AlignmentID = 1, MassCenter = 500, HeightAverage = 1000000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 0, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 2, MassCenter = 500 + MassDiffDictionary.C13_C12, HeightAverage = 900000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 1, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 3, MassCenter = 500 + MassDiffDictionary.C13_C12 * 3, HeightAverage = 800000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 4, MassCenter = 500 + MassDiffDictionary.C13_C12 * 5, HeightAverage = 700000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 5, MassCenter = 500 + MassDiffDictionary.C13_C12 * 7, HeightAverage = 600000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 6, MassCenter = 500 + MassDiffDictionary.C13_C12 * 10, HeightAverage = 500000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
        };

        IsotopeEstimator.EstimateIsotopes(actuals, param, iupac);

        foreach ((var actual, var expect) in actuals.Zip(expects)) {
            AreEqual(expect, actual);
        }
    }

    // charge
    [TestMethod()]
    public void EstimateIsotopesTest9() {
        var iupac = IupacResourceParser.GetIUPACDatabase();
        var param = new ParameterBase();
        var actuals = new List<AlignmentSpotProperty> {
            new AlignmentSpotProperty {
                AlignmentID = 1, MassCenter = 500, HeightAverage = 1000000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 2 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 2, MassCenter = 500 + MassDiffDictionary.C13_C12 / 2, HeightAverage = 900000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 3, MassCenter = 500 + MassDiffDictionary.C13_C12 / 2 * 2, HeightAverage = 800000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 4, MassCenter = 500 + MassDiffDictionary.C13_C12 / 2 * 3, HeightAverage = 700000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 5, MassCenter = 500 + MassDiffDictionary.C13_C12 / 2 * 4, HeightAverage = 600000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 6, MassCenter = 500 + MassDiffDictionary.C13_C12 / 2 * 4 + MassDiffDictionary.C13_C12, HeightAverage = 500000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
        };
        var expects = new List<AlignmentSpotProperty> {
            new AlignmentSpotProperty {
                AlignmentID = 1, MassCenter = 500, HeightAverage = 1000000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 0, Charge = 2 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 2, MassCenter = 500 + MassDiffDictionary.C13_C12 / 2, HeightAverage = 900000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 1, Charge = 2 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 3, MassCenter = 500 + MassDiffDictionary.C13_C12 / 2 * 2, HeightAverage = 800000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 2, Charge = 2 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 4, MassCenter = 500 + MassDiffDictionary.C13_C12 / 2 * 3, HeightAverage = 700000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 3, Charge = 2 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 5, MassCenter = 500 + MassDiffDictionary.C13_C12 / 2 * 4, HeightAverage = 600000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 4, Charge = 2 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 6, MassCenter = 500 + MassDiffDictionary.C13_C12 / 2 * 4 + MassDiffDictionary.C13_C12, HeightAverage = 500000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
        };

        IsotopeEstimator.EstimateIsotopes(actuals, param, iupac);

        foreach ((var actual, var expect) in actuals.Zip(expects)) {
            AreEqual(expect, actual);
        }
    }

    // trace isotopes
    [TestMethod()]
    public void EstimateIsotopesTest10() {
        var iupac = IupacResourceParser.GetIUPACDatabase();
        var param = new ParameterBase();
        var actuals = new List<AlignmentSpotProperty> {
            new AlignmentSpotProperty {
                AlignmentID = 1, MassCenter = 500, HeightAverage = 1000000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 2, MassCenter = 500 + MassDiffDictionary.C13_C12, HeightAverage = 900000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 3, MassCenter = 500 + MassDiffDictionary.C13_C12 * 2, HeightAverage = 800000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 4, MassCenter = 500 + MassDiffDictionary.C13_C12 * 3, HeightAverage = 700000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 5, MassCenter = 500 + MassDiffDictionary.C13_C12 * 4, HeightAverage = 600000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 6, MassCenter = 500 + MassDiffDictionary.C13_C12 * 5, HeightAverage = 500000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 7, MassCenter = 500 + MassDiffDictionary.C13_C12 * 6, HeightAverage = 490000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 8, MassCenter = 500 + MassDiffDictionary.C13_C12 * 7, HeightAverage = 480000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 9, MassCenter = 500 + MassDiffDictionary.C13_C12 * 8, HeightAverage = 470000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 10, MassCenter = 500 + MassDiffDictionary.C13_C12 * 9, HeightAverage = 460000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 11, MassCenter = 500 + MassDiffDictionary.C13_C12 * 10, HeightAverage = 450000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 12, MassCenter = 500 + MassDiffDictionary.C13_C12 * 11, HeightAverage = 440000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 13, MassCenter = 500 + MassDiffDictionary.C13_C12 * 12, HeightAverage = 430000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 14, MassCenter = 500 + MassDiffDictionary.C13_C12 * 13, HeightAverage = 420000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 15, MassCenter = 500 + MassDiffDictionary.C13_C12 * 14, HeightAverage = 410000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 16, MassCenter = 500 + MassDiffDictionary.C13_C12 * 15, HeightAverage = 400000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            // new AlignmentSpotProperty {
            //     AlignmentID = 17, MassCenter = 500 + MassDiffDictionary.C13_C12 * 16, HeightAverage = 390000,
            //     PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            // },
        };
        var expects = new List<AlignmentSpotProperty> {
            new AlignmentSpotProperty {
                AlignmentID = 1, MassCenter = 500, HeightAverage = 1000000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 0, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 2, MassCenter = 500 + MassDiffDictionary.C13_C12, HeightAverage = 900000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 1, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 3, MassCenter = 500 + MassDiffDictionary.C13_C12 * 2, HeightAverage = 800000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 2, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 4, MassCenter = 500 + MassDiffDictionary.C13_C12 * 3, HeightAverage = 700000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 3, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 5, MassCenter = 500 + MassDiffDictionary.C13_C12 * 4, HeightAverage = 600000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 4, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 6, MassCenter = 500 + MassDiffDictionary.C13_C12 * 5, HeightAverage = 500000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 5, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 7, MassCenter = 500 + MassDiffDictionary.C13_C12 * 6, HeightAverage = 490000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 6, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 8, MassCenter = 500 + MassDiffDictionary.C13_C12 * 7, HeightAverage = 480000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 7, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 9, MassCenter = 500 + MassDiffDictionary.C13_C12 * 8, HeightAverage = 470000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 8, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 10, MassCenter = 500 + MassDiffDictionary.C13_C12 * 9, HeightAverage = 460000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 9, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 11, MassCenter = 500 + MassDiffDictionary.C13_C12 * 10, HeightAverage = 450000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 10, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 12, MassCenter = 500 + MassDiffDictionary.C13_C12 * 11, HeightAverage = 440000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 11, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 13, MassCenter = 500 + MassDiffDictionary.C13_C12 * 12, HeightAverage = 430000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 12, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 14, MassCenter = 500 + MassDiffDictionary.C13_C12 * 13, HeightAverage = 420000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 13, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 15, MassCenter = 500 + MassDiffDictionary.C13_C12 * 14, HeightAverage = 410000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 14, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 16, MassCenter = 500 + MassDiffDictionary.C13_C12 * 15, HeightAverage = 400000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 15, Charge = 1 },
            },
            // new AlignmentSpotProperty {
            //     AlignmentID = 17, MassCenter = 500 + MassDiffDictionary.C13_C12 * 16, HeightAverage = 390000,
            //     PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 16, Charge = 1 },
            // },
        };

        IsotopeEstimator.EstimateIsotopes(actuals, param, iupac);

        foreach ((var actual, var expect) in actuals.Zip(expects)) {
            AreEqual(expect, actual);
        }
    }

    // mass >= 800 and IsBrClConsideredForIsotopes
    [TestMethod()]
    public void EstimateIsotopesTest11() {
        var iupac = IupacResourceParser.GetIUPACDatabase();
        var param = new ParameterBase() { IsBrClConsideredForIsotopes = true };
        var actuals = new List<AlignmentSpotProperty> {
            new AlignmentSpotProperty {
                AlignmentID = 1, MassCenter = 812, HeightAverage = 1000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 2, MassCenter = 812 + MassDiffDictionary.C13_C12, HeightAverage = 6000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 3, MassCenter = 812 + MassDiffDictionary.C13_C12 * 2, HeightAverage = 25000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 4, MassCenter = 812 + MassDiffDictionary.C13_C12 * 3, HeightAverage = 125000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 5, MassCenter = 812 + MassDiffDictionary.C13_C12 * 4, HeightAverage = 625000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 6, MassCenter = 812 + MassDiffDictionary.C13_C12 * 5, HeightAverage = 3125000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
        };
        var expects = new List<AlignmentSpotProperty> {
            new AlignmentSpotProperty {
                AlignmentID = 1, MassCenter = 812, HeightAverage = 1000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 0, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 2, MassCenter = 812 + MassDiffDictionary.C13_C12, HeightAverage = 6000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 3, MassCenter = 812 + MassDiffDictionary.C13_C12 * 2, HeightAverage = 25000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 4, MassCenter = 812 + MassDiffDictionary.C13_C12 * 3, HeightAverage = 125000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 5, MassCenter = 812 + MassDiffDictionary.C13_C12 * 4, HeightAverage = 625000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 6, MassCenter = 812 + MassDiffDictionary.C13_C12 * 5, HeightAverage = 3125000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
        };

        IsotopeEstimator.EstimateIsotopes(actuals, param, iupac);

        foreach ((var actual, var expect) in actuals.Zip(expects)) {
            AreEqual(expect, actual);
        }
    }

    // missing some isotopes and IsBrClConsideredForIsotopes
    [TestMethod()]
    public void EstimateIsotopesTest12() {
        var iupac = IupacResourceParser.GetIUPACDatabase();
        var param = new ParameterBase() { IsBrClConsideredForIsotopes = true };
        var actuals = new List<AlignmentSpotProperty> {
            new AlignmentSpotProperty {
                AlignmentID = 1, MassCenter = 500, HeightAverage = 1000000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 2, MassCenter = 500 + MassDiffDictionary.C13_C12, HeightAverage = 900000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 3, MassCenter = 500 + MassDiffDictionary.C13_C12 * 3, HeightAverage = 800000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 4, MassCenter = 500 + MassDiffDictionary.C13_C12 * 5, HeightAverage = 700000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 5, MassCenter = 500 + MassDiffDictionary.C13_C12 * 7, HeightAverage = 600000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 6, MassCenter = 500 + MassDiffDictionary.C13_C12 * 10, HeightAverage = 500000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
        };
        var expects = new List<AlignmentSpotProperty> {
            new AlignmentSpotProperty {
                AlignmentID = 1, MassCenter = 500, HeightAverage = 1000000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 0, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 2, MassCenter = 500 + MassDiffDictionary.C13_C12, HeightAverage = 900000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 1, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 3, MassCenter = 500 + MassDiffDictionary.C13_C12 * 3, HeightAverage = 800000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 3, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 4, MassCenter = 500 + MassDiffDictionary.C13_C12 * 5, HeightAverage = 700000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 5, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 5, MassCenter = 500 + MassDiffDictionary.C13_C12 * 7, HeightAverage = 600000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 7, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 6, MassCenter = 500 + MassDiffDictionary.C13_C12 * 10, HeightAverage = 500000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
        };

        IsotopeEstimator.EstimateIsotopes(actuals, param, iupac);

        foreach ((var actual, var expect) in actuals.Zip(expects)) {
            AreEqual(expect, actual);
        }
    }

    // mass >= 800 and m/z < 800
    [TestMethod()]
    public void EstimateIsotopesTest13() {
        var iupac = IupacResourceParser.GetIUPACDatabase();
        var param = new ParameterBase();
        var actuals = new List<AlignmentSpotProperty> {
            new AlignmentSpotProperty {
                AlignmentID = 1, MassCenter = 406, HeightAverage = 1000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 2 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 2, MassCenter = 406 + MassDiffDictionary.C13_C12 / 2, HeightAverage = 5000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 3, MassCenter = 406 + MassDiffDictionary.C13_C12 / 2 * 2, HeightAverage = 25000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 4, MassCenter = 406 + MassDiffDictionary.C13_C12 / 2 * 3, HeightAverage = 125000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 5, MassCenter = 406 + MassDiffDictionary.C13_C12 / 2 * 4, HeightAverage = 625000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 6, MassCenter = 406 + MassDiffDictionary.C13_C12 / 2 * 5, HeightAverage = 3125000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
        };
        var expects = new List<AlignmentSpotProperty> {
            new AlignmentSpotProperty {
                AlignmentID = 1, MassCenter = 406, HeightAverage = 1000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 0, Charge = 2 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 2, MassCenter = 406 + MassDiffDictionary.C13_C12 / 2, HeightAverage = 5000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 1, Charge = 2 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 3, MassCenter = 406 + MassDiffDictionary.C13_C12 / 2 * 2, HeightAverage = 25000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 2, Charge = 2 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 4, MassCenter = 406 + MassDiffDictionary.C13_C12 / 2 * 3, HeightAverage = 125000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 3, Charge = 2 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 5, MassCenter = 406 + MassDiffDictionary.C13_C12 / 2 * 4, HeightAverage = 625000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 4, Charge = 2 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 6, MassCenter = 406 + MassDiffDictionary.C13_C12 / 2 * 5, HeightAverage = 3125000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 5, Charge = 2 },
            },
        };

        IsotopeEstimator.EstimateIsotopes(actuals, param, iupac);

        foreach ((var actual, var expect) in actuals.Zip(expects)) {
            AreEqual(expect, actual);
        }
    }

    // tolerance test
    [TestMethod()]
    public void EstimateIsotopesTest14() {
        var iupac = IupacResourceParser.GetIUPACDatabase();
        var param = new ParameterBase() { CentroidMs1Tolerance = 0.05f };
        var ppm = MolecularFormulaUtility.PpmCalculator(200.0, 200.0 + param.CentroidMs1Tolerance); //based on m/z 400
        var tolerance = (float)MolecularFormulaUtility.ConvertPpmToMassAccuracy(500, ppm);
        var actuals = new List<AlignmentSpotProperty> {
            new AlignmentSpotProperty {
                AlignmentID = 1, MassCenter = 500, HeightAverage = 1000000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 2, MassCenter = 500 + MassDiffDictionary.C13_C12 + tolerance * 0.99, HeightAverage = 900000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 3, MassCenter = 500 + MassDiffDictionary.C13_C12 * 2 + tolerance * 0.99 * 2, HeightAverage = 800000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 4, MassCenter = 500 + MassDiffDictionary.C13_C12 * 3 + tolerance * 0.99 * 3, HeightAverage = 700000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 5, MassCenter = 500 + MassDiffDictionary.C13_C12 * 4 + tolerance * 0.99 * 2, HeightAverage = 600000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 6, MassCenter = 500 + MassDiffDictionary.C13_C12 * 5 + tolerance * 0.979, HeightAverage = 500000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
        };
        var expects = new List<AlignmentSpotProperty> {
            new AlignmentSpotProperty {
                AlignmentID = 1, MassCenter = 500, HeightAverage = 1000000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 0, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 2, MassCenter = 500 + MassDiffDictionary.C13_C12 + tolerance * 0.99, HeightAverage = 900000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 1, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 3, MassCenter = 500 + MassDiffDictionary.C13_C12 * 2 + tolerance * 0.99 * 2, HeightAverage = 800000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 2, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 4, MassCenter = 500 + MassDiffDictionary.C13_C12 * 3 + tolerance * 0.99 * 3, HeightAverage = 700000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 3, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 5, MassCenter = 500 + MassDiffDictionary.C13_C12 * 4 + tolerance * 0.99 * 2, HeightAverage = 600000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 1, IsotopeWeightNumber = 4, Charge = 1 },
            },
            new AlignmentSpotProperty {
                AlignmentID = 6, MassCenter = 500 + MassDiffDictionary.C13_C12 * 5 + tolerance * 0.979, HeightAverage = 500000,
                PeakCharacter = new IonFeatureCharacter { IsotopeParentPeakID = 0, IsotopeWeightNumber = 0, Charge = 0 },
            },
        };

        IsotopeEstimator.EstimateIsotopes(actuals, param, iupac);

        foreach ((var actual, var expect) in actuals.Zip(expects)) {
            AreEqual(expect, actual);
        }
    }

    void AreEqual(AlignmentSpotProperty expect, AlignmentSpotProperty actual) {
        Assert.AreEqual(expect.AlignmentID, actual.AlignmentID,
                        $"Error AlignmentID | expect {expect.AlignmentID}, actual {actual.AlignmentID}");
        Assert.AreEqual(expect.MassCenter, actual.MassCenter,
                        $"Error MassCenter | expect {expect.AlignmentID}, actual {actual.AlignmentID}");
        Assert.AreEqual(expect.HeightAverage, actual.HeightAverage,
                        $"Error HeightAverage | expect {expect.AlignmentID}, actual {actual.AlignmentID}");
        Assert.AreEqual(expect.PeakCharacter.IsotopeParentPeakID, actual.PeakCharacter.IsotopeParentPeakID,
                        $"Error IsotopeParentPeakID | expect {expect.AlignmentID}, actual {actual.AlignmentID}");
        Assert.AreEqual(expect.PeakCharacter.IsotopeWeightNumber, actual.PeakCharacter.IsotopeWeightNumber,
                        $"Error IsotopeWeightNumber | expect {expect.AlignmentID}, actual {actual.AlignmentID}");
        Assert.AreEqual(expect.PeakCharacter.Charge, actual.PeakCharacter.Charge,
                        $"Error Charge | expect {expect.AlignmentID}, actual {actual.AlignmentID}");
    }
}