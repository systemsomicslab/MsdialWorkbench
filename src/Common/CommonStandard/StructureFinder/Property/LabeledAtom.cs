namespace CompMs.Common.StructureFinder.Property
{
    public class LabeledAtom
    {
        private string atomString;
        private double replacedMass;

        public string AtomString
        {
            get { return atomString; }
            set { atomString = value; }
        }

        public double ReplacedMass
        {
            get { return replacedMass; }
            set { replacedMass = value; }
        }
    }
}
