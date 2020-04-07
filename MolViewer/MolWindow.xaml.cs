/*
 * Copyright (C) 2017-2018  Kazuya Ujihara <ujihara.kazuya@gmail.com>
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

using Microsoft.Win32;
using NCDK.IO;
using System;
using System.IO;
using System.Windows;

namespace NCDK.MolViewer
{
    public partial class MolWindow : Window
    {
        private static ReaderFactory readerFactory = new ReaderFactory();
        private AppearanceViewModel viewModel;
        private AppearanceDialog appearanceDialog;

        public MolWindow()
        {
            InitializeComponent();

            this.DataContext = viewModel = new AppearanceViewModel();

            Toggle_appearance_Click(null, null);
        }

        private void MenuItem_Open_Click(object sender, RoutedEventArgs e)
        {
            IChemObject obj = null;
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    FilterIndex = 1,
                    Filter = 
                        "All supported files (*.mol;*.rxn;*.mol2)|*.mol;*.rxn;*.mol2|" +
                        "MDL Molfile (*.mol)|*.mol|" +
                        "MDL Rxnfile (*.rxn)|*.rxn|" +
                        "Mol2 (Sybyl) (*.mol2)|*.mol2|" +
                        "All Files (*.*)|*.*"
                };
                bool? result = openFileDialog.ShowDialog();
                if (result == true)
                {
                    var fn = openFileDialog.FileName;
                    var ex = Path.GetExtension(fn);
                    switch (ex)
                    {
                        case ".rxn":
                            using (var reader = readerFactory.CreateReader(new FileStream(fn, FileMode.Open)))
                            {
                                obj = reader.Read(new Silent.Reaction());
                            }
                            break;
                        default:
                            using (var reader = readerFactory.CreateReader(new FileStream(fn, FileMode.Open)))
                            {
                                if (reader == null)
                                    throw new Exception("Not supported.");
                                obj = reader.Read(new Silent.AtomContainer());
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            if (obj == null)
                return;

            viewModel.ChemObject = obj;
        }

        private void MenuItem_SaveAs_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel.ChemObject == null)
                return;

            SaveFileDialog fileDialog = new SaveFileDialog
            {
                FilterIndex = 1,
                Filter =
                    "PNG file (*.png)|*.png|" +
                    "JPEG file (*.jpg)|*.jpg;*.jpeg|" +
                    "GIF file (*.gif)|*.gif|" +
                    "SVG file (*.svg)|*.svg|" +
                    "All Files (*.*)|*.*"
            };

            if (fileDialog.ShowDialog() != true)
                return;

            objectBox.depiction.WriteTo(fileDialog.FileName);
        }

        private void Toggle_appearance_Click(object sender, RoutedEventArgs e)
        {
            if (appearanceDialog != null && appearanceDialog.IsVisible)
            {
                appearanceDialog.Close();
                appearanceDialog = null;
            }
            else
            {
                appearanceDialog = new AppearanceDialog(this.DataContext)
                {
                    Topmost = true
                };
                appearanceDialog.Show();
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (appearanceDialog != null)
            {
                appearanceDialog.Close();
                appearanceDialog = null;
            }
        }
    }
}
