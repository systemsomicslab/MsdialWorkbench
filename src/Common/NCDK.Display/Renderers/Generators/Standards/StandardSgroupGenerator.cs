/*
 * Copyright (c) 2015 John May <jwmay@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or modify it
 * under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation; either version 2.1 of the License, or (at
 * your option) any later version. All we ask is that proper credit is given
 * for our work, which includes - but is not limited to - adding the above
 * copyright notice to the beginning of your source code files, and to any
 * copyright notice that you may distribute with programs based on this work.
 *
 * This program is distributed in the hope that it will be useful, but WITHOUT
 * ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Lesser General Public
 * License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 U
 */

using NCDK.Common.Collections;
using NCDK.Common.Mathematics;
using NCDK.Geometries;
using NCDK.Numerics;
using NCDK.Renderers.Elements;
using NCDK.Sgroups;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace NCDK.Renderers.Generators.Standards
{
    /// <summary>
    /// Internal class, call exclusively from StandardGenerator - separate purely for code organisation only.
    /// </summary>
    sealed class StandardSgroupGenerator
    {
        public const double EQUIV_THRESHOLD = 0.1;
        public const char INTERPUNCT = '·';
        private readonly double stroke;
        private readonly double scale;
        private readonly double bracketDepth;
        private readonly Typeface font;
        private readonly double emSize;
        private readonly Color foreground;
        private readonly double labelScale;
        private readonly StandardAtomGenerator atomGenerator;
        private readonly RendererModel parameters;

        private StandardSgroupGenerator(RendererModel parameters, StandardAtomGenerator atomGenerator, double stroke, Typeface font, double emSize, Color foreground)
        {
            this.font = font;
            this.emSize = emSize;
            this.scale = parameters.GetScale();
            this.stroke = stroke;
            double length = parameters.GetBondLength() / scale;
            this.bracketDepth = parameters.GetSgroupBracketDepth() * length;
            this.labelScale = parameters.GetSgroupFontScale();

            // foreground is based on the carbon color
            this.foreground = foreground;
            this.atomGenerator = atomGenerator;
            this.parameters = parameters;
        }

        public static IRenderingElement Generate(RendererModel parameters, double stroke, Typeface font, double emSize, Color foreground, StandardAtomGenerator atomGenerator, AtomSymbol[] symbols, IAtomContainer container)
        {
            return new StandardSgroupGenerator(parameters, atomGenerator, stroke, font, emSize, foreground).GenerateSgroups(container, symbols);
        }

        /// <summary>
        /// If the molecule has display shortcuts (abbreviations or multiple group sgroups) certain parts
        /// of the structure are hidden from display. This method marks the parts to hide and in the case
        /// of abbreviations, remaps atom symbols. Apart from additional property flags, the molecule
        /// is unchanged by this method.
        /// </summary>
        /// <param name="container">molecule input</param>
        /// <param name="symbolRemap">a map that will hold symbol remapping</param>
        public static void PrepareDisplayShortcuts(IAtomContainer container, IDictionary<IAtom, string> symbolRemap)
        {
            var sgroups = container.GetCtabSgroups();
            if (sgroups == null || !sgroups.Any())
                return;

            // select abbreviations that should be contracted
            foreach (var sgroup in sgroups)
            {
                if (sgroup.Type == SgroupType.CtabAbbreviation)
                {
                    bool? expansion = (bool?)sgroup.GetValue(SgroupKey.CtabExpansion);
                    // abbreviation is displayed as expanded
                    if (expansion ?? false)
                        continue;
                    // no or empty label, skip it
                    if (string.IsNullOrEmpty(sgroup.Subscript))
                        continue;

                    // only contract if the atoms are either partially or fully highlighted
                    if (CheckAbbreviationHighlight(container, sgroup))
                        ContractAbbreviation(container, symbolRemap, sgroup);
                }
                else if (sgroup.Type == SgroupType.CtabMultipleGroup)
                {
                    HideMultipleParts(container, sgroup);
                }
                else if (sgroup.Type == SgroupType.ExtMulticenter)
                {
                    var atoms = sgroup.Atoms;
                    // should only be one bond
                    foreach (var bond in sgroup.Bonds)
                    {
                        var beg = bond.Begin;
                        var end = bond.End;
                        if (atoms.Contains(beg))
                        {
                            StandardGenerator.HideFully(beg);
                        }
                        else
                        {
                            StandardGenerator.HideFully(end);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Checks whether the given abbreviation Sgroup either has no highlight or is fully highlighted. If an
        /// abbreviation is partially highlighted we don't want to contract it as this would hide the part
        /// being highlighted.
        /// </summary>
        /// <param name="container">molecule</param>
        /// <param name="sgroup">abbreviation Sgroup</param>
        /// <returns>the abbreviation can be contracted</returns>
        private static bool CheckAbbreviationHighlight(IAtomContainer container, Sgroup sgroup)
        {
            Debug.Assert(sgroup.Type == SgroupType.CtabAbbreviation);

            var sgroupAtoms = sgroup.Atoms;
            int atomHighlight = 0;
            int bondHighlight = 0;
            int numSgroupAtoms = sgroupAtoms.Count;
            int numSgroupBonds = 0;

            Color? color = null;
            Color? refcolor = null;

            foreach (var atom in sgroupAtoms)
            {
                if ((color = atom.GetProperty<Color?>(StandardGenerator.HighlightColorKey)) != null)
                {
                    atomHighlight++;
                    if (refcolor == null)
                        refcolor = color;
                    else if (!color.Equals(refcolor))
                        return false; // multi-colored
                }
                else if (atomHighlight != 0)
                {
                    return false; // fail fast
                }
            }
            foreach (var bond in container.Bonds)
            {
                var beg = bond.Begin;
                var end = bond.End;
                if (sgroupAtoms.Contains(beg) && sgroupAtoms.Contains(end))
                {
                    numSgroupBonds++;
                    if ((color = bond.GetProperty<Color?>(StandardGenerator.HighlightColorKey)) != null)
                    {
                        bondHighlight++;
                        if (refcolor == null)
                            refcolor = color;
                        else if (!color.Equals(refcolor))
                            return false; // multi-colored
                    }
                    else if (bondHighlight != 0)
                    {
                        return false; // fail fast
                    }
                }
            }
            return atomHighlight + bondHighlight == 0 || (atomHighlight == numSgroupAtoms &&
                                                          bondHighlight == numSgroupBonds);
        }

        /// <summary>
        /// Hide the repeated atoms and bonds of a multiple group. We hide al atoms that
        /// belong to the group unless they are defined in the parent atom list. Any
        /// bond to those atoms that is not a crossing bond or one connecting atoms in
        /// the parent list is hidden.
        /// </summary>
        /// <param name="container">molecule</param>
        /// <param name="sgroup">multiple group display shortcut</param>
        private static void HideMultipleParts(IAtomContainer container, Sgroup sgroup)
        {
            var crossing = sgroup.Bonds;
            var atoms = sgroup.Atoms;
            var parentAtoms = (ICollection<IAtom>)sgroup.GetValue(SgroupKey.CtabParentAtomList);

            foreach (var bond in container.Bonds)
            {
                if (parentAtoms.Contains(bond.Begin) && parentAtoms.Contains(bond.End))
                    continue;
                if (atoms.Contains(bond.Begin) || atoms.Contains(bond.End))
                    StandardGenerator.Hide(bond);
            }
            foreach (var atom in atoms)
            {
                if (!parentAtoms.Contains(atom))
                    StandardGenerator.Hide(atom);
            }
            foreach (var bond in crossing)
            {
                StandardGenerator.Unhide(bond);
            }
        }

        /// <summary>
        /// Hide the atoms and bonds of a contracted abbreviation. If the abbreviations is attached
        /// we remap the attachment symbol to display the name. If there are no attachments the symbol
        /// we be added later ({@see #generateSgroups}).
        /// </summary>
        /// <param name="container">molecule</param>
        /// <param name="sgroup">abbreviation group display shortcut</param>
        private static void ContractAbbreviation(IAtomContainer container, IDictionary<IAtom, string> symbolRemap, Sgroup sgroup)
        {
            var crossing = sgroup.Bonds;
            var atoms = sgroup.Atoms;

            // only do 0,1 attachments for now
            if (crossing.Count > 1)
                return;

            foreach (var atom in atoms)
            {
                StandardGenerator.Hide(atom);
            }
            foreach (var bond in container.Bonds)
            {
                if (atoms.Contains(bond.Begin) ||
                    atoms.Contains(bond.End))
                    StandardGenerator.Hide(bond);
            }
            foreach (var bond in crossing)
            {
                StandardGenerator.Unhide(bond);
                var a1 = bond.Begin;
                var a2 = bond.End;
                StandardGenerator.Unhide(a1);
                if (atoms.Contains(a1))
                    symbolRemap[a1] = sgroup.Subscript;
                StandardGenerator.Unhide(a2);
                if (atoms.Contains(a2))
                    symbolRemap[a2] = sgroup.Subscript;
            }
        }

        /// <summary>
        /// Generate the Sgroup elements for the provided atom contains.
        /// </summary>
        /// <param name="container">molecule</param>
        /// <returns>Sgroup rendering elements</returns>
        IRenderingElement GenerateSgroups(IAtomContainer container, AtomSymbol[] symbols)
        {
            var result = new ElementGroup();
            var sgroups = container.GetCtabSgroups();

            if (sgroups == null || !sgroups.Any())
                return result;

            var symbolMap = new Dictionary<IAtom, AtomSymbol>();
            for (int i = 0; i < symbols.Length; i++)
            {
                if (symbols[i] != null)
                    symbolMap[container.Atoms[i]] = symbols[i];
            }

            foreach (var sgroup in sgroups)
            {
                switch (sgroup.Type)
                {
                    case SgroupType.CtabAbbreviation:
                        result.Add(GenerateAbbreviationSgroup(container, sgroup));
                        break;
                    case SgroupType.CtabMultipleGroup:
                        result.Add(GenerateMultipleSgroup(sgroup));
                        break;
                    case SgroupType.CtabAnyPolymer:
                    case SgroupType.CtabMonomer:
                    case SgroupType.CtabCrossLink:
                    case SgroupType.CtabCopolymer:
                    case SgroupType.CtabStructureRepeatUnit:
                    case SgroupType.CtabMer:
                    case SgroupType.CtabGraft:
                    case SgroupType.CtabModified:
                        result.Add(GeneratePolymerSgroup(sgroup, symbolMap));
                        break;
                    case SgroupType.CtabComponent:
                    case SgroupType.CtabMixture:
                    case SgroupType.CtabFormulation:
                        result.Add(GenerateMixtureSgroup(sgroup));
                        break;
                    case SgroupType.CtabGeneric:
                        // not strictly a polymer but okay to draw as one
                        result.Add(GeneratePolymerSgroup(sgroup, null));
                        break;
                }
            }

            return result;
        }

        private IRenderingElement GenerateMultipleSgroup(Sgroup sgroup)
        {
            // just draw the brackets - multiplied group parts have already been hidden in prep phase
            var brackets = (IList<SgroupBracket>)sgroup.GetValue(SgroupKey.CtabBracket);
            if (brackets != null)
            {
                return GenerateSgroupBrackets(sgroup,
                                              brackets,
                                              Dictionaries.Empty<IAtom, AtomSymbol>(),
                                              (string)sgroup.GetValue(SgroupKey.CtabSubScript),
                                              null);
            }
            else
            {
                return new ElementGroup();
            }
        }

        private IRenderingElement GenerateAbbreviationSgroup(IAtomContainer mol, Sgroup sgroup)
        {
            string label = sgroup.Subscript;
            // already handled by symbol remapping
            if (sgroup.Bonds.Count > 0 || string.IsNullOrEmpty(label))
            {
                return new ElementGroup();
            }
            if (!CheckAbbreviationHighlight(mol, sgroup))
                return new ElementGroup();
            // we're showing a label where there were no atoms before, we put it in the
            // middle of all of those which were hidden
            var sgroupAtoms = sgroup.Atoms;
            Debug.Assert(sgroupAtoms.Any());

            var highlight = sgroupAtoms.First().GetProperty<Color>(StandardGenerator.HighlightColorKey);
            var style = parameters.GetHighlighting();
            var glowWidth = parameters.GetOuterGlowWidth();

            Vector2 labelLocation;
            if (mol.Atoms.Count == sgroup.Atoms.Count)
            {
                labelLocation = GeometryUtil.Get2DCenter(sgroupAtoms);
            }
            else
            {
                // contraction of part of a fragment, e.g. SALT
                // here we work out the point we want to place the contract relative
                // to the SGroup Atoms
                labelLocation = new Vector2();
                var sgrpCenter = GeometryUtil.Get2DCenter(sgroupAtoms);
                var molCenter = GeometryUtil.Get2DCenter(mol);
                var minMax = GeometryUtil.GetMinMax(sgroupAtoms);
                var xDiff = sgrpCenter.X - molCenter.X;
                var yDiff = sgrpCenter.Y - molCenter.Y;
                if (xDiff > 0.1)
                {
                    labelLocation.X = minMax[0]; // min x
                    label = INTERPUNCT + label;
                }
                else if (xDiff < -0.1)
                {
                    labelLocation.X = minMax[2]; // max x
                    label = label + INTERPUNCT;
                }
                else
                {
                    labelLocation.X = sgrpCenter.X;
                    label = INTERPUNCT + label;
                }
                if (yDiff > 0.1)
                    labelLocation.Y = minMax[1]; // min y
                else if (yDiff < -0.1)
                    labelLocation.Y = minMax[3]; // max y
                else
                    labelLocation.Y = sgrpCenter.Y;
            }

            var labelgroup = new ElementGroup();
            foreach (var outline in atomGenerator.GenerateAbbreviatedSymbol(label, HydrogenPosition.Right)
                                                 .Center(labelLocation.X, labelLocation.Y)
                                                 .Resize(1 / scale, 1 / -scale)
                                                 .GetOutlines())
            {
                if (highlight != null && style == HighlightStyle.Colored)
                {
                    labelgroup.Add(GeneralPath.ShapeOf(outline, highlight));
                }
                else
                {
                    labelgroup.Add(GeneralPath.ShapeOf(outline, foreground));
                }
            }

            if (highlight != null && style == HighlightStyle.OuterGlow)
            {
                var group = new ElementGroup
                {
                    // outer glow needs to be being the label
                    StandardGenerator.OuterGlow(labelgroup, highlight, glowWidth, stroke),
                    labelgroup
                };
                return group;
            }
            else
            {
                return MarkedElement.MarkupAtom(labelgroup, null);
            }
        }

        /// <summary>
        /// Generates polymer Sgroup elements.
        /// </summary>
        /// <param name="sgroup">the Sgroup</param>
        /// <returns>the rendered elements (empty if no brackets defined)</returns>
        private IRenderingElement GeneratePolymerSgroup(Sgroup sgroup, IReadOnlyDictionary<IAtom, AtomSymbol> symbolMap)
        {
            // draw the brackets
            var brackets = (IList<SgroupBracket>)sgroup.GetValue(SgroupKey.CtabBracket);
            if (brackets != null)
            {
                var type = sgroup.Type;

                var subscript = (string)sgroup.GetValue(SgroupKey.CtabSubScript);
                var connectivity = (string)sgroup.GetValue(SgroupKey.CtabConnectivity);

                switch (type)
                {
                    case SgroupType.CtabCopolymer:
                        subscript = "co";
                        string subtype = (string)sgroup.GetValue(SgroupKey.CtabSubType);
                        if (string.Equals("RAN", subtype, StringComparison.Ordinal))
                            subscript = "ran";
                        else if (string.Equals("BLK", subtype, StringComparison.Ordinal))
                            subscript = "blk";
                        else if (string.Equals("ALT", subtype, StringComparison.Ordinal))
                            subscript = "alt";
                        break;
                    case SgroupType.CtabCrossLink:
                        subscript = "xl";
                        break;
                    case SgroupType.CtabAnyPolymer:
                        subscript = "any";
                        break;
                    case SgroupType.CtabGraft:
                        subscript = "grf";
                        break;
                    case SgroupType.CtabMer:
                        subscript = "mer";
                        break;
                    case SgroupType.CtabMonomer:
                        subscript = "mon";
                        break;
                    case SgroupType.CtabModified:
                        subscript = "mod";
                        break;
                    case SgroupType.CtabStructureRepeatUnit:
                        if (subscript == null)
                            subscript = "n";
                        if (connectivity == null)
                            connectivity = "eu";
                        break;
                }

                // connectivity doesn't matter if symmetric... which is hard to test
                // here but we can certainly ignore it for single atoms (e.g. methylene)
                // also when we see brackets we presume head-to-tail repeating
                if ("ht".Equals(connectivity) || sgroup.Atoms.Count == 1)
                    connectivity = null;

                return GenerateSgroupBrackets(sgroup,
                                              brackets,
                                              symbolMap,
                                              subscript,
                                              connectivity);
            }
            else
            {
                return new ElementGroup();
            }
        }

        private IRenderingElement GenerateMixtureSgroup(Sgroup sgroup)
        {
            // draw the brackets
            // TODO - mixtures normally have attached Sgroup data
            // TODO - e.g. COMPONENT_FRACTION, ACTIVITY_TYPE, WEIGHT_PERCENT
            var brackets = (IList<SgroupBracket>)sgroup.GetValue(SgroupKey.CtabBracket);
            if (brackets != null)
            {
                var type = sgroup.Type;
                string subscript = "?";
                switch (type)
                {
                    case SgroupType.CtabComponent:
                        var compNum = (int?)sgroup.GetValue(SgroupKey.CtabComponentNumber);
                        if (compNum != null)
                            subscript = "c" + compNum.ToString();
                        else
                            subscript = "c";
                        break;
                    case SgroupType.CtabMixture:
                        subscript = "mix";
                        break;
                    case SgroupType.CtabFormulation:
                        subscript = "f";
                        break;
                }

                return GenerateSgroupBrackets(sgroup,
                                              brackets,
                                              null,
                                              subscript,
                                              null);
            }
            else
            {
                return new ElementGroup();
            }
        }

        private static bool IsDigit(char c)
        {
            return c >= '0' && c <= '9';
        }

        private static bool IsUnsignedInt(string str)
        {
            int pos = 0;
            if (str == null)
                return false;
            int len = str.Length;
            while (pos < len)
                if (!IsDigit(str[pos++]))
                    return false;
            return true;
        }

        private IRenderingElement GenerateSgroupBrackets(Sgroup sgroup,
                                                         IList<SgroupBracket> brackets,
                                                         IReadOnlyDictionary<IAtom, AtomSymbol> symbols,
                                                         string subscriptSuffix,
                                                         string superscriptSuffix)
        {
            // brackets are square by default (style:0)
            var style = (int?)sgroup.GetValue(SgroupKey.CtabBracketStyle);
            bool round = style != null && style == 1;
            var result = new ElementGroup();

            var atoms = sgroup.Atoms;
            var crossingBonds = sgroup.Bonds;

            // easy to depict in correct orientation, we just
            // point each bracket at the atom of a crossing
            // bond that is 'in' the group - this scales
            // to more than two brackets

            // first we need to pair the brackets with the bonds
            var pairs = crossingBonds.Count == brackets.Count ? BracketBondPairs(brackets, crossingBonds) : Dictionaries.Empty<SgroupBracket, IBond>();

            // override bracket layout around single atoms to bring them in closer
            if (atoms.Count == 1)
            {
                var atom = atoms.First();

                // e.g. 2 HCL, 8 H2O etc.
                if (IsUnsignedInt(subscriptSuffix) &&
                    !crossingBonds.Any() &&
                    symbols.ContainsKey(atom))
                {
                    var prefix = new TextOutline('·' + subscriptSuffix, font, emSize).Resize(1 / scale, 1 / -scale);
                    var prefixBounds = prefix.LogicalBounds;

                    var symbol = symbols[atom];

                    var bounds = symbol.GetConvexHull().Outline.Bounds;

                    // make slightly large
                    bounds = new Rect(bounds.Bottom - 2 * stroke,
                                      bounds.Left - 2 * stroke,
                                      bounds.Width + 4 * stroke,
                                      bounds.Height + 4 * stroke);

                    prefix = prefix.Translate(bounds.Bottom - prefixBounds.Top,
                                              symbol.GetAlignmentCenter().Y - prefixBounds.CenterY());

                    result.Add(GeneralPath.ShapeOf(prefix.GetOutline(), foreground));
                }
                // e.g. CC(O)nCC
                else if (crossingBonds.Count > 0)
                {
                    var scriptscale = labelScale;

                    var leftBracket = new TextOutline("(", font, emSize).Resize(1 / scale, 1 / -scale);
                    var rightBracket = new TextOutline(")", font, emSize).Resize(1 / scale, 1 / -scale);

                    var leftCenter = leftBracket.GetCenter();
                    var rightCenter = rightBracket.GetCenter();

                    if (symbols.ContainsKey(atom))
                    {
                        var symbol = symbols[atom];

                        var bounds = symbol.GetConvexHull().Outline.Bounds;
                        // make slightly large
                        bounds = new Rect(bounds.Left - 2 * stroke,
                                          bounds.Top - 2 * stroke,
                                          bounds.Width + 4 * stroke,
                                          bounds.Height + 4 * stroke);

                        leftBracket = leftBracket.Translate(bounds.Left - 0.1 - leftCenter.X,
                                                            symbol.GetAlignmentCenter().Y - leftCenter.Y);
                        rightBracket = rightBracket.Translate(bounds.Right + 0.1 - rightCenter.X,
                                                              symbol.GetAlignmentCenter().Y - rightCenter.Y);
                    }
                    else
                    {
                        var p = atoms.First().Point2D.Value;
                        leftBracket = leftBracket.Translate(p.X - 0.2 - leftCenter.X, p.Y - leftCenter.Y);
                        rightBracket = rightBracket.Translate(p.X + 0.2 - rightCenter.X, p.Y - rightCenter.Y);
                    }

                    result.Add(GeneralPath.ShapeOf(leftBracket.GetOutline(), foreground));
                    result.Add(GeneralPath.ShapeOf(rightBracket.GetOutline(), foreground));

                    var rightBracketBounds = rightBracket.GetBounds();

                    // subscript/superscript suffix annotation
                    if (subscriptSuffix != null && subscriptSuffix.Any())
                    {
                        TextOutline subscriptOutline = LeftAlign(MakeText(subscriptSuffix.ToLowerInvariant(),
                                                                          new Vector2(rightBracketBounds.Right,
                                                                                      rightBracketBounds.Top - 0.1),
                                                                          new Vector2(-0.5 * rightBracketBounds.Width, 0),
                                                                          scriptscale));
                        result.Add(GeneralPath.ShapeOf(subscriptOutline.GetOutline(), foreground));
                    }
                    if (superscriptSuffix != null && superscriptSuffix.Any())
                    {
                        var superscriptOutline = LeftAlign(MakeText(superscriptSuffix.ToLowerInvariant(),
                                                                    new Vector2(rightBracketBounds.Right,
                                                                                rightBracketBounds.Bottom + 0.1),
                                                                    new Vector2(-rightBracketBounds.Width, 0),
                                                                    scriptscale));
                        result.Add(GeneralPath.ShapeOf(superscriptOutline.GetOutline(), foreground));
                    }
                }
            }
            else if (pairs.Any())
            {
                SgroupBracket suffixBracket = null;
                Vector2? suffixBracketPerp = null;

                foreach (var e in pairs)
                {
                    var bracket = e.Key;
                    var bond = e.Value;
                    var inGroupAtom = atoms.Contains(bond.Begin) ? bond.Begin : bond.End;

                    var p1 = bracket.FirstPoint;
                    var p2 = bracket.SecondPoint;

                    var perp = VecmathUtil.NewPerpendicularVector(VecmathUtil.NewUnitVector(p1, p2));

                    // point the vector at the atom group
                    var midpoint = VecmathUtil.Midpoint(p1, p2);
                    if (Vector2.Dot(perp, VecmathUtil.NewUnitVector(midpoint, inGroupAtom.Point2D.Value)) < 0)
                    {
                        perp = Vector2.Negate(perp);
                    }
                    perp *= bracketDepth;

                    if (round)
                        result.Add(CreateRoundBracket(p1, p2, perp, midpoint));
                    else
                        result.Add(CreateSquareBracket(p1, p2, perp));

                    if (suffixBracket == null)
                    {
                        suffixBracket = bracket;
                        suffixBracketPerp = perp;
                    }
                    else
                    {
                        // is this bracket better as a suffix?
                        var sp1 = suffixBracket.FirstPoint;
                        var sp2 = suffixBracket.SecondPoint;
                        var bestMaxX = Math.Max(sp1.X, sp2.X);
                        var thisMaxX = Math.Max(p1.X, p2.X);
                        var bestMaxY = Math.Max(sp1.Y, sp2.Y);
                        var thisMaxY = Math.Max(p1.Y, p2.Y);

                        // choose the most eastern or.. the most southern
                        var xDiff = thisMaxX - bestMaxX;
                        var yDiff = thisMaxY - bestMaxY;
                        if (xDiff > EQUIV_THRESHOLD || (xDiff > -EQUIV_THRESHOLD && yDiff < -EQUIV_THRESHOLD))
                        {
                            suffixBracket = bracket;
                            suffixBracketPerp = perp;
                        }
                    }
                }

                // write the labels
                if (suffixBracket != null)
                {
                    var subSufPnt = suffixBracket.FirstPoint;
                    var supSufPnt = suffixBracket.SecondPoint;

                    // try to put the subscript on the bottom
                    var xDiff = subSufPnt.X - supSufPnt.X;
                    var yDiff = subSufPnt.Y - supSufPnt.Y;
                    if (yDiff > EQUIV_THRESHOLD || (yDiff > -EQUIV_THRESHOLD && xDiff > EQUIV_THRESHOLD))
                    {
                        var tmpP = subSufPnt;
                        subSufPnt = supSufPnt;
                        supSufPnt = tmpP;
                    }

                    // subscript/superscript suffix annotation
                    if (subscriptSuffix != null && subscriptSuffix.Any())
                    {
                        var subscriptOutline = LeftAlign(MakeText(subscriptSuffix.ToLowerInvariant(), subSufPnt, suffixBracketPerp.Value, labelScale));
                        result.Add(GeneralPath.ShapeOf(subscriptOutline.GetOutline(), foreground));
                    }
                    if (superscriptSuffix != null && superscriptSuffix.Any())
                    {
                        var superscriptOutline = LeftAlign(MakeText(superscriptSuffix.ToLowerInvariant(), supSufPnt, suffixBracketPerp.Value, labelScale));
                        result.Add(GeneralPath.ShapeOf(superscriptOutline.GetOutline(), foreground));
                    }
                }
            }
            else if (brackets.Count == 2)
            {
                var b1p1 = brackets[0].FirstPoint;
                var b1p2 = brackets[0].SecondPoint;
                var b2p1 = brackets[1].FirstPoint;
                var b2p2 = brackets[1].SecondPoint;

                var b1vec = VecmathUtil.NewUnitVector(b1p1, b1p2);
                var b2vec = VecmathUtil.NewUnitVector(b2p1, b2p2);

                var b1pvec = VecmathUtil.NewPerpendicularVector(b1vec);
                var b2pvec = VecmathUtil.NewPerpendicularVector(b2vec);

                // Point the vectors at each other
                if (Vector2.Dot(b1pvec, VecmathUtil.NewUnitVector(b1p1, b2p1)) < 0)
                    b1pvec = Vector2.Negate(b1pvec);
                if (Vector2.Dot(b2pvec, VecmathUtil.NewUnitVector(b2p1, b1p1)) < 0)
                    b2pvec = Vector2.Negate(b2pvec);

                // scale perpendicular vectors by how deep the brackets need to be
                b1pvec *= bracketDepth;
                b2pvec *= bracketDepth;

                // bad brackets
                if (double.IsNaN(b1pvec.X) || double.IsNaN(b1pvec.Y) 
                 || double.IsNaN(b2pvec.X) || double.IsNaN(b2pvec.Y))
                    return result;

                {
                    var path = new PathGeometry();

                    if (round)
                    {
                        {
                            // bracket 1 (cp: control point)
                            var pf = new PathFigure
                            {
                                StartPoint = new Point(b1p1.X + b1pvec.X, b1p1.Y + b1pvec.Y)
                            };
                            Vector2 cpb1 = VecmathUtil.Midpoint(b1p1, b1p2);
                            cpb1 += VecmathUtil.Negate(b1pvec);
                            var seg = new QuadraticBezierSegment
                            {
                                Point1 = new Point(cpb1.X, cpb1.Y),
                                Point2 = new Point(b1p2.X + b1pvec.X, b1p2.Y + b1pvec.Y)
                            };
                            pf.Segments.Add(seg);
                            path.Figures.Add(pf);
                        }

                        {
                            // bracket 2 (cp: control point)
                            var pf = new PathFigure
                            {
                                StartPoint = new Point(b2p1.X + b2pvec.X, b2p1.Y + b2pvec.Y)
                            };
                            var cpb2 = VecmathUtil.Midpoint(b2p1, b2p2);
                            cpb2 += VecmathUtil.Negate(b2pvec);
                            var seg = new QuadraticBezierSegment
                            {
                                Point1 = new Point(cpb2.X, cpb2.Y),
                                Point2 = new Point(b2p2.X + b2pvec.X, b2p2.Y + b2pvec.Y)
                            };
                            pf.Segments.Add(seg);
                            path.Figures.Add(pf);
                        }
                    }
                    else
                    {
                        {
                            // bracket 1
                            var pf = new PathFigure
                            {
                                StartPoint = new Point(b1p1.X + b1pvec.X, b1p1.Y + b1pvec.Y)
                            };
                            var seg = new PolyLineSegment();
                            seg.Points.Add(new Point(b1p1.X, b1p1.Y));
                            seg.Points.Add(new Point(b1p2.X, b1p2.Y));
                            seg.Points.Add(new Point(b1p2.X + b1pvec.X, b1p2.Y + b1pvec.Y));
                            pf.Segments.Add(seg);
                            path.Figures.Add(pf);
                        }

                        {
                            // bracket 2
                            var pf = new PathFigure
                            {
                                StartPoint = new Point(b2p1.X + b2pvec.X, b2p1.Y + b2pvec.Y)
                            };
                            var seg = new PolyLineSegment();
                            seg.Points.Add(new Point(b2p1.X, b2p1.Y));
                            seg.Points.Add(new Point(b2p2.X, b2p2.Y));
                            seg.Points.Add(new Point(b2p2.X + b2pvec.X, b2p2.Y + b2pvec.Y));
                            pf.Segments.Add(seg);
                            path.Figures.Add(pf);
                        }
                    }

                    result.Add(GeneralPath.OutlineOf(path, stroke, foreground));
                }

                // work out where to put the suffix labels (e.g. ht/hh/eu) superscript
                // and (e.g. n, xl, c, mix) subscript
                // TODO: could be improved
                var b1MaxX = Math.Max(b1p1.X, b1p2.X);
                var b2MaxX = Math.Max(b2p1.X, b2p2.X);
                var b1MaxY = Math.Max(b1p1.Y, b1p2.Y);
                var b2MaxY = Math.Max(b2p1.Y, b2p2.Y);

                var subSufPnt = b2p2;
                var supSufPnt = b2p1;
                var subpvec = b2pvec;

                var bXDiff = b1MaxX - b2MaxX;
                var bYDiff = b1MaxY - b2MaxY;

                if (bXDiff > EQUIV_THRESHOLD || (bXDiff > -EQUIV_THRESHOLD && bYDiff < -EQUIV_THRESHOLD))
                {
                    subSufPnt = b1p2;
                    supSufPnt = b1p1;
                    subpvec = b1pvec;
                }

                var xDiff = subSufPnt.X - supSufPnt.X;
                var yDiff = subSufPnt.Y - supSufPnt.Y;

                if (yDiff > EQUIV_THRESHOLD || (yDiff > -EQUIV_THRESHOLD && xDiff > EQUIV_THRESHOLD))
                {
                    var tmpP = subSufPnt;
                    subSufPnt = supSufPnt;
                    supSufPnt = tmpP;
                }

                // subscript/superscript suffix annotation
                if (subscriptSuffix != null && subscriptSuffix.Any())
                {
                    var subscriptOutline = LeftAlign(MakeText(subscriptSuffix.ToLowerInvariant(), subSufPnt, subpvec, labelScale));
                    result.Add(GeneralPath.ShapeOf(subscriptOutline.GetOutline(), foreground));
                }
                if (superscriptSuffix != null && superscriptSuffix.Any())
                {
                    var superscriptOutline = LeftAlign(MakeText(superscriptSuffix.ToLowerInvariant(), supSufPnt, subpvec, labelScale));
                    result.Add(GeneralPath.ShapeOf(superscriptOutline.GetOutline(), foreground));
                }
            }
            return result;
        }

        private GeneralPath CreateRoundBracket(Vector2 p1, Vector2 p2, Vector2 perp, Vector2 midpoint)
        {
            var path = new PathGeometry();

            // bracket 1 (cp: control point)
            var pf = new PathFigure
            {
                StartPoint = new Point(p1.X + perp.X, p1.Y + perp.Y)
            };
            var cpb1 = midpoint + VecmathUtil.Negate(perp);
            var seg = new QuadraticBezierSegment
            {
                Point1 = new Point(cpb1.X, cpb1.Y),
                Point2 = new Point(p2.X + perp.X, p2.Y + perp.Y)
            };
            pf.Segments.Add(seg);
            path.Figures.Add(pf);
            return GeneralPath.OutlineOf(path, stroke, foreground);
        }

        private GeneralPath CreateSquareBracket(Vector2 p1, Vector2 p2, Vector2 perp)
        {
            var path = new PathGeometry();

            var pf = new PathFigure
            {
                StartPoint = new Point(p1.X + perp.X, p1.Y + perp.Y)
            };
            var seg = new PolyLineSegment();
            seg.Points.Add(new Point(p1.X, p1.Y));
            seg.Points.Add(new Point(p2.X, p2.Y));
            seg.Points.Add(new Point(p2.X + perp.X, p2.Y + perp.Y));
            pf.Segments.Add(seg);
            path.Figures.Add(pf);
            return GeneralPath.OutlineOf(path, stroke, foreground);
        }

        private static IReadOnlyDictionary<SgroupBracket, IBond> BracketBondPairs(ICollection<SgroupBracket> brackets, ICollection<IBond> bonds)
        {
            var pairs = new Dictionary<SgroupBracket, IBond>();

            foreach (var bracket in brackets)
            {
                IBond crossingBond = null;
                foreach (var bond in bonds)
                {
                    var a1 = bond.Begin;
                    var a2 = bond.End;
                    if (Vectors.LinesIntersect(
                        bracket.FirstPoint.X, bracket.FirstPoint.Y,
                        bracket.SecondPoint.X, bracket.SecondPoint.Y,
                        a1.Point2D.Value.X, a1.Point2D.Value.Y,
                        a2.Point2D.Value.X, a2.Point2D.Value.Y))
                    {
                        // more than one... not good
                        if (crossingBond != null)
                            return new Dictionary<SgroupBracket, IBond>();
                        crossingBond = bond;
                    }
                }
                if (crossingBond == null)
                    return new Dictionary<SgroupBracket, IBond>();
                pairs[bracket] = crossingBond;
            }

            return pairs;
        }

        private TextOutline MakeText(string subscriptSuffix, Vector2 b1p2, Vector2 b1pvec, double labelScale)
        {
            return StandardGenerator.GenerateAnnotation(b1p2, subscriptSuffix, VecmathUtil.Negate(b1pvec), 1, labelScale, font, emSize, null).Resize(1 / scale, 1 / scale);
        }

        private static TextOutline LeftAlign(TextOutline outline)
        {
            var center = outline.GetCenter();
            var first = outline.GetFirstGlyphCenter();
            return outline.Translate(center.X - first.X, center.Y - first.Y);
        }
    }
}
