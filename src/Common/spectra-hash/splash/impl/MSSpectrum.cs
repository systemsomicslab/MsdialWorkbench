//
//  MSSpectrum.cs
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

namespace NSSplash.impl
{
	public class MSSpectrum : AbstractSpectrum {
		public MSSpectrum(string spectrum) : base(spectrum)
		{
			this.Type = SpectrumType.MS;
		}

		public MSSpectrum(List<Ion> ions) : base(ions)
		{
			this.Type = SpectrumType.MS;
		}

		public override List<Ion> GetIons()
		{
			return Ions;
		}
	}
}

