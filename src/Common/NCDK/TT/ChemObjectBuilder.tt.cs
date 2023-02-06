



// .NET Framework port by Kazuya Ujihara
// Copyright (C) 2016-2017  Kazuya Ujihara <ujihara.kazuya@gmail.com>

/* Copyright (C) 2010  Egon Willighagen <egonw@users.sf.net>
 *               2012  John May <jwmay@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Formula;
using NCDK.Numerics;
using NCDK.Stereo;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace NCDK.Default
{
    /// <summary>
    /// A factory class to provide implementation independent <see cref="ICDKObject"/>s.
    /// </summary>
    /// <example>
    /// <code>    
    /// IChemObjectBuilder builder = ChemObjectBuilder.Instance;
    /// IAtom a = builder.NewAtom();
    /// IAtom c12 = builder.NewAtom("C");
    /// IAtom c13 = builder.NewAtom(builder.NewIsotope("C", 13));
    /// </code>
    /// </example>
    // @author        egonw
    // @author        john may
    // @cdk.module    data
    public sealed class ChemObjectBuilder
        : IChemObjectBuilder
    {
        public static IChemObjectBuilder Instance { get; } = new ChemObjectBuilder();
        
        private bool LegacyAtomContainer { get; set; }

        internal ChemObjectBuilder()
        {
            var val = System.Environment.GetEnvironmentVariable("NCDKUseLegacyAtomContainer");
            if (string.IsNullOrWhiteSpace(val))
                LegacyAtomContainer = false;
            else
            {
                val = val.Trim();
                switch (val.ToUpperInvariant())
                {
                    case "T":
                    case "TRUE":
                    case "1":
                        LegacyAtomContainer = true;
                        break;
                    case "F":
                    case "FALSE":
                    case "0":
                        LegacyAtomContainer = false;
                        break;
                    default:
                        throw new InvalidOperationException("Invalid value, expected true/false: " + val);
                }
            }

			if (LegacyAtomContainer)
				Trace.TraceError("[WARN] Using the old AtomContainer implementation.");
        }

        internal ChemObjectBuilder(bool legacyAtomContainer)
        {
            this.LegacyAtomContainer = legacyAtomContainer;
        }

#pragma warning disable CA1822
        public T New<T>() where T : IAtomContainer, new() => new T();
#pragma warning restore

        public IAdductFormula NewAdductFormula() => (IAdductFormula)new AdductFormula();
        public IAdductFormula NewAdductFormula(IMolecularFormula formula) => (IAdductFormula)new AdductFormula(formula);
        public IAminoAcid NewAminoAcid() => (IAminoAcid)new AminoAcid();
        public IAtom NewAtom() => (IAtom)new Atom();
        public IAtom NewAtom(IElement element) => (IAtom)new Atom(element);
        public IAtom NewAtom(ChemicalElement element) => (IAtom)new Atom(element);
        public IAtom NewAtom(int atomicNumber) => (IAtom)new Atom(atomicNumber);
        public IAtom NewAtom(int atomicNumber, int implicitHydrogenCount) => (IAtom)new Atom(atomicNumber, implicitHydrogenCount);
        public IAtom NewAtom(int atomicNumber, int implicitHydrogenCount, int formalCharge) => (IAtom)new Atom(atomicNumber, implicitHydrogenCount, formalCharge);
        public IAtom NewAtom(string symbol) => (IAtom)new Atom(symbol);
        public IAtom NewAtom(ChemicalElement element, Vector2 point2d) => (IAtom)new Atom(element, point2d);
        public IAtom NewAtom(string symbol, Vector2 point2d) => (IAtom)new Atom(symbol, point2d);
        public IAtom NewAtom(ChemicalElement element, Vector3 point3d) => (IAtom)new Atom(element, point3d);
        public IAtom NewAtom(string symbol, Vector3 point3d) => (IAtom)new Atom(symbol, point3d);
        public IChemObjectSet<T> NewChemObjectSet<T>() where T : IAtomContainer => (IChemObjectSet<T>)new ChemObjectSet<T>();
        public IAtomContainer NewAtomContainer() => LegacyAtomContainer ? (IAtomContainer)new AtomContainer() : (IAtomContainer)new AtomContainer2();
        public IAtomContainer NewAtomContainer(IAtomContainer container) => LegacyAtomContainer ? (IAtomContainer)new AtomContainer(container) : (IAtomContainer)new AtomContainer2(container);
        public IAtomContainer NewAtomContainer(IEnumerable<IAtom> atoms, IEnumerable<IBond> bonds) => LegacyAtomContainer ? (IAtomContainer)new AtomContainer(atoms, bonds) : (IAtomContainer)new AtomContainer2(atoms, bonds);
        public IAtomContainerSet NewAtomContainerSet() => (IAtomContainerSet)new AtomContainerSet();
        public IAtomType NewAtomType(IElement element) => (IAtomType)new AtomType(element);
        public IAtomType NewAtomType(ChemicalElement element) => (IAtomType)new AtomType(element);
        public IAtomType NewAtomType(string elementSymbol) => (IAtomType)new AtomType(elementSymbol);
        public IAtomType NewAtomType(string identifier, string elementSymbol) => (IAtomType)new AtomType(identifier, elementSymbol);
        public IBioPolymer NewBioPolymer() => (IBioPolymer)new BioPolymer();
        public IBond NewBond() => (IBond)new Bond();
        public IBond NewBond(IAtom atom1, IAtom atom2) => (IBond)new Bond(atom1, atom2);
        public IBond NewBond(IAtom atom1, IAtom atom2, BondOrder order) => (IBond)new Bond(atom1, atom2, order);
        public IBond NewBond(IEnumerable<IAtom> atoms) => (IBond)new Bond(atoms);
        public IBond NewBond(IEnumerable<IAtom> atoms, BondOrder order) => (IBond)new Bond(atoms, order);
        public IBond NewBond(IAtom atom1, IAtom atom2, BondOrder order, BondStereo stereo) => (IBond)new Bond(atom1, atom2, order, stereo);
        public IBond NewBond(IEnumerable<IAtom> atoms, BondOrder order, BondStereo stereo) => (IBond)new Bond(atoms, order, stereo);
        public IChemFile NewChemFile() => (IChemFile)new ChemFile();
        public IChemModel NewChemModel() => (IChemModel)new ChemModel();
        public IChemObject NewChemObject() => (IChemObject)new ChemObject();
        public IChemObject NewChemObject(IChemObject chemObject) => (IChemObject)new ChemObject(chemObject);
        public IChemSequence NewChemSequence() => (IChemSequence)new ChemSequence();
        public ICrystal NewCrystal() => (ICrystal)new Crystal();
        public ICrystal NewCrystal(IAtomContainer container) => (ICrystal)new Crystal(container);
        public IElectronContainer NewElectronContainer() => (IElectronContainer)new ElectronContainer();
        public IFragmentAtom NewFragmentAtom() => (IFragmentAtom)new FragmentAtom();
        public ILonePair NewLonePair() => (ILonePair)new LonePair();
        public ILonePair NewLonePair(IAtom atom) => (ILonePair)new LonePair(atom);
        public IIsotope NewIsotope(ChemicalElement element) => (IIsotope)new Isotope(element);
        public IIsotope NewIsotope(int atomicNumber) => (IIsotope)new Isotope(atomicNumber);
        public IIsotope NewIsotope(string symbol) => (IIsotope)new Isotope(symbol);
        public IIsotope NewIsotope(ChemicalElement element, int massNumber, double exactMass , double abundance) => (IIsotope)new Isotope(element, massNumber, exactMass, abundance);
        public IIsotope NewIsotope(int atomicNumber, int massNumber, double exactMass , double abundance) => (IIsotope)new Isotope(atomicNumber, massNumber, exactMass, abundance);
        public IIsotope NewIsotope(string symbol, int massNumber, double exactMass , double abundance) => (IIsotope)new Isotope(symbol, massNumber, exactMass, abundance);
        public IIsotope NewIsotope(ChemicalElement element, int massNumber) => (IIsotope)new Isotope(element, massNumber);
        public IIsotope NewIsotope(int atomicNumber, int massNumber) => (IIsotope)new Isotope(atomicNumber, massNumber);
        public IIsotope NewIsotope(string symbol, int massNumber) => (IIsotope)new Isotope(symbol, massNumber);
        public IIsotope NewIsotope(ChemicalElement element, double exactMass, double abundance) => (IIsotope)new Isotope(element, exactMass, abundance);
        public IIsotope NewIsotope(int atomicNumber, double exactMass, double abundance) => (IIsotope)new Isotope(atomicNumber, exactMass, abundance);
        public IIsotope NewIsotope(string symbol, double exactMass, double abundance) => (IIsotope)new Isotope(symbol, exactMass, abundance);
        public IIsotope NewIsotope(IElement element) => (IIsotope)new Isotope(element);
        public IMapping NewMapping(IChemObject objectOne, IChemObject objectTwo) => (IMapping)new Mapping(objectOne, objectTwo);
        public IMolecularFormula NewMolecularFormula() => (IMolecularFormula)new MolecularFormula();
        public IMolecularFormulaSet NewMolecularFormulaSet() => (IMolecularFormulaSet)new MolecularFormulaSet();
        public IMolecularFormulaSet NewMolecularFormulaSet(IMolecularFormula formula) => (IMolecularFormulaSet)new MolecularFormulaSet(formula);
        public IMonomer NewMonomer() => (IMonomer)new Monomer();
        public IPseudoAtom NewPseudoAtom() => (IPseudoAtom)new PseudoAtom();
        public IPseudoAtom NewPseudoAtom(ChemicalElement element) => (IPseudoAtom)new PseudoAtom(element);
        public IPseudoAtom NewPseudoAtom(string label) => (IPseudoAtom)new PseudoAtom(label);
        public IPseudoAtom NewPseudoAtom(IElement element) => (IPseudoAtom)new PseudoAtom(element);
        public IPseudoAtom NewPseudoAtom(string label, Vector2 point2d) => (IPseudoAtom)new PseudoAtom(label, point2d);
        public IPseudoAtom NewPseudoAtom(string label, Vector3 point3d) => (IPseudoAtom)new PseudoAtom(label, point3d);
        public IReaction NewReaction() => (IReaction)new Reaction();
        public IReactionSet NewReactionSet() => (IReactionSet)new ReactionSet();
        public IReactionScheme NewReactionScheme() => (IReactionScheme)new ReactionScheme();
        public IPDBAtom NewPDBAtom(ChemicalElement element) => (IPDBAtom)new PDBAtom(element);
        public IPDBAtom NewPDBAtom(IElement element) => (IPDBAtom)new PDBAtom(element);
        public IPDBAtom NewPDBAtom(string symbol) => (IPDBAtom)new PDBAtom(symbol);
        public IPDBAtom NewPDBAtom(string symbol, Vector3 coordinate) => (IPDBAtom)new PDBAtom(symbol, coordinate);
        public IPDBMonomer NewPDBMonomer() => (IPDBMonomer)new PDBMonomer();
        public IPDBPolymer NewPDBPolymer() => (IPDBPolymer)new PDBPolymer();
        public IPDBStrand NewPDBStrand() => (IPDBStrand)new PDBStrand();
        public IPDBStructure NewPDBStructure() => (IPDBStructure)new PDBStructure();
        public IPolymer NewPolymer() => (IPolymer)new Polymer();
        public IRing NewRing() => (IRing)new Ring();
        public IRing NewRing(int ringSize, string elementSymbol) => (IRing)new Ring(ringSize, elementSymbol);
        public IRing NewRing(IAtomContainer atomContainer) => (IRing)new Ring(atomContainer);
        public IRing NewRing(IEnumerable<IAtom> atoms, IEnumerable<IBond> bonds) => (IRing)new Ring(atoms, bonds);
        public IRingSet NewRingSet() => (IRingSet)new RingSet();
        public ISubstance NewSubstance() => (ISubstance)new Substance();
        public ISingleElectron NewSingleElectron() => (ISingleElectron)new SingleElectron();
        public ISingleElectron NewSingleElectron(IAtom atom) => (ISingleElectron)new SingleElectron(atom);
        public IStrand NewStrand() => (IStrand)new Strand();
        public ITetrahedralChirality NewTetrahedralChirality(IAtom chiralAtom, IReadOnlyList<IAtom> ligandAtoms, TetrahedralStereo chirality) => (ITetrahedralChirality)new TetrahedralChirality(chiralAtom, ligandAtoms, chirality) { Builder = this };
        
    }
}
namespace NCDK.Silent
{
    /// <summary>
    /// A factory class to provide implementation independent <see cref="ICDKObject"/>s.
    /// </summary>
    /// <example>
    /// <code>    
    /// IChemObjectBuilder builder = ChemObjectBuilder.Instance;
    /// IAtom a = builder.NewAtom();
    /// IAtom c12 = builder.NewAtom("C");
    /// IAtom c13 = builder.NewAtom(builder.NewIsotope("C", 13));
    /// </code>
    /// </example>
    // @author        egonw
    // @author        john may
    // @cdk.module    data
    public sealed class ChemObjectBuilder
        : IChemObjectBuilder
    {
        public static IChemObjectBuilder Instance { get; } = new ChemObjectBuilder();
        
        private bool LegacyAtomContainer { get; set; }

        internal ChemObjectBuilder()
        {
            var val = System.Environment.GetEnvironmentVariable("NCDKUseLegacyAtomContainer");
            if (string.IsNullOrWhiteSpace(val))
                LegacyAtomContainer = false;
            else
            {
                val = val.Trim();
                switch (val.ToUpperInvariant())
                {
                    case "T":
                    case "TRUE":
                    case "1":
                        LegacyAtomContainer = true;
                        break;
                    case "F":
                    case "FALSE":
                    case "0":
                        LegacyAtomContainer = false;
                        break;
                    default:
                        throw new InvalidOperationException("Invalid value, expected true/false: " + val);
                }
            }

			if (LegacyAtomContainer)
				Trace.TraceError("[WARN] Using the old AtomContainer implementation.");
        }

        internal ChemObjectBuilder(bool legacyAtomContainer)
        {
            this.LegacyAtomContainer = legacyAtomContainer;
        }

#pragma warning disable CA1822
        public T New<T>() where T : IAtomContainer, new() => new T();
#pragma warning restore

        public IAdductFormula NewAdductFormula() => (IAdductFormula)new AdductFormula();
        public IAdductFormula NewAdductFormula(IMolecularFormula formula) => (IAdductFormula)new AdductFormula(formula);
        public IAminoAcid NewAminoAcid() => (IAminoAcid)new AminoAcid();
        public IAtom NewAtom() => (IAtom)new Atom();
        public IAtom NewAtom(IElement element) => (IAtom)new Atom(element);
        public IAtom NewAtom(ChemicalElement element) => (IAtom)new Atom(element);
        public IAtom NewAtom(int atomicNumber) => (IAtom)new Atom(atomicNumber);
        public IAtom NewAtom(int atomicNumber, int implicitHydrogenCount) => (IAtom)new Atom(atomicNumber, implicitHydrogenCount);
        public IAtom NewAtom(int atomicNumber, int implicitHydrogenCount, int formalCharge) => (IAtom)new Atom(atomicNumber, implicitHydrogenCount, formalCharge);
        public IAtom NewAtom(string symbol) => (IAtom)new Atom(symbol);
        public IAtom NewAtom(ChemicalElement element, Vector2 point2d) => (IAtom)new Atom(element, point2d);
        public IAtom NewAtom(string symbol, Vector2 point2d) => (IAtom)new Atom(symbol, point2d);
        public IAtom NewAtom(ChemicalElement element, Vector3 point3d) => (IAtom)new Atom(element, point3d);
        public IAtom NewAtom(string symbol, Vector3 point3d) => (IAtom)new Atom(symbol, point3d);
        public IChemObjectSet<T> NewChemObjectSet<T>() where T : IAtomContainer => (IChemObjectSet<T>)new ChemObjectSet<T>();
        public IAtomContainer NewAtomContainer() => LegacyAtomContainer ? (IAtomContainer)new AtomContainer() : (IAtomContainer)new AtomContainer2();
        public IAtomContainer NewAtomContainer(IAtomContainer container) => LegacyAtomContainer ? (IAtomContainer)new AtomContainer(container) : (IAtomContainer)new AtomContainer2(container);
        public IAtomContainer NewAtomContainer(IEnumerable<IAtom> atoms, IEnumerable<IBond> bonds) => LegacyAtomContainer ? (IAtomContainer)new AtomContainer(atoms, bonds) : (IAtomContainer)new AtomContainer2(atoms, bonds);
        public IAtomContainerSet NewAtomContainerSet() => (IAtomContainerSet)new AtomContainerSet();
        public IAtomType NewAtomType(IElement element) => (IAtomType)new AtomType(element);
        public IAtomType NewAtomType(ChemicalElement element) => (IAtomType)new AtomType(element);
        public IAtomType NewAtomType(string elementSymbol) => (IAtomType)new AtomType(elementSymbol);
        public IAtomType NewAtomType(string identifier, string elementSymbol) => (IAtomType)new AtomType(identifier, elementSymbol);
        public IBioPolymer NewBioPolymer() => (IBioPolymer)new BioPolymer();
        public IBond NewBond() => (IBond)new Bond();
        public IBond NewBond(IAtom atom1, IAtom atom2) => (IBond)new Bond(atom1, atom2);
        public IBond NewBond(IAtom atom1, IAtom atom2, BondOrder order) => (IBond)new Bond(atom1, atom2, order);
        public IBond NewBond(IEnumerable<IAtom> atoms) => (IBond)new Bond(atoms);
        public IBond NewBond(IEnumerable<IAtom> atoms, BondOrder order) => (IBond)new Bond(atoms, order);
        public IBond NewBond(IAtom atom1, IAtom atom2, BondOrder order, BondStereo stereo) => (IBond)new Bond(atom1, atom2, order, stereo);
        public IBond NewBond(IEnumerable<IAtom> atoms, BondOrder order, BondStereo stereo) => (IBond)new Bond(atoms, order, stereo);
        public IChemFile NewChemFile() => (IChemFile)new ChemFile();
        public IChemModel NewChemModel() => (IChemModel)new ChemModel();
        public IChemObject NewChemObject() => (IChemObject)new ChemObject();
        public IChemObject NewChemObject(IChemObject chemObject) => (IChemObject)new ChemObject(chemObject);
        public IChemSequence NewChemSequence() => (IChemSequence)new ChemSequence();
        public ICrystal NewCrystal() => (ICrystal)new Crystal();
        public ICrystal NewCrystal(IAtomContainer container) => (ICrystal)new Crystal(container);
        public IElectronContainer NewElectronContainer() => (IElectronContainer)new ElectronContainer();
        public IFragmentAtom NewFragmentAtom() => (IFragmentAtom)new FragmentAtom();
        public ILonePair NewLonePair() => (ILonePair)new LonePair();
        public ILonePair NewLonePair(IAtom atom) => (ILonePair)new LonePair(atom);
        public IIsotope NewIsotope(ChemicalElement element) => (IIsotope)new Isotope(element);
        public IIsotope NewIsotope(int atomicNumber) => (IIsotope)new Isotope(atomicNumber);
        public IIsotope NewIsotope(string symbol) => (IIsotope)new Isotope(symbol);
        public IIsotope NewIsotope(ChemicalElement element, int massNumber, double exactMass , double abundance) => (IIsotope)new Isotope(element, massNumber, exactMass, abundance);
        public IIsotope NewIsotope(int atomicNumber, int massNumber, double exactMass , double abundance) => (IIsotope)new Isotope(atomicNumber, massNumber, exactMass, abundance);
        public IIsotope NewIsotope(string symbol, int massNumber, double exactMass , double abundance) => (IIsotope)new Isotope(symbol, massNumber, exactMass, abundance);
        public IIsotope NewIsotope(ChemicalElement element, int massNumber) => (IIsotope)new Isotope(element, massNumber);
        public IIsotope NewIsotope(int atomicNumber, int massNumber) => (IIsotope)new Isotope(atomicNumber, massNumber);
        public IIsotope NewIsotope(string symbol, int massNumber) => (IIsotope)new Isotope(symbol, massNumber);
        public IIsotope NewIsotope(ChemicalElement element, double exactMass, double abundance) => (IIsotope)new Isotope(element, exactMass, abundance);
        public IIsotope NewIsotope(int atomicNumber, double exactMass, double abundance) => (IIsotope)new Isotope(atomicNumber, exactMass, abundance);
        public IIsotope NewIsotope(string symbol, double exactMass, double abundance) => (IIsotope)new Isotope(symbol, exactMass, abundance);
        public IIsotope NewIsotope(IElement element) => (IIsotope)new Isotope(element);
        public IMapping NewMapping(IChemObject objectOne, IChemObject objectTwo) => (IMapping)new Mapping(objectOne, objectTwo);
        public IMolecularFormula NewMolecularFormula() => (IMolecularFormula)new MolecularFormula();
        public IMolecularFormulaSet NewMolecularFormulaSet() => (IMolecularFormulaSet)new MolecularFormulaSet();
        public IMolecularFormulaSet NewMolecularFormulaSet(IMolecularFormula formula) => (IMolecularFormulaSet)new MolecularFormulaSet(formula);
        public IMonomer NewMonomer() => (IMonomer)new Monomer();
        public IPseudoAtom NewPseudoAtom() => (IPseudoAtom)new PseudoAtom();
        public IPseudoAtom NewPseudoAtom(ChemicalElement element) => (IPseudoAtom)new PseudoAtom(element);
        public IPseudoAtom NewPseudoAtom(string label) => (IPseudoAtom)new PseudoAtom(label);
        public IPseudoAtom NewPseudoAtom(IElement element) => (IPseudoAtom)new PseudoAtom(element);
        public IPseudoAtom NewPseudoAtom(string label, Vector2 point2d) => (IPseudoAtom)new PseudoAtom(label, point2d);
        public IPseudoAtom NewPseudoAtom(string label, Vector3 point3d) => (IPseudoAtom)new PseudoAtom(label, point3d);
        public IReaction NewReaction() => (IReaction)new Reaction();
        public IReactionSet NewReactionSet() => (IReactionSet)new ReactionSet();
        public IReactionScheme NewReactionScheme() => (IReactionScheme)new ReactionScheme();
        public IPDBAtom NewPDBAtom(ChemicalElement element) => (IPDBAtom)new PDBAtom(element);
        public IPDBAtom NewPDBAtom(IElement element) => (IPDBAtom)new PDBAtom(element);
        public IPDBAtom NewPDBAtom(string symbol) => (IPDBAtom)new PDBAtom(symbol);
        public IPDBAtom NewPDBAtom(string symbol, Vector3 coordinate) => (IPDBAtom)new PDBAtom(symbol, coordinate);
        public IPDBMonomer NewPDBMonomer() => (IPDBMonomer)new PDBMonomer();
        public IPDBPolymer NewPDBPolymer() => (IPDBPolymer)new PDBPolymer();
        public IPDBStrand NewPDBStrand() => (IPDBStrand)new PDBStrand();
        public IPDBStructure NewPDBStructure() => (IPDBStructure)new PDBStructure();
        public IPolymer NewPolymer() => (IPolymer)new Polymer();
        public IRing NewRing() => (IRing)new Ring();
        public IRing NewRing(int ringSize, string elementSymbol) => (IRing)new Ring(ringSize, elementSymbol);
        public IRing NewRing(IAtomContainer atomContainer) => (IRing)new Ring(atomContainer);
        public IRing NewRing(IEnumerable<IAtom> atoms, IEnumerable<IBond> bonds) => (IRing)new Ring(atoms, bonds);
        public IRingSet NewRingSet() => (IRingSet)new RingSet();
        public ISubstance NewSubstance() => (ISubstance)new Substance();
        public ISingleElectron NewSingleElectron() => (ISingleElectron)new SingleElectron();
        public ISingleElectron NewSingleElectron(IAtom atom) => (ISingleElectron)new SingleElectron(atom);
        public IStrand NewStrand() => (IStrand)new Strand();
        public ITetrahedralChirality NewTetrahedralChirality(IAtom chiralAtom, IReadOnlyList<IAtom> ligandAtoms, TetrahedralStereo chirality) => (ITetrahedralChirality)new TetrahedralChirality(chiralAtom, ligandAtoms, chirality) { Builder = this };
        
    }
}
namespace NCDK
{
    public partial class Chem
    {
        public static IAtom NewAtom() => (IAtom)CDK.Builder.NewAtom();
        public static IAtom NewAtom(IElement element) => (IAtom)CDK.Builder.NewAtom(element);
        public static IAtom NewAtom(ChemicalElement element) => (IAtom)CDK.Builder.NewAtom(element);
        public static IAtom NewAtom(int atomicNumber) => (IAtom)CDK.Builder.NewAtom(atomicNumber);
        public static IAtom NewAtom(int atomicNumber, int implicitHydrogenCount) => (IAtom)CDK.Builder.NewAtom(atomicNumber, implicitHydrogenCount);
        public static IAtom NewAtom(int atomicNumber, int implicitHydrogenCount, int formalCharge) => (IAtom)CDK.Builder.NewAtom(atomicNumber, implicitHydrogenCount, formalCharge);
        public static IAtom NewAtom(string symbol) => (IAtom)CDK.Builder.NewAtom(symbol);
        public static IAtom NewAtom(ChemicalElement element, Vector2 point2d) => (IAtom)CDK.Builder.NewAtom(element, point2d);
        public static IAtom NewAtom(string symbol, Vector2 point2d) => (IAtom)CDK.Builder.NewAtom(symbol, point2d);
        public static IAtom NewAtom(ChemicalElement element, Vector3 point3d) => (IAtom)CDK.Builder.NewAtom(element, point3d);
        public static IAtom NewAtom(string symbol, Vector3 point3d) => (IAtom)CDK.Builder.NewAtom(symbol, point3d);
        public static IAtomContainer NewAtomContainer() => (IAtomContainer)CDK.Builder.NewAtomContainer();
        public static IAtomContainer NewAtomContainer(IAtomContainer container) => (IAtomContainer)CDK.Builder.NewAtomContainer(container);
        public static IAtomContainer NewAtomContainer(IEnumerable<IAtom> atoms, IEnumerable<IBond> bonds) => (IAtomContainer)CDK.Builder.NewAtomContainer(atoms, bonds);
        public static IBond NewBond() => (IBond)CDK.Builder.NewBond();
        public static IBond NewBond(IAtom atom1, IAtom atom2) => (IBond)CDK.Builder.NewBond(atom1, atom2);
        public static IBond NewBond(IAtom atom1, IAtom atom2, BondOrder order) => (IBond)CDK.Builder.NewBond(atom1, atom2, order);
        public static IBond NewBond(IEnumerable<IAtom> atoms) => (IBond)CDK.Builder.NewBond(atoms);
        public static IBond NewBond(IEnumerable<IAtom> atoms, BondOrder order) => (IBond)CDK.Builder.NewBond(atoms, order);
        public static IBond NewBond(IAtom atom1, IAtom atom2, BondOrder order, BondStereo stereo) => (IBond)CDK.Builder.NewBond(atom1, atom2, order, stereo);
        public static IBond NewBond(IEnumerable<IAtom> atoms, BondOrder order, BondStereo stereo) => (IBond)CDK.Builder.NewBond(atoms, order, stereo);
        public static ILonePair NewLonePair() => (ILonePair)CDK.Builder.NewLonePair();
        public static ILonePair NewLonePair(IAtom atom) => (ILonePair)CDK.Builder.NewLonePair(atom);
        public static IReaction NewReaction() => (IReaction)CDK.Builder.NewReaction();
        public static ISingleElectron NewSingleElectron() => (ISingleElectron)CDK.Builder.NewSingleElectron();
        public static ISingleElectron NewSingleElectron(IAtom atom) => (ISingleElectron)CDK.Builder.NewSingleElectron(atom);
    }
}

