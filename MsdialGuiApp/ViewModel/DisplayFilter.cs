using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.ViewModel
{
    [Flags]
    enum DisplayFilter : uint
    {
        Unset = 0x0,
        RefMatched = 0x1,
        Suggested = 0x2,
        Unknown = 0x4,
        Ms2Acquired = 0x8,
        MolecularIon = 0x10,
        Blank = 0x20,
        UniqueIons = 0x40,
        CcsMatched = 0x80,

        Annotates = RefMatched | Suggested | Unknown | CcsMatched,
    }

    static class DisplayFilterExtention
    {
        public static bool Read(this DisplayFilter self, DisplayFilter flag) {
            return Any(self, flag);
        }

        public static void Write(this ref DisplayFilter self, DisplayFilter flag, bool set) {
            if (set)
                self |= flag;
            else
                self &= ~flag;
        }

        public static bool Any(this DisplayFilter self, DisplayFilter flag) {
            return (self & flag) != 0;
        }

        public static bool All(this DisplayFilter self, DisplayFilter flag) {
            return (self & flag) == flag;
        }
    }
}
