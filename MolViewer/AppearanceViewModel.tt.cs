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



using NCDK.Renderers;
using NCDK.Renderers.Colors;
using Prism.Mvvm;
using WPF = System.Windows;

namespace NCDK.MolViewer
{
    partial class AppearanceViewModel : BindableBase
    {
        private string _HighlightingObjects = "";
        public string HighlightingObjects
        {
            get { return _HighlightingObjects; }
            set { this.SetProperty(ref this._HighlightingObjects, value); }
        }
        private double _Zoom = 1;
        public double Zoom
        {
            get { return _Zoom; }
            set { this.SetProperty(ref this._Zoom, value); }
        }
        private bool _AlignMappedReaction = true;
        public bool AlignMappedReaction
        {
            get { return _AlignMappedReaction; }
            set { this.SetProperty(ref this._AlignMappedReaction, value); }
        }
        private WPF.Media.Color _BackgroundColor = WPF.Media.Colors.Transparent;
        public WPF.Media.Color BackgroundColor
        {
            get { return _BackgroundColor; }
            set { this.SetProperty(ref this._BackgroundColor, value); }
        }
        private IAtomColorer _AtomColorer = new UniColor(WPF.Media.Colors.Black);
        public IAtomColorer AtomColorer
        {
            get { return _AtomColorer; }
            set { this.SetProperty(ref this._AtomColorer, value); }
        }
        private HighlightStyle _Highlighting = HighlightStyle.None;
        public HighlightStyle Highlighting
        {
            get { return _Highlighting; }
            set { this.SetProperty(ref this._Highlighting, value); }
        }
        private double _OuterGlowWidth = RendererModelTools.DefaultOuterGlowWidth;
        public double OuterGlowWidth
        {
            get { return _OuterGlowWidth; }
            set { this.SetProperty(ref this._OuterGlowWidth, value); }
        }
    }
}
