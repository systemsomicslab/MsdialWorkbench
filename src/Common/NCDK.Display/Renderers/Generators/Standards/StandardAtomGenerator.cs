/*
 * Copyright (c) 2014 European Bioinformatics Institute (EMBL-EBI)
 *                    John May <jwmay@users.sf.net>
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
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace NCDK.Renderers.Generators.Standards
{
    /// <summary>
    /// Generates <see cref="AtomSymbol"/> instances with positioned adjuncts.
    /// </summary>
    /// <remarks>
    /// Note the generator is purposefully not an <see cref="IGenerator{T}"/> and is intended as part be called from the StandardGenerator.
    /// </remarks>
    // @author John May
    internal sealed class StandardAtomGenerator
    {
        /// <summary>
        /// Default options for spacing and sizing adjuncts, could be configurable parameters.
        /// </summary>
        private const double DefaultAdjunctSpacingRatio = 0.15d;
        private const double DefaultSubscriptSize = 0.6d;

        /// <summary>
        /// The font used in the symbol.
        /// </summary>
        private readonly Typeface font;

        private readonly double emSize;

        /// <summary>
        /// Relative size of the adjunct sub/super script labels (0-1).
        /// </summary>
        private readonly double scriptSize;

        /// <summary>
        /// The absolute distance to 'pad' adjunct positioning with.
        /// </summary>
        private readonly double padding;

        /// <summary>
        /// Text outline is immutable so we can create a hydrogen are reuse it.
        /// </summary>
        private readonly TextOutline defaultHydrogenLabel;

        /// <summary>
        /// Create a standard atom generator using the specified font.
        /// </summary>
        /// <param name="font">the symbol font</param>
        public StandardAtomGenerator(Typeface font, double emSize)
            : this(font, emSize, DefaultAdjunctSpacingRatio, DefaultSubscriptSize)
        {
        }

        /// <summary>
        /// Internal constructor with required attributes.
        /// </summary>
        /// <param name="font">the font to depict symbols with</param>
        /// <param name="adjunctSpacing">the spacing between adjuncts and the element symbol as fraction of 'H'  width</param>
        /// <param name="scriptSize">the size of</param>
        private StandardAtomGenerator(Typeface font, double emSize, double adjunctSpacing, double scriptSize)
        {
            this.font = font;
            this.emSize = emSize;
            this.scriptSize = scriptSize;
            this.defaultHydrogenLabel = new TextOutline("H", font, emSize);
            this.padding = adjunctSpacing * defaultHydrogenLabel.GetBounds().Width;
        }

        /// <summary>
        /// Generate the displayed atom symbol for an atom in given structure with the specified hydrogen
        /// position.
        /// </summary>
        /// <param name="container">structure to which the atom belongs</param>
        /// <param name="atom">the atom to generate the symbol for</param>
        /// <param name="position">the hydrogen position</param>
        /// <param name="model">additional rendering options</param>
        /// <returns>atom symbol</returns>
        public AtomSymbol GenerateSymbol(IAtomContainer container, IAtom atom, HydrogenPosition position, RendererModel model)
        {
            if (atom is IPseudoAtom pAtom)
            {
                if (pAtom.AttachPointNum <= 0)
                {
                    if ("*".Equals(pAtom.Label, StringComparison.Ordinal))
                    {
                        var mass = pAtom.MassNumber ?? 0;
                        var charge = pAtom.FormalCharge ?? 0;
                        var hcnt = pAtom.ImplicitHydrogenCount ?? 0;
                        var nrad = container.GetConnectedSingleElectrons(atom).Count();
                        if (mass != 0 || charge != 0 || hcnt != 0)
                        {
                            return GeneratePeriodicSymbol(0, hcnt, mass, charge, nrad, position);
                        }
                    }
                    return GeneratePseudoSymbol(AccessPseudoLabel(pAtom, "?"), position);
                }
                else
                    return null; // attach point drawn in bond generator
            }
            else
            {
                var number = atom.AtomicNumber;

                // unset the mass if it's the major isotope (could be an option)
                var mass = atom.MassNumber;
                if (number != 0
                 && mass != null 
                 && model != null
                 && model.GetOmitMajorIsotopes() 
                 && IsMajorIsotope(number, mass.Value))
                {
                    mass = null;
                }

                return GeneratePeriodicSymbol(
                    number, 
                    atom.ImplicitHydrogenCount ?? 0,
                    mass ?? -1, 
                    atom.FormalCharge ?? 0,
                    container.GetConnectedSingleElectrons(atom).Count(), position);
            }
        }

        /// <summary>
        /// Generates an atom symbol for a pseudo atom.
        /// </summary>
        /// <returns>the atom symbol</returns>
        public AtomSymbol GeneratePseudoSymbol(string label, HydrogenPosition position)
        {
            var italicFont = new Typeface(font.FontFamily, FontStyles.Italic, FontWeights.Bold, font.Stretch);
            var outlines = new List<TextOutline>(3);

            int beg = 0;
            int pos = 0;
            int len = label.Length;

            // upper case followed by lower case
            while (pos < len && IsUpperCase(label[pos]))
                pos++;
            if (label[0] != 'R') // Ar is not A^r but 'Ra' is R^a etc
                while (pos < len && IsLowerCase(label[pos]))
                    pos++;

            if (pos > beg)
            {
                outlines.Add(new TextOutline(label.Substring(beg, pos), italicFont, emSize));
                beg = pos;
                // 2a etc.
                while (pos < len && IsDigit(label[pos]))
                    pos++;
                while (pos < len && IsLowerCase(label[pos]))
                    pos++;

                if (pos > beg)
                {
                    var outline = new TextOutline(label.Substring(beg, pos - beg), italicFont, emSize);
                    outline = outline.Resize(scriptSize, scriptSize);
                    outline = PositionSuperscript(outlines[0], outline);
                    outlines.Add(outline);
                }

                int numPrimes = 0;

                while (pos < len)
                {
                    switch (label[pos])
                    {
                        case '\'': numPrimes++; break;
                        case '`': numPrimes++; break;
                        case '‘': numPrimes++; break;
                        case '’': numPrimes++; break;
                        case '‛': numPrimes++; break;
                        case '“': numPrimes += 2; break;
                        case '”': numPrimes += 2; break;
                        case '′': numPrimes++; break;
                        case '″': numPrimes += 2; break;
                        case '‴': numPrimes += 3; break;
                        case '⁗': numPrimes += 4; break;
                        case '‵': numPrimes++; break;
                        case '‶': numPrimes += 2; break;
                        case '‷': numPrimes += 3; break;
                        case '´': numPrimes++; break;
                        case 'ˊ': numPrimes++; break;
                        case '́': numPrimes++; break;
                        case '˝': numPrimes += 2; break;
                        case '̋': numPrimes += 2; break;
                        default:
                            goto break_PRIMES;
                    }
                    pos++;
                }
                break_PRIMES:
                if (pos < len)
                {
                    return new AtomSymbol(
                        new TextOutline(label, italicFont, emSize),
                        Array.Empty<TextOutline>());
                }
                else
                {
                    TextOutline outline = null;
                    var ref_ = outlines[outlines.Count - 1];
                    switch (numPrimes)
                    {
                        case 0: break;
                        case 1: outline = new TextOutline("′", font, emSize); break;
                        case 2: outline = new TextOutline("″", font, emSize); break;
                        case 3: outline = new TextOutline("‴", font, emSize); break;
                        default:
                            string lab = "";
                            while (numPrimes-- > 0)
                                lab += "′";
                            outline = new TextOutline(lab, font, emSize);
                            break;
                    }
                    if (outline != null)
                    {
                        if (outlines.Count > 1)
                            outline = outline.Resize(scriptSize, scriptSize);
                        outline = PositionSuperscript(ref_, outline);
                        outlines.Add(outline);
                    }
                }

                // line up text
                for (int i = 1; i < outlines.Count; i++)
                {
                    var ref_ = outlines[i - 1];
                    var curr = outlines[i];
                    outlines[i] = PositionAfter(ref_, curr);
                }

                return new AtomSymbol(outlines[0], outlines.GetRange(1, outlines.Count - 1));
            }
            else
            {
                return new AtomSymbol(new TextOutline(label, italicFont, emSize), Array.Empty<TextOutline>());
            }
        }

        private static bool IsUpperCase(char c)
        {
            return c >= 'A' && c <= 'Z';
        }

        private static bool IsLowerCase(char c)
        {
            return c >= 'a' && c <= 'z';
        }

        private static bool IsDigit(char c)
        {
            return c >= '0' && c <= '9';
        }

        public AtomSymbol GenerateAbbreviatedSymbol(string label, HydrogenPosition position)
        {
            var tokens = new List<string>();
            if (AbbreviationLabel.Parse(label, tokens))
            {
                return GenerateAbbreviationSymbol(tokens, position);
            }
            else
            {
                return new AtomSymbol(new TextOutline(label, font, emSize), Array.Empty<TextOutline>());
            }
        }

        /// <summary>
        /// Generate a formatted abbreviation AtomSymbol for the given Hydrogen position.
        /// </summary>
        /// <param name="tokens">the parsed tokens</param>
        /// <param name="position">hydrogen position - determines if we reverse the label</param>
        /// <returns>the generated symbol</returns>
        public AtomSymbol GenerateAbbreviationSymbol(List<string> tokens, HydrogenPosition position)
        {
            if (position == HydrogenPosition.Left)
                AbbreviationLabel.Reverse(tokens);

            var tmpRefPoint = new TextOutline("H", font, emSize);
            var fTexts = AbbreviationLabel.Format(tokens);

            var italicFont = new Typeface(font.FontFamily, FontStyles.Italic, font.Weight, font.Stretch);

            var outlines = new List<TextOutline>(fTexts.Count);
            foreach (var fText in fTexts)
            {
                var outline = fText.Style == AbbreviationLabel.STYLE_ITALIC
                            ? new TextOutline(fText.Text, italicFont, emSize)
                            : new TextOutline(fText.Text, font, emSize);

                // resize and position scripts
                if (fText.Style == AbbreviationLabel.STYLE_SUBSCRIPT)
                {
                    outline = outline.Resize(scriptSize, scriptSize);
                    outline = PositionSubscript(tmpRefPoint, outline);
                }
                else if (fText.Style == AbbreviationLabel.STYLE_SUPSCRIPT)
                {
                    outline = outline.Resize(scriptSize, scriptSize);
                    outline = PositionSuperscript(tmpRefPoint, outline);
                }

                outlines.Add(outline);
            }

            // position the outlines relative to each other
            for (int i = 1; i < outlines.Count; i++)
            {
                var ref_ = outlines[i - 1];
                var curr = outlines[i];
                // charge aligns to symbol not a subscript part
                if (fTexts[i].Style == AbbreviationLabel.STYLE_SUPSCRIPT &&
                    fTexts[i - 1].Style == AbbreviationLabel.STYLE_SUBSCRIPT && i > 1)
                {
                    ref_ = outlines[i - 2];
                }
                outlines[i] = PositionAfter(ref_, curr);
            }

            // find symbol where we want to attach the bond
            // this becomes the primary outline
            int index;
            if (position == HydrogenPosition.Left)
            {
                for (index = outlines.Count - 1; index >= 0; index--)
                    if ((fTexts[index].Style & 0x1) == 0)
                        break;
            }
            else
            {
                for (index = 0; index < outlines.Count; index++)
                    if ((fTexts[index].Style & 0x1) == 0)
                        break;
            }
            var primary = outlines[index];
            outlines.RemoveAt(index);

            return new AtomSymbol(primary, outlines);
        }

        /// <summary>
        /// Generate an atom symbol for a periodic element with the specified number of hydrogens, ionic
        /// charge, mass,
        /// </summary>
        /// <param name="number">atomic number</param>
        /// <param name="hydrogens">labelled hydrogen count</param>
        /// <param name="mass">atomic mass</param>
        /// <param name="charge">ionic formal charge</param>
        /// <param name="unpaired">number of unpaired electrons</param>
        /// <param name="position">placement of hydrogen</param>
        /// <returns>laid out atom symbol</returns>
        public AtomSymbol GeneratePeriodicSymbol(int number, int hydrogens, int mass, int charge, int unpaired, HydrogenPosition position)
        {
            var element = number == 0 
                        ? new TextOutline("*", font, emSize)
                        : new TextOutline(ChemicalElement.Of(number).Symbol, font, emSize);
            var hydrogenAdjunct = defaultHydrogenLabel;

            // the hydrogen count, charge, and mass adjuncts are script size
            var hydrogenCount = new TextOutline(hydrogens.ToString(), font, emSize).Resize(scriptSize, scriptSize);
            var chargeAdjunct = new TextOutline(ChargeAdjunctText(charge, unpaired), font, emSize).Resize(scriptSize, scriptSize);
            var massAdjunct = new TextOutline(mass.ToString(), font, emSize).Resize(scriptSize, scriptSize);

            // position each adjunct relative to the element label and each other
            hydrogenAdjunct = PositionHydrogenLabel(position, element, hydrogenAdjunct);
            hydrogenCount = PositionSubscript(hydrogenAdjunct, hydrogenCount);
            chargeAdjunct = PositionChargeLabel(hydrogens, position, chargeAdjunct, element, hydrogenAdjunct);
            massAdjunct = PositionMassLabel(massAdjunct, element);

            // when the hydrogen label is positioned to the left we may need to nudge it
            // over to account for the hydrogen count and/or the mass adjunct colliding
            // with the element label
            if (position == HydrogenPosition.Left)
            {
                var nudgeX = HydrogenXDodge(hydrogens, mass, element, hydrogenAdjunct, hydrogenCount, massAdjunct);
                hydrogenAdjunct = hydrogenAdjunct.Translate(nudgeX, 0);
                hydrogenCount = hydrogenCount.Translate(nudgeX, 0);
            }

            var adjuncts = new List<TextOutline>(4);

            if (hydrogens > 0)
                adjuncts.Add(hydrogenAdjunct);
            if (hydrogens > 1)
                adjuncts.Add(hydrogenCount);
            if (charge != 0 || unpaired > 0)
                adjuncts.Add(chargeAdjunct);
            if (mass > 0)
                adjuncts.Add(massAdjunct);

            return new AtomSymbol(element, adjuncts);
        }

        /// <summary>
        /// Position the hydrogen label relative to the element label.
        /// </summary>
        /// <param name="position">relative position where the hydrogen is placed</param>
        /// <param name="element">the outline of the element label</param>
        /// <param name="hydrogen">the outline of the hydrogen</param>
        /// <returns>positioned hydrogen label</returns>
        public TextOutline PositionHydrogenLabel(HydrogenPosition position, TextOutline element, TextOutline hydrogen)
        {
            var elementBounds = element.GetBounds();
            var hydrogenBounds = hydrogen.GetBounds();
            switch (position)
            {
                case HydrogenPosition.Above:
                    return hydrogen.Translate(0, (elementBounds.Top - padding) - hydrogenBounds.Bottom);
                case HydrogenPosition.Right:
                    return hydrogen.Translate((elementBounds.Right + padding) - hydrogenBounds.Left, 0);
                case HydrogenPosition.Below:
                    return hydrogen.Translate(0, (elementBounds.Bottom + padding) - hydrogenBounds.Top);
                case HydrogenPosition.Left:
                    return hydrogen.Translate((elementBounds.Left - padding) - hydrogenBounds.Right, 0);
            }
            return hydrogen; // never reached
        }

        /// <summary>
        /// Positions an outline in the subscript position relative to another 'primary' label.
        /// </summary>
        /// <param name="label">a label outline</param>
        /// <param name="subscript">the label outline to position as subscript</param>
        /// <returns>positioned subscript outline</returns>
        public TextOutline PositionSubscript(TextOutline label, TextOutline subscript)
        {
            var hydrogenBounds = label.GetBounds();
            var hydrogenCountBounds = subscript.GetBounds();
            subscript = subscript.Translate((hydrogenBounds.Right + padding) - hydrogenCountBounds.Left,
                                            (hydrogenBounds.Bottom + (hydrogenCountBounds.Height / 2)) - hydrogenCountBounds.Bottom);
            return subscript;
        }

        public TextOutline PositionSuperscript(TextOutline label, TextOutline superscript)
        {
            var labelBounds = label.GetBounds();
            var superscriptBounds = superscript.GetBounds();
            superscript = superscript.Translate((labelBounds.Right + padding) - superscriptBounds.Left,
                                                (labelBounds.Top - (superscriptBounds.Height / 2)) - superscriptBounds.Top);
            return superscript;
        }

        public TextOutline PositionAfter(TextOutline before, TextOutline after)
        {
            var fixedBounds = before.GetBounds();
            var movableBounds = after.GetBounds();
            after = after.Translate((fixedBounds.Right + padding) - movableBounds.Left, 0);
            return after;
        }

        /// <summary>
        /// Position the charge label on the top right of either the element or hydrogen label. Where the
        /// charge is placed depends on the number of hydrogens and their position relative to the
        /// element symbol.
        /// </summary>
        /// <param name="hydrogens">number of hydrogen</param>
        /// <param name="position">position of hydrogen</param>
        /// <param name="charge">the charge label outline (to be positioned)</param>
        /// <param name="element">the element label outline</param>
        /// <param name="hydrogen">the hydrogen label outline</param>
        /// <returns>positioned charge label</returns>
        public TextOutline PositionChargeLabel(int hydrogens, HydrogenPosition position, TextOutline charge, TextOutline element, TextOutline hydrogen)
        {
            var chargeBounds = charge.GetBounds();

            // the charge is placed to the top right of the element symbol
            // unless either the hydrogen label or the hydrogen count label
            // are in the way - in which case we place it relative to the
            // hydrogen
            var referenceBounds = element.GetBounds();
            if (hydrogens > 0 && position == HydrogenPosition.Right)
                referenceBounds = hydrogen.GetBounds();
            else if (hydrogens > 1 && position == HydrogenPosition.Above) referenceBounds = hydrogen.GetBounds();

            return charge.Translate((referenceBounds.Right + padding) - chargeBounds.Left,
                                    (referenceBounds.Top - (chargeBounds.Height / 2)) - chargeBounds.Top);
        }

        /// <summary>
        /// Position the mass label relative to the element label. The mass adjunct is position to the
        /// top left of the element label.
        /// </summary>
        /// <param name="massLabel">mass label outline</param>
        /// <param name="elementLabel">element label outline</param>
        /// <returns>positioned mass label</returns>
        public TextOutline PositionMassLabel(TextOutline massLabel, TextOutline elementLabel)
        {
            var elementBounds = elementLabel.GetBounds();
            var massBounds = massLabel.GetBounds();
            return massLabel.Translate((elementBounds.Left - padding) - massBounds.Right,
                                       (elementBounds.Top - (massBounds.Height / 2)) - massBounds.Top);
        }

        /// <summary>
        /// If the hydrogens are position in from of the element we may need to move the hydrogen and
        /// hydrogen count labels. This code assesses the positions of the mass, hydrogen, and hydrogen
        /// count labels and determines the x-axis adjustment needed for the hydrogen label to dodge a
        /// collision.
        /// </summary>
        /// <param name="hydrogens">number of hydrogens</param>
        /// <param name="mass">atomic mass</param>
        /// <param name="elementLabel">element label outline</param>
        /// <param name="hydrogenLabel">hydrogen label outline</param>
        /// <param name="hydrogenCount">hydrogen count label outline</param>
        /// <param name="massLabel">the mass label outline</param>
        /// <returns>required adjustment to x-axis</returns>
        private double HydrogenXDodge(int hydrogens, int mass, TextOutline elementLabel, TextOutline hydrogenLabel,
                                      TextOutline hydrogenCount, TextOutline massLabel)
        {
            if (mass < 0 && hydrogens > 1)
            {
                return (elementLabel.GetBounds().Left - padding) - hydrogenCount.GetBounds().Right;
            }
            else if (mass >= 0)
            {
                if (hydrogens > 1)
                {
                    return (massLabel.GetBounds().Left + padding) - hydrogenCount.GetBounds().Right;
                }
                else if (hydrogens > 0)
                {
                    return (massLabel.GetBounds().Left - padding) - hydrogenLabel.GetBounds().Right;
                }
            }
            return 0;
        }

        /// <summary>
        /// Utility to determine if the specified mass is the major isotope for the given atomic number.
        /// </summary>
        /// <param name="number">atomic number</param>
        /// <param name="mass">atomic mass</param>
        /// <returns>the mass is the major mass for the atomic number</returns>
        private static bool IsMajorIsotope(int number, int mass)
        {
            try
            {
                var isotope = CDK.IsotopeFactory.GetMajorIsotope(number);
                return isotope != null && isotope.MassNumber.Equals(mass);
            }
            catch (IOException)
            {
                return false;
            }
        }

        /// <summary>
        /// Characters used in the charge label.
        /// </summary>
        private const char BULLET = '•', // '\u2022'
                PLUS = '+', MINUS = '−'; // '\u2212' and not a hyphen

        /// <summary>
        /// Create the charge adjunct text for the specified charge and number of unpaired electrons.
        /// </summary>
        /// <param name="charge">formal charge</param>
        /// <param name="unpaired">number of unpaired electrons</param>
        /// <returns>adjunct text</returns>
        public static string ChargeAdjunctText(int charge, int unpaired)
        {
            var sb = new StringBuilder();

            if (unpaired == 1)
            {
                if (charge != 0)
                {
                    sb.Append('(').Append(BULLET).Append(')');
                }
                else
                {
                    sb.Append(BULLET);
                }
            }
            else if (unpaired > 1)
            {
                if (charge != 0)
                {
                    sb.Append('(').Append(unpaired).Append(BULLET).Append(')');
                }
                else
                {
                    sb.Append(unpaired).Append(BULLET);
                }
            }

            var sign = charge < 0 ? MINUS : PLUS;
            var coefficient = Math.Abs(charge);

            if (coefficient > 1) sb.Append(coefficient);
            if (coefficient > 0) sb.Append(sign);

            return sb.ToString();
        }

        /// <summary>
        /// Safely access the label of a pseudo atom. If the label is null or empty, the default label is
        /// returned.
        /// </summary>
        /// <param name="atom">the pseudo</param>
        /// <returns>pseudo label</returns>
        public static string AccessPseudoLabel(IPseudoAtom atom, string defaultLabel)
        {
            var label = atom.Label;
            if (!string.IsNullOrEmpty(label))
                return label;
            return defaultLabel;
        }
    }
}
