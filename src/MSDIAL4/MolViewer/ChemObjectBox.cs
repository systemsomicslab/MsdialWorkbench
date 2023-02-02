/*
 * Copyright (C) 2018  Kazuya Ujihara <ujihara.kazuya@gmail.com>
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 * All we ask is that proper credit is given for our work, which includes
 * - but is not limited to - adding the above copyright notice to the beginning
 * of your source code files, and to any copyright notice that you may distribute
 * with programs based on this work.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT Any WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Depict;
using NCDK.Tools.Manipulator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace NCDK.MolViewer
{
    public partial class ChemObjectBox : System.Windows.Controls.UserControl
    {
        public delegate void ChemObjectChangedEventHandler(object sender, ChemObjectChangedEventArgs e);

        public event ChemObjectChangedEventHandler ChemObjectChanged;

        internal Depiction depiction;
        private IChemObject _ChemObject = null;
        private string _HighlightObjects = "";

        public ChemObjectBox()
        {
            Generator = new DepictionGenerator();
        }

        public DepictionGenerator Generator { get; }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            if (depiction != null)
            {
                depiction.Draw(dc);
            }
        }

        private static readonly Regex reSplitHL = new Regex(@"(?<ab>Atoms|Bonds)\[(?<nn>\d+)\]", RegexOptions.Compiled);

        private static IEnumerable<IChemObject> SplitHighlightinhObjects(IAtomContainer mol, string text)
        {
            var abs = text.Split(',').Select(n => n.Trim());
            foreach (var ab in abs)
            {
                var match = reSplitHL.Match(ab);
                int nn;
                try
                {
                    nn = int.Parse(match.Groups["nn"].Value);
                }
                catch (Exception)
                {
                    yield break;
                }
                switch (match.Groups["ab"].Value)
                {
                    case "Atoms":
                        yield return mol.Atoms[nn];
                        break;
                    case "Bonds":
                        yield return mol.Bonds[nn];
                        break;
                }
            }
            yield break;
        }

        private void UpdateVisual()
        {
            switch (_ChemObject)
            {
                case IAtomContainer mol:
                    depiction = Generator.Depict(mol, MakeHighlightDictionary(mol));
                    break;
                case IReaction rxn:
                    depiction = Generator.Depict(rxn, MakeHighlightDictionary(ReactionManipulator.ToMolecule(rxn)));
                    break;
                default:
                    depiction = null;
                    break;
            }

            this.InvalidateVisual();
        }

        private Dictionary<IChemObject, Color> MakeHighlightDictionary(IAtomContainer mol)
        {
            var highlightDic = new Dictionary<IChemObject, Color>();
            foreach (var o in SplitHighlightinhObjects(mol, this.HighlightingObjects))
            {
                highlightDic.Add(o, Colors.Aqua);
            }

            return highlightDic;
        }
    }
}
