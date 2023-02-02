using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.DataObj
{
    internal class StandardCompoundSet
    {
        private readonly IReadOnlyList<StandardCompound> _standardCompounds;
        private readonly ILookup<string, StandardCompound> _stdCompoundsTable;
        private readonly StandardCompound _otherCompound;

        public StandardCompoundSet(IReadOnlyList<StandardCompound> standardCompounds) {
            _standardCompounds = standardCompounds ?? throw new ArgumentNullException(nameof(standardCompounds));
            _stdCompoundsTable = Compounds.Where(lipid => lipid.TargetClass != StandardCompound.AnyOthers).ToLookup(compound => compound.TargetClass);
            _otherCompound = Compounds.FirstOrDefault(lipid => lipid.TargetClass == StandardCompound.AnyOthers);
        }

        public IReadOnlyList<StandardCompound> Compounds => _standardCompounds;
        public ILookup<string, StandardCompound> StdCompoundsTable => _stdCompoundsTable;
        public StandardCompound OtherCompound => _otherCompound;
    }
}
