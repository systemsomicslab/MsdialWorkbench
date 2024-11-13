using NCDK;
using System.Collections.Generic;

namespace CompMs.Common.StructureFinder.Property
{
    /// <summary>
    /// Atom difference: +1 means 13C atom in carbon atom case.
    /// Atom charge: (0=none, and charge number is described by intuitive way)
    /// Atom numbering: automatically determined
    /// see also: http://cdk.github.io/cdk/1.5/docs/api/org/openscience/cdk/aromaticity/ElectronDonation.html
    /// </summary>
    public class AtomProperty
    {
        private IAtom iAtom;
        private AtomFunctionType atomFunctionType;

        private int atomID;
        private string atomString;
        private float atomMass;
        private int atomCharge;
        private int atomDifference;
        private int atomPriority;
        private int atomValence;
        private int atomIdInRing;

        private AtomEnvironmentProperty atomEnv;

        //ring prop
        private bool isInRing;
        private int ringID;
        private bool isSharedAtomInRings;
        private List<int> sharedRingIDs;

        private List<BondProperty> connectedBonds;

        public AtomProperty()
        {
            atomFunctionType = Property.AtomFunctionType.Other;
            connectedBonds = new List<BondProperty>();
            atomEnv = new AtomEnvironmentProperty();
            sharedRingIDs = new List<int>();
            ringID = -1;
            atomIdInRing = -1;
        }

        #region prop
        public IAtom IAtom
        {
            get { return iAtom; }
            set { iAtom = value; }
        }

        public int AtomID
        {
            get { return atomID; }
            set { atomID = value; }
        }

        public int RingID
        {
            get { return ringID; }
            set { ringID = value; }
        }

        public int AtomPriority
        {
            get { return atomPriority; }
            set { atomPriority = value; }
        }

        public string AtomString
        {
            get { return atomString; }
            set { atomString = value; }
        }

        public float AtomMass
        {
            get { return atomMass; }
            set { atomMass = value; }
        }

        public int AtomValence
        {
            get { return atomValence; }
            set { atomValence = value; }
        }

        public int AtomCharge
        {
            get { return atomCharge; }
            set { atomCharge = value; }
        }

        public int AtomDifference
        {
            get { return atomDifference; }
            set { atomDifference = value; }
        }

        public int AtomIdInRing
        {
            get { return atomIdInRing; }
            set { atomIdInRing = value; }
        }

        public List<BondProperty> ConnectedBonds
        {
            get { return connectedBonds; }
            set { connectedBonds = value; }
        }

        public AtomEnvironmentProperty AtomEnv
        {
            get { return atomEnv; }
            set { atomEnv = value; }
        }

        public bool IsSharedAtomInRings
        {
            get { return isSharedAtomInRings; }
            set { isSharedAtomInRings = value; }
        }

        public List<int> SharedRingIDs
        {
            get { return sharedRingIDs; }
            set { sharedRingIDs = value; }
        }

        public bool IsInRing
        {
            get { return isInRing; }
            set { isInRing = value; }
        }

        public AtomFunctionType AtomFunctionType
        {
            get
            {
                return atomFunctionType;
            }

            set
            {
                atomFunctionType = value;
            }
        }
        #endregion
    }
}
