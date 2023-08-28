using CompMs.Common.DataObj.Property;
using CompMs.Common.StructureFinder.Descriptor;
using CompMs.Common.StructureFinder.Mapper;
using CompMs.Common.StructureFinder.Utility;
using NCDK;
using NCDK.Tools.Manipulator;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.StructureFinder.Property
{
    #region Enums
    //cite: https://www.pinterest.com/explore/functional-group/
    //R means C or H
    public enum AtomFunctionType
    {
        //hydrogen
        H_AcidicProton, H_NonacidicProton,

        //oxygen enum R-OH, R=O, C-O-C, R-O-O-R', C=[O+]-C, =C#[O+], C-O(-C)-C, C-O-HeteroAtom other
        O_Hydroxy, O_Ketone, O_Ether, O_Peroxide, O_TripleBondsType1COC, O_TripleBondsType2CO, O_TripleBondsTypeCOCC, O_HeteroEther, O_Other,

        //phosphorus enum RO-P(=O)(-[O-])-OR1, R1-P(-R2)-(R3), R1-O-P(=O)(-O-R2)-R3, R1-O-P(=O)(-O-R2)-O-R3, R1-P(=O)(-R2)-R3, R1-O-P(-O-R2)-O-R3,
        //R1-O-P(-R2)-O-R3, R1-P(-R2)-O-R3, Other
        P_Phosphodiester, P_Phosphine, P_PhosphonatePO3, P_PhosphonatePO4, P_PhosphineOxide, P_PhosphiteEster, P_Phosphonite, P_Phosphinite, P_Other,

        //C-S-C, R1-S-S-R2, R1-S(=O)(=O)-R2, R-SH, R1-C(=S)-R2, R1-S(=O)-R2, R1-S(=O)(=O)-N-R2 , S(=O)(=O)-O
        S_ThioEther, S_Disulfide, S_Sulfone, S_Thiol, S_Thione, S_Sulfoxide, S_Sulfonamide, S_Sulfonate, S_Sulfate, S_Other,

        //C-[N+](-[O-])(-C)-C, C-NH2, C-NH-C, C-N(-C)-C, C-[N+](-C)(-C)-C,R1-N=C, -N-N-, -N=N-
        //, -C#N, C-NO2, HO-N=C, O=N-C,C-N(-OH)-C, N=[N+]=N
        N_Noxide, N_PrimaryAmine, N_SecondaryAmine, N_TertiaryAmine, N_QuatenaryAmine, N_Imine, N_Hydrazine, N_Azo, N_Nitrile, 
        N_Nitro, N_Ketoxime, N_Nitroso, N_HydroxyAmine, N_Azide, N_Other,

        //R1-Ct(-R2)-R3, R1-Ct(=R2)-R3, R1-Ct#R2, C=Ct=C, C-Ct(=O)-O-C, C-Ct(=O)-S-C, C=Ct=O, R1-Ct(-R2)-OH, C-Ct(=O)-C, C-Ct(=C)-OH, C-Ct(=O)-H, Ct-O-C
        //C-Ct(=O)-OH, RO-Ct(=O)-NR, CO-Ct(-OC)(-C)-C, C-C(-H)(-O-C)-O-C, C-C(-H)(-OH)-O-C, R1NH-C(=S)-R2, R1-C(-R2)-SH, R1-C(=S)-R2, RN=C=S,
        //R1NH-C(=O)-R2, R1-C(=NR2)-R3, R1NH-C(=O)-NHR2, -N=C=O, R1-C#N, R1-C(=C)-NH2, -N=C=N-, -N=C-N- -C-C(=NOH)-C, C-C(=NOH)-H, N=C-O, N=C(N)N
        C_Methyl, C_Methylene, C_Alkane, C_Alkene, C_Alkyne, C_Allene, C_Ester, C_Thioester, C_Ketene, C_Alcohol, C_Ketone, C_Enol, C_Aldehyde, C_Ether,
        C_Carboxylate, C_Carbamate, C_Ketal, C_Acetal, C_Hemiacetal, C_ThioAmide, C_Thiol, C_Thione, C_Isothiocyanate, C_PrimaryAmine,
        C_Amide, C_Imine, C_Carbamide, C_Isocyanate, C_Nitrile, C_Enamine, C_Carbodiimide, C_Carbodiamine, C_Carboxamidine, C_Ketoxime, C_Aldoxime,
        C_CarboximicAcid, C_Guanidine, C_HalogenConnected, C_HeteroatomConnected, C_HalogenHeteroConjugated, C_Other, 
        
        //Halogen
        X_Halogen,

        //Silicon
        Si_TriCarbon, Si_Other,

        Other
    }

    public enum RingFunctionType
    {
        //C3 C1CC1, C1OC1, C1NC1
        Cyclopropane, Epoxide, Aziridine, OtherC3Ring,

        //C4 C1CCC1, C1CC=C1, C1=CC=C1 C1NCC1, C1OCC1
        Cyclobutane, Cyclobutene, Cyclobutadiene, Azetidine, Oxetane, OtherC4Ring,

        //C5 non-aromatic ring: C1CCCCC1 C1CCC=CC1 C1C=CC=CC1 N1CCCC1 S1CCCC1 O1CCCC1 N1CC=CC1 C1OCCO1
        Cyclopentane, Cyclopentene, Cyclopendadiene, Pyrrolidine, Thiolane, Tetrahydrofuran, HydroPyrrolidine, Dioxolane,

        //C5 aromatic ring: n1cccc1 n1cncc1 n1nccc1 s1cncc1 s1nccc1 o1cncc o1nccc1 s1cccc1 o1cccc1 n1ncnc1
        Pyrrole, Imidazole, Pyrazole, Thiazole, Isothioazole, Oxazole, IsoOxazole, Thiofene, Furan, Triazole, OtherC5Ring,

        //C6 non-aromatic ring: C1CCCCC1 C1CC=CCC1 C1C=CC=CC1 N1CCCCC1 O1CCCCC1 N1CCNCC1 N1CCOCC1 O1C=CCC=C1 O1CC=CC=C1 O1C=CCCC1 N1C=C-C-C=C1 N1C=CCCC1 P1OCCCO1 
        Cyclohexane, Cyclohexene, Cyclohexadiene, Piperidine, Tetrahydropyran, 
        Piperazine, Morpholine, FhPyran, ThPyran, DihydroPyran, Dihydropyridine, Dihydropyrimidine, Hydropiperidine, CycloO1POCCC1,

        //C6 aromatic ring: c1ccccc1 n1ccccc1 n1cnccc1 n1ccncc1 o1ccccc1
        Benzene, Pyridine, Pyrimidine, Pyrazine, Pyridazine, Pyrylium, AromaticPyran, OtherC6Ring,

        //C7 non-aromatic ring: C1CCCCCC1 C1CCCC=CC1 C1CC=CC=CC1 C1=CC=CC=CC1 N1=CC=CC=CC1 N1C=CC=NC=C1 N1=C-C=C-S-C=C1
        CycloOctane, CycloOctene, CycloOctadiene, CycloOctatriene, Azepine, Diazepine, Thiazepine, OtherC7Ring,

        //C9 non-aromatic ring: O1CC=CCCCCC1
        Oxonane,
        
        Other
    }

    public enum RingsetFunctionType
    {
        SingleRingset,

        //C5-C5
        TetrahydroPyrrolizine, OtherC5C5Ringset,

        //C6-C5
        Indole, DihydroIndole, Benzimidazole, Benzofuran, Purine, DihydroPurine, Tropane, Benzodioxole, ThFran_CycloO1POCCC1, OtherC6C5Ringset,
        
        //C6-C6
        Naphthalene, Quinoline, Isoquinoline, Quinazoline, Phthalazine,
        Pteridine, Coumarin, Chromone, Chroman, Chromenylium, OtherC6C6Ringset,

        //6-6-6
        Citizin,

        //6-7-6
        DibenzoThiazepine,

        //Others
        ConjugatedAromaticRingset, ConjugatedHeteroatomContainedRingset, 
        ConjugatedAromaticHeteroatomContainedRingset,
        SteroidRingset, 
        
        OtherRingset,
    }

    public enum StructureFunctionType { Glycerolipid, GlyceroPhospholipid, Sphingolipid, ShingoPhospholipid,
        Flavonoid, PhenolicAcid, Sinapoyl, Carotenoid, Tocopherol, Quinone, Sterol, Glucosinolate, Alkaloid, Other }
    #endregion

    public class Structure
    {
        //metadata
        private string title;
        private string smiles;
        private string id; //short inchikey
        private string inchikey;
        private string ontology;
        private string ontologyID;
        private string resourceNames;
        private int resourceNumber;
        private string databaseQueries;

        //structure info
        private IAtomContainer iContainer;
        private StructureFunctionType structureType;

        private float exactMass;
        private bool isValidatedStructure;
        private double xlogP;
        private float retentiontime;
        private Dictionary<string, float> adductToCcs;
        private string errorMessage;
        private double totalBondEnergy;
        private List<string> substructureInChIKeys;
        private List<string> substructureOntologies;

        private Formula formula;
        private MolecularFingerprint molecularDescriptor;

        private Dictionary<int, AtomProperty> atomDictionary;
        private Dictionary<int, BondProperty> bondDictionary;
        private Dictionary<int, RingProperty> ringDictionary;
        private Dictionary<int, RingsetProperty> ringsetDictionary;

        public Structure(IAtomContainer container, List<LabeledAtom> labeledAtoms = null)
        {
            this.errorMessage = string.Empty;

            if (container == null) {
                this.errorMessage = "IAtomContainer was null.";
                this.isValidatedStructure = false; return;
            }

            this.iContainer = container;
            
            AtomContainerManipulator.ConvertImplicitToExplicitHydrogens(container);
            this.isValidatedStructure =  ExplicitHydrogenAdder.AddExplicitHydrogens(container);
            if (this.isValidatedStructure == false) {
                this.errorMessage = "ExplicitHydrogenAdder was not passed.";
                return;
            }

            this.isValidatedStructure = MoleculeMapper.MapAtomMass(container, labeledAtoms);
            if (this.isValidatedStructure == false) {
                this.errorMessage = "MoleculeMapper was not passed.";
                return;
            }

            MoleculeMapper.MapMolecule(this.iContainer, out this.atomDictionary, out this.bondDictionary, 
                out this.ringDictionary, out this.ringsetDictionary, out this.structureType, 
                out this.molecularDescriptor, out this.smiles);

            this.exactMass = this.atomDictionary.Sum(n => n.Value.AtomMass);
            this.formula = MoleculeConverter.ConvertAtomDicionaryToFormula(this.atomDictionary);
            this.totalBondEnergy = BondEnergyCalculator.TotalBondEnergy(this);
            this.substructureInChIKeys = MoleculeMapper.GetAssignedSubstructureInChIKeys(this.molecularDescriptor);
            this.substructureOntologies = MoleculeMapper.GetAssignedFragmentOntologies(this.substructureInChIKeys);
            this.xlogP = XlogpCalculator.XlogP(this);
            this.adductToCcs = new Dictionary<string, float>();
            //this.inchikey = MoleculeConverter.AtomContainerToInChIKey(this.iContainer);

            PubChemFingerprint.SetSection1Properties(this.molecularDescriptor, this.formula);
        }

        #region property
        public IAtomContainer IContainer
        {
            get { return iContainer; }
            set { iContainer = value; }
        }

        public Formula Formula
        {
            get { return formula; }
            set { formula = value; }
        }

        public float ExactMass
        {
            get { return exactMass; }
            set { exactMass = value; }
        }

        public bool IsValidatedStructure
        {
            get { return isValidatedStructure; }
            set { isValidatedStructure = value; }
        }

        public Dictionary<int, AtomProperty> AtomDictionary
        {
            get { return atomDictionary; }
            set { atomDictionary = value; }
        }

        public Dictionary<int, BondProperty> BondDictionary
        {
            get { return bondDictionary; }
            set { bondDictionary = value; }
        }

        public Dictionary<int, RingProperty> RingDictionary
        {
            get { return ringDictionary; }
            set { ringDictionary = value; }
        }

        public Dictionary<int, RingsetProperty> RingsetDictionary
        {
            get { return ringsetDictionary; }
            set { ringsetDictionary = value; }
        }

        public string ErrorMessage
        {
            get { return errorMessage; }
            set { errorMessage = value; }
        }

        public StructureFunctionType StructureType
        {
            get { return structureType; }
            set { structureType = value; }
        }

        public MolecularFingerprint MolecularDescriptor
        {
            get { return molecularDescriptor; }
            set { molecularDescriptor = value; }
        }

        public string Title
        {
            get {
                return title;
            }

            set {
                title = value;
            }
        }

        public string Smiles
        {
            get {
                return smiles;
            }

            set {
                smiles = value;
            }
        }

        public string Id
        {
            get {
                return id;
            }

            set {
                id = value;
            }
        }

        public string Inchikey
        {
            get {
                return inchikey;
            }

            set {
                inchikey = value;
            }
        }

        public double TotalBondEnergy {
            get {
                return totalBondEnergy;
            }

            set {
                totalBondEnergy = value;
            }
        }

        public string ResourceNames {
            get {
                return resourceNames;
            }

            set {
                resourceNames = value;
            }
        }

        public int ResourceNumber {
            get {
                return resourceNumber;
            }

            set {
                resourceNumber = value;
            }
        }

        public string DatabaseQueries
        {
            get { return databaseQueries; }
            set { databaseQueries = value; }
        }

        public List<string> SubstructureInChIKeys {
            get {
                return substructureInChIKeys;
            }

            set {
                substructureInChIKeys = value;
            }
        }

        public string Ontology {
            get {
                return ontology;
            }

            set {
                ontology = value;
            }
        }

        public string OntologyID {
            get {
                return ontologyID;
            }

            set {
                ontologyID = value;
            }
        }

        public double XlogP {
            get {
                return xlogP;
            }

            set {
                xlogP = value;
            }
        }

        public List<string> SubstructureOntologies {
            get {
                return substructureOntologies;
            }

            set {
                substructureOntologies = value;
            }
        }

        public float Retentiontime {
            get {
                return retentiontime;
            }

            set {
                retentiontime = value;
            }
        }

        public Dictionary<string, float> AdductToCcs {
            get {
                return adductToCcs;
            }

            set {
                adductToCcs = value;
            }
        }

        #endregion
    }

    public class RingBasicEnvironmentProperty
    {
        //atom in ring
        private int carbonInRing; //carbon count in ring
        private int nitorogenInRing; //nitrogen count in ring
        private int sulfurInRing; //sulfur count in ring
        private int phosphorusInRing; // phosphorus count in ring
        private int oxygenInRing; // oxygen count in ring
        private int sp2AtomInRing; //sp2 count in ring

        //bond in ring
        private int doublebondInRing;
        private int aromaticBondInRing;

        private int ncnConnect; //N-C-N bond count in ring
        private int ocnConnect;
        private int scnConnect;
        private int scsConnect;
        private int nncConnect;
        private int nnnConnect;
        private int cncConnect;
        private int onnConnect;
        private int cocConnect;
        private int conConnect;
        private int cscConnect;
        private int csnConnect;
        private int ocoConnect;
        private int opoConnect;

        //specific atom in ring 
        private int esterInRing;
        private int etherInRing;
        private int oxoniumInRing;

        //out of ring
        private int hydroxyOutRing;
        private int ketonOutRing;
        private int etherOutRing;
        private int methylOutRing;
        private int carboxylateOutRing;
        private int alcoholOutRing;
        private int primaryAmineOutRing;
        private int esterOutRing;

        private int carbon_AlkeneOutRing; //ring-C-C=C
        private int carbon_AlkaneOutRing; //ring-C-C-C
        private int carbon_KetoneOutRing; //ring-C-C=O
        private int carbon_OxygenOutRing; //ring-C-O-R
        private int carbon_DoubleOxygensOutRing; //ring-C(=O)-O-R
        private int carbon_NitrogenOutRing; //ring-C-N-R
        private int carbon_AmideOutRing; //ring-C(=O)-N-R
        private int carbon_ImidateOutRing; //ring-C(=N)-O-R

        private int hydrogenOutRing;
        private int carbonOutRing;
        private int nitrogenOutRing;
        private int sulfurOutRing;
        private int oxygenOutRing;
        private int phosphorusOutRing;
        private int fOutRing;
        private int clOutRing;
        private int brOutRing;
        private int iOutRing;
        private int doubleSingleCarbonsOutRing;


        private int singleBond_CarbonOutRing;
        private int doubleBond_CarbonOutRing;

        //dictionary of outside atoms <atomid, List<atomprop>>
        private Dictionary<int, List<AtomProperty>> outsideAtomDictionary;
        //dictionary of inside atoms <atomid, List<atomprop>>
        private Dictionary<int, List<AtomProperty>> insideAtomDictionary;
        //dictionary of inside atoms <atomid, List<bondprop>>
        private Dictionary<int, List<BondProperty>> outsideBondDictionary;
        //dictionary of inside atoms <atomid, List<bondprop>>
        private Dictionary<int, List<BondProperty>> insideBondDictionary;

        public RingBasicEnvironmentProperty()
        {
            outsideAtomDictionary = new Dictionary<int, List<AtomProperty>>();
            insideAtomDictionary = new Dictionary<int, List<AtomProperty>>();
            outsideBondDictionary = new Dictionary<int, List<BondProperty>>();
            insideBondDictionary = new Dictionary<int, List<BondProperty>>();
        }

        #region

        public int CarbonInRing
        {
            get { return carbonInRing; }
            set { carbonInRing = value; }
        }

        public int NitorogenInRing
        {
            get { return nitorogenInRing; }
            set { nitorogenInRing = value; }
        }

        public int SulfurInRing
        {
            get { return sulfurInRing; }
            set { sulfurInRing = value; }
        }

        public int PhosphorusInRing
        {
            get { return phosphorusInRing; }
            set { phosphorusInRing = value; }
        }

        public int OxygenInRing
        {
            get { return oxygenInRing; }
            set { oxygenInRing = value; }
        }

        public int DoublebondInRing
        {
            get { return doublebondInRing; }
            set { doublebondInRing = value; }
        }

        public int Sp2AtomInRing
        {
            get { return sp2AtomInRing; }
            set { sp2AtomInRing = value; }
        }

        public int HydrogenOutRing
        {
            get { return hydrogenOutRing; }
            set { hydrogenOutRing = value; }
        }

        public int EsterOutRing
        {
            get { return esterOutRing; }
            set { esterOutRing = value; }
        }

        public int HydroxyOutRing
        {
            get { return hydroxyOutRing; }
            set { hydroxyOutRing = value; }
        }

        public int EtherOutRing
        {
            get { return etherOutRing; }
            set { etherOutRing = value; }
        }

        public int AromaticBondInRing
        {
            get { return aromaticBondInRing; }
            set { aromaticBondInRing = value; }
        }

        public int DoubleSingleCarbonsOutRing
        {
            get { return doubleSingleCarbonsOutRing; }
            set { doubleSingleCarbonsOutRing = value; }
        }

        public int SingleBond_CarbonOutRing
        {
            get { return singleBond_CarbonOutRing; }
            set { singleBond_CarbonOutRing = value; }
        }

        public int DoubleBond_CarbonOutRing
        {
            get { return doubleBond_CarbonOutRing; }
            set { doubleBond_CarbonOutRing = value; }
        }

        public int Carbon_AmideOutRing
        {
            get { return carbon_AmideOutRing; }
            set { carbon_AmideOutRing = value; }
        }

        public int MethylOutRing
        {
            get { return methylOutRing; }
            set { methylOutRing = value; }
        }

        public int CarboxylateOutRing
        {
            get { return carboxylateOutRing; }
            set { carboxylateOutRing = value; }
        }

        public int AlcoholOutRing
        {
            get { return alcoholOutRing; }
            set { alcoholOutRing = value; }
        }

        public int CarbonOutRing
        {
            get { return carbonOutRing; }
            set { carbonOutRing = value; }
        }

        public int KetonOutRing
        {
            get { return ketonOutRing; }
            set { ketonOutRing = value; }
        }

        public int PrimaryAmineOutRing
        {
            get { return primaryAmineOutRing; }
            set { primaryAmineOutRing = value; }
        }

        public int NitrogenOutRing
        {
            get { return nitrogenOutRing; }
            set { nitrogenOutRing = value; }
        }

        public int SulfurOutRing
        {
            get { return sulfurOutRing; }
            set { sulfurOutRing = value; }
        }

        public int OxygenOutRing
        {
            get { return oxygenOutRing; }
            set { oxygenOutRing = value; }
        }

        public int PhosphorusOutRing
        {
            get { return phosphorusOutRing; }
            set { phosphorusOutRing = value; }
        }

        public int EsterInRing
        {
            get { return esterInRing; }
            set { esterInRing = value; }
        }

        public int EtherInRing
        {
            get { return etherInRing; }
            set { etherInRing = value; }
        }

        public int OxoniumInRing
        {
            get { return oxoniumInRing; }
            set { oxoniumInRing = value; }
        }

        public int Carbon_AlkaneOutRing
        {
            get { return carbon_AlkaneOutRing; }
            set { carbon_AlkaneOutRing = value; }
        }

        public int Carbon_AlkeneOutRing
        {
            get { return carbon_AlkeneOutRing; }
            set { carbon_AlkeneOutRing = value; }
        }

        public int Carbon_KetoneOutRing
        {
            get { return carbon_KetoneOutRing; }
            set { carbon_KetoneOutRing = value; }
        }

        public int Carbon_OxygenOutRing
        {
            get { return carbon_OxygenOutRing; }
            set { carbon_OxygenOutRing = value; }
        }

        public int Carbon_NitrogenOutRing
        {
            get { return carbon_NitrogenOutRing; }
            set { carbon_NitrogenOutRing = value; }
        }

        public int Carbon_DoubleOxygensOutRing
        {
            get { return carbon_DoubleOxygensOutRing; }
            set { carbon_DoubleOxygensOutRing = value; }
        }

        public int Carbon_ImidateOutRing
        {
            get { return carbon_ImidateOutRing; }
            set { carbon_ImidateOutRing = value; }
        }

        public int NcnConnect
        {
            get { return ncnConnect; }
            set { ncnConnect = value; }
        }

        public int OcnConnect
        {
            get { return ocnConnect; }
            set { ocnConnect = value; }
        }

        public int ScnConnect
        {
            get { return scnConnect; }
            set { scnConnect = value; }
        }

        public int ScsConnect
        {
            get { return scsConnect; }
            set { scsConnect = value; }
        }

        public int NncConnect
        {
            get { return nncConnect; }
            set { nncConnect = value; }
        }

        public int NnnConnect
        {
            get { return nnnConnect; }
            set { nnnConnect = value; }
        }

        public int CncConnect
        {
            get { return cncConnect; }
            set { cncConnect = value; }
        }

        public int OnnConnect
        {
            get { return onnConnect; }
            set { onnConnect = value; }
        }

        public int CocConnect
        {
            get { return cocConnect; }
            set { cocConnect = value; }
        }

        public int ConConnect
        {
            get { return conConnect; }
            set { conConnect = value; }
        }

        public int CscConnect
        {
            get { return cscConnect; }
            set { cscConnect = value; }
        }

        public int CsnConnect
        {
            get { return csnConnect; }
            set { csnConnect = value; }
        }

        public int OcoConnect
        {
            get { return ocoConnect; }
            set { ocoConnect = value; }
        }

        public int OpoConnect
        {
            get { return opoConnect; }
            set { opoConnect = value; }
        }

        public Dictionary<int, List<AtomProperty>> OutsideAtomDictionary
        {
            get { return outsideAtomDictionary; }
            set { outsideAtomDictionary = value; }
        }
        public Dictionary<int, List<AtomProperty>> InsideAtomDictionary
        {
            get { return insideAtomDictionary; }
            set { insideAtomDictionary = value; }
        }

        public Dictionary<int, List<BondProperty>> OutsideBondDictionary
        {
            get { return outsideBondDictionary; }
            set { outsideBondDictionary = value; }
        }

        public Dictionary<int, List<BondProperty>> InsideBondDictionary
        {
            get { return insideBondDictionary; }
            set { insideBondDictionary = value; }
        }

        public int FOutRing
        {
            get { return fOutRing; }
            set { fOutRing = value; }
        }

        public int ClOutRing
        {
            get { return clOutRing; }
            set { clOutRing = value; }
        }

        public int BrOutRing
        {
            get { return brOutRing; }
            set { brOutRing = value; }
        }

        public int IOutRing
        {
            get { return iOutRing; }
            set { iOutRing = value; }
        }


        #endregion
    }

    public class RingProperty
    {
        private IAtomContainer iRing;
        private int ringID;
        private int ringsetID;
        private RingFunctionType ringFunctionType;

        private bool isAromaticRing;
        private bool isSugarRing;
        private bool isKetoneRing;
        private bool isEsterRing;
        private bool isHeteroRing;
        private bool isBenzeneRing;

        private RingBasicEnvironmentProperty ringEnv;

        private List<AtomProperty> connectedAtoms;
        private List<BondProperty> connectedBonds;

        public RingProperty()
        {
            connectedAtoms = new List<AtomProperty>();
            connectedBonds = new List<BondProperty>();
            ringEnv = new RingBasicEnvironmentProperty();
            ringID = -1;
            ringsetID = -1;
        }

        #region property
        public IAtomContainer IRing
        {
            get {
                return iRing;
            }

            set {
                iRing = value;
            }
        }

        public RingBasicEnvironmentProperty RingEnv
        {
            get { return ringEnv; }
            set { ringEnv = value; }
        }

        public bool IsHeteroRing
        {
            get { return isHeteroRing; }
            set { isHeteroRing = value; }
        }

        public int RingID
        {
            get {
                return ringID;
            }

            set {
                ringID = value;
            }
        }

        public RingFunctionType RingFunctionType
        {
            get {
                return ringFunctionType;
            }

            set {
                ringFunctionType = value;
            }
        }

        public bool IsAromaticRing
        {
            get {
                return isAromaticRing;
            }

            set {
                isAromaticRing = value;
            }
        }

        public bool IsSugarRing
        {
            get { return isSugarRing; }
            set { isSugarRing = value; }
        }

        public bool IsKetoneRing
        {
            get { return isKetoneRing; }
            set { isKetoneRing = value; }
        }

        public bool IsBenzeneRing
        {
            get { return isBenzeneRing; }
            set { isBenzeneRing = value; }
        }

        public List<AtomProperty> ConnectedAtoms
        {
            get {
                return connectedAtoms;
            }

            set {
                connectedAtoms = value;
            }
        }

        public List<BondProperty> ConnectedBonds
        {
            get {
                return connectedBonds;
            }

            set {
                connectedBonds = value;
            }
        }

        public bool IsEsterRing
        {
            get { return isEsterRing; }
            set { isEsterRing = value; }
        }

        public int RingsetID
        {
            get { return ringsetID; }
            set { ringsetID = value; }
        }
        #endregion
    }

    public class RingsetProperty
    {
        private IRingSet iRingSet;
        
        private int ringsetID;
        private List<int> ringIDs;
        private RingsetFunctionType ringsetFunctionType;

        private bool isAromaticRingset;
        private bool isHeteroRingset;


        private List<AtomProperty> connectedAtoms;
        private List<BondProperty> connectedBonds;

        public RingsetProperty()
        {
            connectedAtoms = new List<AtomProperty>();
            connectedBonds = new List<BondProperty>();
        }

        #region
        public IRingSet IRingSet
        {
            get { return iRingSet; }
            set { iRingSet = value; }
        }

        public int RingsetID
        {
            get { return ringsetID; }
            set { ringsetID = value; }
        }

        public List<int> RingIDs
        {
            get { return ringIDs; }
            set { ringIDs = value; }
        }

        public RingsetFunctionType RingsetFunctionType
        {
            get { return ringsetFunctionType; }
            set { ringsetFunctionType = value; }
        }

        public List<AtomProperty> ConnectedAtoms
        {
            get { return connectedAtoms; }
            set { connectedAtoms = value; }
        }

        public List<BondProperty> ConnectedBonds
        {
            get { return connectedBonds; }
            set { connectedBonds = value; }
        }

        public bool IsAromaticRingset
        {
            get { return isAromaticRingset; }
            set { isAromaticRingset = value; }
        }

        public bool IsHeteroRingset
        {
            get { return isHeteroRingset; }
            set { isHeteroRingset = value; }
        }
        #endregion
    }

    public class AtomEnvironmentProperty
    {
        private int carbonCount;
        private int hydrogenCount;
        private int nitrogenCount;
        private int oxygenCount;
        private int sulfurCount;
        private int phosphorusCount;
        private int fCount;
        private int clCount;
        private int brCount;
        private int iCount;
        private int siCount;
        private int hydroxyCount;
        private int primaryAmineCount;
        private int thiolCount;
        private int hydroxyAmineCount;

        private int etherCount;
        private int heteroEtherCount;

        private int aromaticCarbonCount;
        private int aromaticNitrogenCount;
        private int aromaticOxygenCount;
        private int aromaticSulfurCount;

        private int singleBond_CarbonCount;
        private int singleBond_HydrogenCount;
        private int singleBond_NitrogenCount;
        private int singleBond_OxygenCount;
        private int singleBond_SulfurCount;
        private int singleBond_PhosphorusCount;
        private int singleBond_FCount;
        private int singleBond_ClCount;
        private int singleBond_BrCount;
        private int singleBond_ICount;
        private int singleBond_SiCount;

        private int doubleBond_CC_Count;
        private int doubleBond_CN_Count;
        private int doubleBond_CO_Count;
        private int doubleBond_CS_Count;
        private int doubleBond_NN_Count;
        private int doubleBond_NO_Count;
        private int doubleBond_NP_Count;
        private int doubleBond_PO_Count;
        private int doubleBond_PP_Count;
        private int doubleBond_SO_Count;

        private int tripleBond_CC_Count;
        private int tripleBond_CN_Count;
      
        #region prop
        public int CarbonCount
        {
            get { return carbonCount; }
            set { carbonCount = value; }
        }

        public int HydrogenCount
        {
            get { return hydrogenCount; }
            set { hydrogenCount = value; }
        }

        public int NitrogenCount
        {
            get { return nitrogenCount; }
            set { nitrogenCount = value; }
        }

        public int OxygenCount
        {
            get { return oxygenCount; }
            set { oxygenCount = value; }
        }

        public int SulfurCount
        {
            get { return sulfurCount; }
            set { sulfurCount = value; }
        }

        public int PhosphorusCount
        {
            get { return phosphorusCount; }
            set { phosphorusCount = value; }
        }

        public int FCount
        {
            get { return fCount; }
            set { fCount = value; }
        }

        public int ClCount
        {
            get { return clCount; }
            set { clCount = value; }
        }

        public int BrCount
        {
            get { return brCount; }
            set { brCount = value; }
        }

        public int ICount
        {
            get { return iCount; }
            set { iCount = value; }
        }

        public int SiCount
        {
            get { return siCount; }
            set { siCount = value; }
        }

        public int HydroxyCount
        {
            get { return hydroxyCount; }
            set { hydroxyCount = value; }
        }

        public int PrimaryAmineCount
        {
            get { return primaryAmineCount; }
            set { primaryAmineCount = value; }
        }

        public int ThiolCount
        {
            get { return thiolCount; }
            set { thiolCount = value; }
        }

        public int EtherCount
        {
            get { return etherCount; }
            set { etherCount = value; }
        }

        public int HydroxyAmineCount
        {
            get { return hydroxyAmineCount; }
            set { hydroxyAmineCount = value; }
        }

        public int HeteroEtherCount
        {
            get { return heteroEtherCount; }
            set { heteroEtherCount = value; }
        }

        public int AromaticCarbonCount
        {
            get { return aromaticCarbonCount; }
            set { aromaticCarbonCount = value; }
        }

        public int AromaticNitrogenCount
        {
            get { return aromaticNitrogenCount; }
            set { aromaticNitrogenCount = value; }
        }

        public int AromaticOxygenCount
        {
            get { return aromaticOxygenCount; }
            set { aromaticOxygenCount = value; }
        }

        public int SingleBond_CarbonCount
        {
            get { return singleBond_CarbonCount; }
            set { singleBond_CarbonCount = value; }
        }

        public int SingleBond_HydrogenCount
        {
            get { return singleBond_HydrogenCount; }
            set { singleBond_HydrogenCount = value; }
        }

        public int SingleBond_NitrogenCount
        {
            get { return singleBond_NitrogenCount; }
            set { singleBond_NitrogenCount = value; }
        }

        public int SingleBond_OxygenCount
        {
            get { return singleBond_OxygenCount; }
            set { singleBond_OxygenCount = value; }
        }

        public int SingleBond_SulfurCount
        {
            get { return singleBond_SulfurCount; }
            set { singleBond_SulfurCount = value; }
        }

        public int SingleBond_PhosphorusCount
        {
            get { return singleBond_PhosphorusCount; }
            set { singleBond_PhosphorusCount = value; }
        }

        public int SingleBond_FCount
        {
            get { return singleBond_FCount; }
            set { singleBond_FCount = value; }
        }

        public int SingleBond_ClCount
        {
            get { return singleBond_ClCount; }
            set { singleBond_ClCount = value; }
        }

        public int SingleBond_BrCount
        {
            get { return singleBond_BrCount; }
            set { singleBond_BrCount = value; }
        }

        public int SingleBond_ICount
        {
            get { return singleBond_ICount; }
            set { singleBond_ICount = value; }
        }

        public int SingleBond_SiCount
        {
            get { return singleBond_SiCount; }
            set { singleBond_SiCount = value; }
        }

        public int AromaticSulfurCount
        {
            get { return aromaticSulfurCount; }
            set { aromaticSulfurCount = value; }
        }

        public int DoubleBond_CC_Count
        {
            get { return doubleBond_CC_Count; }
            set { doubleBond_CC_Count = value; }
        }

        public int DoubleBond_CN_Count
        {
            get { return doubleBond_CN_Count; }
            set { doubleBond_CN_Count = value; }
        }

        public int DoubleBond_CO_Count
        {
            get { return doubleBond_CO_Count; }
            set { doubleBond_CO_Count = value; }
        }

        public int DoubleBond_CS_Count
        {
            get { return doubleBond_CS_Count; }
            set { doubleBond_CS_Count = value; }
        }

        public int DoubleBond_NN_Count
        {
            get { return doubleBond_NN_Count; }
            set { doubleBond_NN_Count = value; }
        }

        public int DoubleBond_NO_Count
        {
            get { return doubleBond_NO_Count; }
            set { doubleBond_NO_Count = value; }
        }

        public int DoubleBond_NP_Count
        {
            get { return doubleBond_NP_Count; }
            set { doubleBond_NP_Count = value; }
        }
     
        public int DoubleBond_PO_Count
        {
            get { return doubleBond_PO_Count; }
            set { doubleBond_PO_Count = value; }
        }

        public int DoubleBond_PP_Count
        {
            get { return doubleBond_PP_Count; }
            set { doubleBond_PP_Count = value; }
        }

        public int DoubleBond_SO_Count
        {
            get { return doubleBond_SO_Count; }
            set { doubleBond_SO_Count = value; }
        }

        public int TripleBond_CC_Count
        {
            get { return tripleBond_CC_Count; }
            set { tripleBond_CC_Count = value; }
        }

        public int TripleBond_CN_Count
        {
            get { return tripleBond_CN_Count; }
            set { tripleBond_CN_Count = value; }
        }
        #endregion
    }

    public class BondBasicEnvironmentProperty
    {
        //in one path away
        private int first_CarbonCount;
        private int first_OxygenCount;
        private int first_NitrogenCount;
        private int first_SulfurCount;
        private int first_PhosphorusCount;
        private int first_HeteroatomCount;
        private int first_HalogenCount;
        private int first_SiliconCount;
        private int first_SinglebondCount;
        private int first_DoublebondCount;
        private int first_TriplebondCount;
        private int first_AromaticbondCount;

        //in two path away
        private int second_CarbonCount;
        private int second_OxygenCount;
        private int second_NitrogenCount;
        private int second_SulfurCount;
        private int second_PhosphorusCount;
        private int second_HeteroatomCount;
        private int second_HalogenCount;
        private int second_SiliconCount;
        private int second_SinglebondCount;
        private int second_DoublebondCount;
        private int second_TriplebondCount;
        private int second_AromaticbondCount;

        #region
        public int First_CarbonCount
        {
            get { return first_CarbonCount; }
            set { first_CarbonCount = value; }
        }

        public int First_OxygenCount
        {
            get { return first_OxygenCount; }
            set { first_OxygenCount = value; }
        }

        public int First_NitrogenCount
        {
            get { return first_NitrogenCount; }
            set { first_NitrogenCount = value; }
        }

        public int First_SulfurCount
        {
            get { return first_SulfurCount; }
            set { first_SulfurCount = value; }
        }

        public int First_PhosphorusCount
        {
            get { return first_PhosphorusCount; }
            set { first_PhosphorusCount = value; }
        }

        public int First_HeteroatomCount
        {
            get { return first_HeteroatomCount; }
            set { first_HeteroatomCount = value; }
        }

        public int First_DoublebondCount
        {
            get { return first_DoublebondCount; }
            set { first_DoublebondCount = value; }
        }

        public int First_TriplebondCount
        {
            get { return first_TriplebondCount; }
            set { first_TriplebondCount = value; }
        }

        public int First_SinglebondCount
        {
            get { return first_SinglebondCount; }
            set { first_SinglebondCount = value; }
        }
       
        public int Second_CarbonCount
        {
            get { return second_CarbonCount; }
            set { second_CarbonCount = value; }
        }

        public int Second_OxygenCount
        {
            get { return second_OxygenCount; }
            set { second_OxygenCount = value; }
        }

        public int Second_NitrogenCount
        {
            get { return second_NitrogenCount; }
            set { second_NitrogenCount = value; }
        }

        public int Second_SulfurCount
        {
            get { return second_SulfurCount; }
            set { second_SulfurCount = value; }
        }

        public int Second_PhosphorusCount
        {
            get { return second_PhosphorusCount; }
            set { second_PhosphorusCount = value; }
        }

        public int Second_HeteroatomCount
        {
            get { return second_HeteroatomCount; }
            set { second_HeteroatomCount = value; }
        }

        public int Second_DoublebondCount
        {
            get { return second_DoublebondCount; }
            set { second_DoublebondCount = value; }
        }

        public int Second_TriplebondCount
        {
            get { return second_TriplebondCount; }
            set { second_TriplebondCount = value; }
        }

        public int First_HalogenCount
        {
            get {
                return first_HalogenCount;
            }

            set {
                first_HalogenCount = value;
            }
        }

        public int First_SiliconCount
        {
            get {
                return first_SiliconCount;
            }

            set {
                first_SiliconCount = value;
            }
        }

        public int Second_HalogenCount
        {
            get {
                return second_HalogenCount;
            }

            set {
                second_HalogenCount = value;
            }
        }

        public int Second_SiliconCount
        {
            get {
                return second_SiliconCount;
            }

            set {
                second_SiliconCount = value;
            }
        }

        public int Second_SinglebondCount
        {
            get { return second_SinglebondCount; }
            set { second_SinglebondCount = value; }
        }

        public int First_AromaticbondCount
        {
            get { return first_AromaticbondCount; }
            set { first_AromaticbondCount = value; }
        }

        public int Second_AromaticbondCount
        {
            get { return second_AromaticbondCount; }
            set { second_AromaticbondCount = value; }
        }

        #endregion
    }

    /// <summary>
    /// Bond numbering: automatically determined
    /// Bond type: (1=single, 2=double, 3=triple, 4=aromatic)
    /// Bond direction: (0=none, 1=up, 2=down, 3=either) 
    /// </summary>
    public class BondProperty
    {
        //bond prop
        private IBond iBond;
        private int bondID;
        private BondType bondType;
        private BondDirection bondDirection;
        private BondBasicEnvironmentProperty bondEnv;
        private bool isAromaticity;

        //ring connections
        private bool isRingConnected;
        private bool isAromaticRingConnected;
        private bool isSugarRingConnected;
        private bool isHeteroRingConnected;
        private bool isBenzeneRingConnected;

		//in ring
		private int ringID;
        private bool isInRing;
        private bool isSharedBondInRings;
        private List<int> sharedRingIDs;

        private float cleavageLikelihood;
        private Dictionary<string, int> descriptor;

        private List<AtomProperty> connectedAtoms;

        public BondProperty()
        {
            connectedAtoms = new List<AtomProperty>();
            descriptor = new Dictionary<string,int>();
            sharedRingIDs = new List<int>();
            isAromaticity = false;
            isInRing = false;
            ringID = -1;
            bondEnv = new BondBasicEnvironmentProperty();
        }

        #region prop
        public IBond IBond
        {
            get { return iBond; }
            set { iBond = value; }
        }

        public int RingID
        {
            get { return ringID; }
            set { ringID = value; }
        }

        public bool IsAromaticity
        {
            get { return isAromaticity; }
            set { isAromaticity = value; }
        }

        public bool IsInRing
        {
            get { return isInRing; }
            set { isInRing = value; }
		}

        public int BondID
        {
            get { return bondID; }
            set { bondID = value; }
        }

		public BondType BondType
        {
            get { return bondType; }
            set { bondType = value; }
        }

        public BondDirection BondDirection
        {
            get { return bondDirection; }
            set { bondDirection = value; }
        }

        public BondBasicEnvironmentProperty BondEnv
        {
            get { return bondEnv; }
            set { bondEnv = value; }
        }

        public bool IsRingConnected
        {
            get { return isRingConnected; }
            set { isRingConnected = value; }
        }

        public bool IsAromaticRingConnected
        {
            get { return isAromaticRingConnected; }
            set { isAromaticRingConnected = value; }
        }

        public bool IsSugarRingConnected
        {
            get { return isSugarRingConnected; }
            set { isSugarRingConnected = value; }
        }

        public bool IsHeteroRingConnected
        {
            get { return isHeteroRingConnected; }
            set { isHeteroRingConnected = value; }
        }

        public bool IsBenzeneRingConnected
        {
            get { return isBenzeneRingConnected; }
            set { isBenzeneRingConnected = value; }
        }

        public float CleavageLikelihood
        {
            get { return cleavageLikelihood; }
            set { cleavageLikelihood = value; }
        }

        public List<AtomProperty> ConnectedAtoms
        {
            get { return connectedAtoms; }
            set { connectedAtoms = value; }
        }

        public Dictionary<string, int> Descriptor
        {
            get { return descriptor; }
            set { descriptor = value; }
        }

        public bool IsSharedBondInRings
        {
            get { return isSharedBondInRings; }
            set { isSharedBondInRings = value; }
        }

        public List<int> SharedRingIDs
        {
            get { return sharedRingIDs; }
            set { sharedRingIDs = value; }
        }
        #endregion
    }

    // cite http://cdk.github.io/cdk/1.5/docs/api/org/openscience/cdk/interfaces/IBond.Order.html#DOUBLE
    // cite https://en.wikipedia.org/wiki/Quintuple_bond
    public enum BondType
    {
        Single, Double, Triple, Quadruple, Quintuple, Sextuple, Unset
    }

    // cite http://cdk.github.io/cdk/1.5/docs/api/org/openscience/cdk/interfaces/IBond.Stereo.html
    public enum BondDirection
    {
        Down, DownInverted, E, EorZ, EZbyCoordinates, None, Up, UpInverted, UpOrDown, UpOrDownInverted, Z
    }

    public sealed class CdkConverter
    {
        private CdkConverter() { }

        public static BondType ConvertToCSharpBondType(BondOrder iOrder)
        {
            switch (iOrder) {
                case BondOrder.Single:
                    return BondType.Single;
                case BondOrder.Double:
                    return BondType.Double;
                case BondOrder.Triple:
                    return BondType.Triple;
                case BondOrder.Quadruple:
                    return BondType.Quadruple;
                default:
                    return BondType.Unset;
            }
        }

        public static BondDirection ConvertToCSharpBondDirection(BondStereo iStereo)
        {
            switch (iStereo) {
                case BondStereo.Down:
                    return BondDirection.Down;
                case BondStereo.DownInverted:
                    return BondDirection.DownInverted;
                case BondStereo.E:
                    return BondDirection.E;
                case BondStereo.EOrZ:
                    return BondDirection.EorZ;
                case BondStereo.EZByCoordinates:
                    return BondDirection.EZbyCoordinates;
                case BondStereo.None:
                    return BondDirection.None;
                case BondStereo.Up:
                    return BondDirection.Up;
                case BondStereo.UpInverted:
                    return BondDirection.UpInverted;
                case BondStereo.UpOrDown:
                    return BondDirection.UpOrDown;
                case BondStereo.UpOrDownInverted:
                    return BondDirection.UpOrDownInverted;
                case BondStereo.Z:
                    return BondDirection.Z;
                default: return BondDirection.None;
            }
        }
    }

    public sealed class StructureEnumConverter
    {
        private StructureEnumConverter() { }

        public static string BondTypeToString(BondType type, bool isAromaticBond = false)
        {
            if (isAromaticBond) return ":";
            var order = string.Empty;
            switch (type) {
                case BondType.Single: order = "-"; break;
                case BondType.Double: order = "="; break;
                case BondType.Triple: order = "#"; break;
                default: order = "n"; break;
            }
            return order;
        }
    }
}
