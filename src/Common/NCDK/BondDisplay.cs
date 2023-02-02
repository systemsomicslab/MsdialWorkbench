namespace NCDK
{
    /// <summary>
    /// Bond display style, controlling how bonds appear in a 2D depiction.
    /// </summary>
    public enum BondDisplay
    {
        /// <summary>A solid line (default).</summary>
        Solid = 0,
        /// <summary>A dashed line.</summary>
        Dash = 1,
        /// <summary>A hashed line (bold dashed).</summary>
        Hash = 2,
        /// <summary>A bold line.</summary>
        Bold = 3,
        /// <summary>A wavy line.</summary>
        Wavy = 4,
        /// <summary>A dotted line.</summary>
        Dot = 5,
        /// <summary>
        /// Display as a hashed wedge, with the narrow end
        /// towards the begin atom of the bond (<see cref="IBond.Begin"/>).
        /// </summary>
        WedgedHashBegin = 6,
        /// <summary>
        /// Display as a hashed wedge, with the narrow end
        /// towards the end atom of the bond (<see cref="IBond.End"/>).
        /// </summary>
        WedgedHashEnd = 7,
        /// <summary>
        /// Display as a bold wedge, with the narrow end
        /// towards the begin atom of the bond (<see cref="IBond.Begin"/>).
        /// </summary>
        WedgeBegin = 8,
        /// <summary>
        /// Display as a bold wedge, with the narrow end
        /// towards the end atom of the bond (<see cref="IBond.End"/>).
        /// </summary>
        WedgeEnd = 9,
        /// <summary>
        /// Display as an arrow (e.g. co-ordination bond), the arrow points
        /// to the begin (<see cref="IBond.Begin"/>) atom.
        /// </summary>
        ArrowBegin = 10,
        /// <summary>
        /// Display as an arrow (e.g. co-ordination bond), the arrow points
        /// to the end (<see cref="IBond.End"/>) atom.
        /// </summary>
        ArrowEnd = 11,
    }
}
