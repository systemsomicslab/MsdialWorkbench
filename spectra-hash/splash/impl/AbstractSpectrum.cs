//
//  AbstractSpectrum.cs
//
//  Author:
//       Diego Pedrosa <dpedrosa@ucdavis.edu>
//
//  Copyright (c) 2015 Diego Pedrosa
//
//  This library is free software; you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as
//  published by the Free Software Foundation; either version 2.1 of the
//  License, or (at your option) any later version.
//
//  This library is distributed in the hope that it will be useful, but
//  WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
//  Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public
//  License along with this library; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace NSSplash.impl {
	
	public abstract class AbstractSpectrum : ISpectrum {
		private const int MAX_RELATIVE_INTENSITY = 100;

		#region fields and Properties
		protected SpectrumType type;
		public SpectrumType Type { 
			get { return this.type; }
			internal set { type = value; }
		}

		protected List<Ion> ions = new List<Ion>();
		public List<Ion> Ions {
			get { return ions; }
			internal set { ions = value; }
		}
		#endregion

		#region Constructors
		/// <summary>
		/// Creates a new spectrum based on a string of ions
		/// </summary>
		/// <param name="data"></param>
		public AbstractSpectrum (string data) {
			//checke data has data 
			if ("" == data) {
				throw new ArgumentException ("The spectrum data can't be null or empty.");
			}

            ions = new List<Ion>();
			Array splitData = data.Split (' ');

			foreach(string ion in splitData) {
				//get m/z
				double mz = double.Parse(ion.Split(':')[0]);
				//get intensity
				double intensity = double.Parse(ion.Split(':')[1]);

				Ion newIon = new Ion(mz, intensity);
				Ions.Add(newIon);
			}

			Ions = this.toRelative(MAX_RELATIVE_INTENSITY);
		}


		/// <summary>
		/// Creates a spectrum based on a list of ions
		/// </summary>
		/// <param name="ions"></param>
		public AbstractSpectrum(List<Ion> ions)
		{
			if(ions.Count <= 0)
			{
				throw new ArgumentException("The spectrum data can't be null or empty.");
			}

			Ions = ions;
			Ions = this.toRelative(MAX_RELATIVE_INTENSITY);
		}
		#endregion

		public override string ToString() {
			StringBuilder ionList = new StringBuilder();
			foreach(Ion ion in Ions) {
				ionList.Append(ion);
				ionList.Append(' ');
			}

			if(ionList.Length > 1) {
				ionList.Remove(ionList.Length - 1, 1);
			}

			return string.Format("[Spectrum: Type={0}, Ions={1}]", Type, ionList.ToString());
		}

		//returns a JSON representation of the spectrum
		public string toJSON() {
			StringBuilder ionList = new StringBuilder();
			foreach(Ion ion in Ions) {
				ionList.Append(ion.ToJSON());
				ionList.Append(' ');
			}

			if(ionList.Length > 1) {
				ionList.Remove(ionList.Length - 1, 1);
			}

			return string.Format("[Spectrum: Type={0}, Ions={1}]", Type, ionList.ToString());
		}

		//returns the current spectrum type
		public SpectrumType getSpectrumType() {
			return this.Type;
		}

		// returns the current spectrum's list of ions sorted by descending intensities
		public List<Ion> getSortedIonsByIntensity(bool desc = true) {
			List<Ion> sorted = Ions.OrderByDescending(i => i.Intensity).ThenBy(m => m.MZ).ToList();

			if(!desc) {
				sorted.Reverse();
			}

			return sorted;
		}

		// returns the current spectrum's list of ions sorted by ascending m/z ratio values
		public List<Ion> getSortedIonsByMZ(bool desc = false) {
			List<Ion> sorted = Ions.OrderBy(i => i.MZ).ThenByDescending(i => i.Intensity).ToList();

			if(desc) {
				sorted.Reverse();
			}

			return sorted;
		}

		// calculate the relative spectrum in the range [0 .. 100]
		private List<Ion> toRelative(int scale) {
			List<Ion> relativeIons = new List<Ion>();
			relativeIons.AddRange(Ions);

			double maxInt = relativeIons.Max(ion => ion.Intensity);
			relativeIons.ForEach(i => i.Intensity = i.Intensity / maxInt * scale);

			return relativeIons;
		}

		// returns list of ions
		public abstract List<Ion> GetIons();
	}
}

