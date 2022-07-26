using System;

namespace CompMs.App.Msdial.Model.Search
{
    [Flags]
    public enum DisplayFilter : uint
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
        ManuallyModified = 0x100,
        MscleanrBlank = 0x200,
        MscleanrBlankGhost = 0x400,
        MscleanrIncorrectMass = 0x800,
        MscleanrRsd = 0x1000,
        MscleanrRmd = 0x2000,

        Annotates = RefMatched | Suggested | Unknown | CcsMatched | ManuallyModified,
        All = RefMatched | Suggested | Unknown | Ms2Acquired | MolecularIon | Blank | UniqueIons | CcsMatched | ManuallyModified | MscleanrBlank | MscleanrBlankGhost | MscleanrIncorrectMass | MscleanrRsd | MscleanrRmd,
    }

    static class DisplayFilterExtention
    {
        public static bool Read(this DisplayFilter self, DisplayFilter flag) {
            return self.Any(flag);
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
