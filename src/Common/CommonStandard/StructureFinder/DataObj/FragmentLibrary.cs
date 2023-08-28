using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.StructureFinder.Property;

namespace CompMs.Common.StructureFinder.DataObj
{
    public class FragmentLibrary
    {
        private IonMode ionMode;
        private double fragmentMass;

        private Formula fragmentFormula;
        private string fragmentSmiles;
        private int id;
        private Structure fragmentStructure;

        public FragmentLibrary()
        {
        }

        public IonMode IonMode
        {
            get { return ionMode; }
            set { ionMode = value; }
        }

        public double FragmentMass
        {
            get { return fragmentMass; }
            set { fragmentMass = value; }
        }

        public Formula FragmentFormula
        {
            get { return fragmentFormula; }
            set { fragmentFormula = value; }
        }

        public string FragmentSmiles
        {
            get { return fragmentSmiles; }
            set { fragmentSmiles = value; }
        }

        public int Id
        {
            get { return id; }
            set { id = value; }
        }

        public Structure FragmentStructure
        {
            get
            {
                return fragmentStructure;
            }

            set
            {
                fragmentStructure = value;
            }
        }
    }
}
