//
//  Ion.cs
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
using System.Text;

namespace NSSplash.impl {
	public sealed class Ion : IComparable {
		private double mz = 0.0;
		private double intensity = 0.0;
        private const int PRECISION = 6;

        private string mzFormat = string.Format("{{0,5:F{0}}}", PRECISION);
		private string intFormat = string.Format("{{0,5:F{0}}}", PRECISION);

		public double MZ { 
			get { return mz; } 
			set { mz = value; } 
		}

		public double Intensity { 
			get { return intensity; }
			set { intensity = value; }
		}

		public Ion (double mz, double intensity) {
			this.mz = mz;
			this.intensity = intensity;
		}

		//returning ion in mz:intensity format with 6 decimals
		public override string ToString() {
			return string.Format("{0}:{1}", string.Format(mzFormat, mz), string.Format(intFormat, intensity));
		}

		//returning ion in mz:intensity format with 6 decimals in JSON format
		public string ToJSON() {
			StringBuilder json = new StringBuilder();
			json.Append("{\"mass\": ").AppendFormat(mzFormat, mz).Append(", \"intensity\":").AppendFormat(intFormat, intensity).Append("}");
			return json.ToString();
		}

		//compares by mz value
		public int CompareTo(Object other) {
			if (this.GetType () != other.GetType ()) {
				throw new ArgumentException (String.Format("Can't compare {0} with {1}.", this.GetType (), other.GetType ()));
			}

			Ion otherCpy = (Ion)other;

			if (this.intensity < otherCpy.intensity) {
				return -1;
			} else if (this.intensity > otherCpy.intensity) {
				return 1;
			} else {
				return otherCpy.mz.CompareTo(this.mz);
			}
		}
	}
}

