using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NCDK.Sgroups
{
    /// <summary>
    /// Enumeration of Ctab Sgroup types.
    /// </summary>
    /// <remarks>
    /// <b>Display shortcuts</b>
    /// <list type="bullet">
    /// <item>SUP, abbreviation Sgroup (formerly called superatom)</item>
    /// <item>MUL, multiple group</item>
    /// <item>GEN, generic</item>
    /// </list>
    /// <b>Polymers</b>
    /// <list type="bullet">
    /// <item>SRU, SRU type</item>
    /// <item>MON, monomer</item>
    /// <item>MER, Mer type</item>
    /// <item>COP, copolymer</item>
    /// <item>CRO, crosslink</item>
    /// <item>MOD, modification</item>
    /// <item>GRA, graft</item>
    /// <item>ANY, any polymer</item> 
    /// </list>
    /// <b>Components, Mixtures, and formulations</b>
    /// <list type="bullet">
    /// <item>COM, component</item>
    /// <item>MIX, mixture</item>
    /// <item>FOR, formulation</item>
    /// </list>
    /// <b>Non-chemical</b>
    /// <list type="bullet">
    /// <item>DAT, data Sgroup</item>
    /// </list>
    /// </remarks>
    [Obfuscation(ApplyToMembers = true, Exclude = true)]
    public enum SgroupType
    {
        Nil = 0,

        // Display shortcuts

        /// <summary>
        /// SUP, abbreviation Sgroup (formerly called superatom)
        /// </summary>
        CtabAbbreviation,

        /// <summary>
        /// MUL, multiple group
        /// </summary>
        CtabMultipleGroup,

        /// <summary>
        /// GEN, generic
        /// </summary>
        CtabStructureRepeatUnit,

        // Polymers

        /// <summary>
        /// SRU, SRU type
        /// </summary>
        CtabMonomer,

        /// <summary>
        /// MON, monomer
        /// </summary>
        CtabModified,

        /// <summary>
        /// MER, Mer type
        /// </summary>
        CtabCopolymer,

        /// <summary>
        /// COP, copolymer
        /// </summary>
        CtabMer,

        /// <summary>
        /// CRO, crosslink
        /// </summary>
        CtabCrossLink,

        /// <summary>
        /// MOD, modification
        /// </summary>
        CtabGraft,

        /// <summary>
        /// GRA, graft
        /// </summary>
        CtabAnyPolymer,

        /// <summary>
        /// ANY, any polymer
        /// </summary>
        CtabComponent,

        // Components, Mixtures, and formulations

        /// <summary>
        /// COM, component
        /// </summary>
        CtabMixture,

        /// <summary>
        /// MIX, mixture
        /// </summary>
        CtabFormulation,

        /// <summary>
        /// FOR, formulation
        /// </summary>
        CtabData,

        // Non-chemical

        /// <summary>
        /// DAT, data Sgroup
        /// </summary>
        CtabGeneric,

        // extension for handling positional variation and distributed coordination bonds
        ExtMulticenter,
    }
}
