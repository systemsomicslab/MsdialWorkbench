//
//  Splash.cs
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
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Linq;
using NSSplash.impl;
using System.Diagnostics;

namespace NSSplash {
	public class Splash : ISplash {
		private const string PREFIX = "splash";
		private const int VERSION = 0;

		private const int PREFILTER_BASE = 3;
		private const int PREFILTER_LENGTH = 10;
		private const int PREFILTER_BIN_SIZE = 5;

		private const int SIMILARITY_BASE = 10;
		private const int SIMILARITY_LENGTH = 10;
		private const int SIMILARITY_BIN_SIZE = 100;

		/// <summary>
		/// how to scale the spectrum
		/// </summary>
		public static readonly int scalingOfRelativeIntensity = 100;

		//private static readonly char[] INTENSITY_MAP = new char[] {
		//	'0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c',
		//	'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p',
		//	'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z'
		//};

		private static readonly string INTENSITY_MAP = "0123456789abcdefghijklmnopqrstuvwxyz";

		/// <summary>
		/// how should ions in the string representation be separeted
		/// </summary>
		private static readonly string ION_SEPERATOR = " ";

		/// <summary>
		/// how many character should be in the spectrum block. Basically this reduces the SHA256 code down
		/// to a fixed length of N characater
		/// </summary>
		private static readonly int maxCharactersForSpectrumBlockTruncation = 20;

		/// <summary>
		/// Fixed precission of masses
		/// </summary>
		private static readonly int fixedPrecissionOfMasses = 6;

		/// <summary>
		/// factor to scale m/z floating point values
		/// </summary>
		private static readonly long MZ_PRECISION_FACTOR = (long)Math.Pow(10, fixedPrecissionOfMasses);

		/// <summary>
		/// Fixed precission of intensites
		/// </summary>
		private static readonly int fixedPrecissionOfIntensities = 0;

		/// <summary>
		/// factor to scale m/z floating point values
		/// </summary>
		private static readonly long INTENSITY_PRECISION_FACTOR = (long)Math.Pow(10, fixedPrecissionOfIntensities);

		/// <summary>
		/// Correction factor to avoid floating point issues between implementations
		/// and processor architectures
		/// </summary>
		private static readonly double EPSILON = 1.0e-7;



		public string splashIt(ISpectrum spectrum) {

			// check spectrum var
			if (spectrum == null) {
				throw new ArgumentNullException("The spectrum can't be null");
			}

			StringBuilder hash = new StringBuilder();

			//creating first block 'splash<type><version>'
			hash.Append(getFirstBlock(spectrum.getSpectrumType()));
			hash.Append('-');

			//create prefilter block
			var filteredSpec = filterSpectrum(spectrum, 10, 0.1);
			Debug.WriteLine("filtered spectrum: " + filteredSpec.ToString());
			var preFilterHistogram = getHistoBlock(filteredSpec, PREFILTER_BASE, PREFILTER_LENGTH, PREFILTER_BIN_SIZE);
			Debug.WriteLine("prefilter block: " + preFilterHistogram);
			var translated = translateBase(preFilterHistogram, PREFILTER_BASE, 36, 4);

			hash.Append(translated);
			hash.Append('-');

			//create similarity block
			hash.Append(getHistoBlock(spectrum, SIMILARITY_BASE, SIMILARITY_LENGTH, SIMILARITY_BIN_SIZE));
			hash.Append('-');

			//create the spetrum hash block
			hash.Append(getSpectrumBlock(spectrum));

			return hash.ToString();

		}


		
		/// <summary>
		/// Generates the version block
		/// </summary>
		/// <param name="specType">type of spectrum beign splashed</param>
		/// <returns>the version block as a string</returns>
		private string getFirstBlock(SpectrumType specType) {
			Debug.WriteLine(string.Format("version block: {0}", PREFIX + (int)specType + VERSION));
			return (PREFIX + (int)specType + VERSION);
		}


		/// <summary>
		/// calculates a histogram of the spectrum. If weighted, it sums the mz * intensities for the peaks in each bin
		/// </summary>
		/// <param name="spec">the spectrum data (in mz:int pairs)</param>
		/// <returns>the histogram block for the given spectrum</returns>
		private string getHistoBlock(ISpectrum spec, int nbase, int length, int binSize) {
			double[] binnedIons = new double[length];
			double maxIntensity = 0;

			// initialize and populate bins
			foreach (Ion ion in ((MSSpectrum)spec).Ions) {
				int bin = (int)(ion.MZ / binSize) % length;
				binnedIons[bin] += ion.Intensity;

				if (binnedIons[bin] > maxIntensity) {
					maxIntensity = binnedIons[bin];
				}
			}

			// Normalize the histogram
			for (int i = 0; i < length; i++) {
				binnedIons[i] = (nbase - 1) * binnedIons[i] / maxIntensity;
			}

			// build histogram
			StringBuilder histogram = new StringBuilder();

			foreach (double bin in binnedIons.ToList().GetRange(0, length)) {
				histogram.Append(INTENSITY_MAP.ElementAt((int)(bin + EPSILON)));
			}

			Debug.WriteLine(string.Format("{1} block: {0}", histogram.ToString(), length==10?"histogram":"similarity"));
			return histogram.ToString();
		}


		/// <summary>
		/// calculate the hash for the whole spectrum
		/// </summary>
		/// <param name="spec">the spectrum data (in mz:int pairs)</param>
		/// <returns>the Hash of the spectrum data</returns>
		private string getSpectrumBlock(ISpectrum spec)
		{
			List<Ion> ions = spec.getSortedIonsByMZ();

			StringBuilder strIons = new StringBuilder();
			foreach (Ion i in ions)
			{
				strIons.Append(string.Format("{0}:{1}", formatMZ(i.MZ), formatIntensity(i.Intensity)));
				strIons.Append(ION_SEPERATOR);
			}

			//string to hash
			strIons.Remove(strIons.Length - 1, 1);
			byte[] message = Encoding.UTF8.GetBytes(strIons.ToString());

			SHA256Managed hashString = new SHA256Managed();
			hashString.ComputeHash(message);

			string hash = BitConverter.ToString(hashString.Hash);
			hash = hash.Replace("-", "").Substring(0, maxCharactersForSpectrumBlockTruncation).ToLower();

			Debug.WriteLine(string.Format("hash block: {0}", hash));

			return hash;
		}


		/// <summary>
		/// Translate a number in string format from one numerical base to another
		/// </summary>
		/// <param name="number">number in string format</param>
		/// <param name="initialBase">base in which the given number is represented</param>
		/// <param name="finalBase">base to translate the number to, up to 36</param>
		/// <param name="fill">minimum length of string</param>
		/// <returns></returns>
		private string translateBase(string number, int initialBase, int finalBase, int fill)
		{
			int n = ToBase10(number, initialBase);

			StringBuilder result = new StringBuilder();

			while (n > 0)
			{
				result.Insert(0, INTENSITY_MAP[n % finalBase]);
				n /= finalBase;
			}

			while (result.Length < fill)
			{
				result.Insert(0, '0');
			}

			Debug.WriteLine("prefilter: " + result);
			return result.ToString();
		}

		private string formatMZ(double number) {
			return string.Format("{0}", (long)((number + EPSILON) * MZ_PRECISION_FACTOR));
		}

		private string formatIntensity(double number) {
			return string.Format("{0}", (long)((number + EPSILON) * INTENSITY_PRECISION_FACTOR));
		}


		public static int ToBase10(string number, int start_base)
		{
			int sum = 0;
			int power = 0;
			Debug.WriteLine("before base10: " + number);
			foreach(char c in number.Reverse())
			{
				sum += (int)(INTENSITY_MAP.IndexOf(c) * Math.Pow(start_base, power));
				power++;
			}
			Debug.WriteLine("after base10: " + sum);

			return sum;
		}

		/**
		 * Filters spectrum by number of highest abundance ions and by base peak percentage
		 * @param s spectrum
		 * @param topIons number of top ions to retain
		 * @param basePeakPercentage percentage of base peak above which to retain
		 * @return filtered spectrum
		 */
		protected ISpectrum filterSpectrum(ISpectrum s, int topIons, double basePeakPercentage)
		{
			List<Ion> ions = s.GetIons();

			// Find base peak intensity
			double basePeakIntensity = 0.0;

			foreach (Ion ion in ions)
			{
				if (ion.Intensity > basePeakIntensity)
					basePeakIntensity = ion.Intensity;
			}

			// Filter by base peak percentage if needed
			if (basePeakPercentage >= 0)
			{
				List<Ion> filteredIons = new List<Ion>();

				foreach (Ion ion in ions)
				{
					if (ion.Intensity + EPSILON >= basePeakPercentage * basePeakIntensity)
						filteredIons.Add(new Ion(ion.MZ, ion.Intensity));
				}

				ions = filteredIons;
			}

			// Filter by top ions if necessary
			if (topIons > 0 && ions.Count > topIons)
			{
				ions = ions.OrderByDescending(i => i.Intensity).ThenBy(m => m.MZ).ToList();

				ions = ions.GetRange(0, topIons);
			}

			return new MSSpectrum(ions);
		}

		/**
		 * Filters spectrum by number of highest abundance ions
		 * @param s spectrum
		 * @param topIons number of top ions to retain
		 * @return filtered spectrum
		 */
		protected ISpectrum filterSpectrum(ISpectrum s, int topIons)
		{
			return filterSpectrum(s, topIons, -1);
		}

		/**
		 * Filters spectrum by base peak percentage
		 * @param s spectrum
		 * @param basePeakPercentage percentage of base peak above which to retain
		 * @return filtered spectrum
		 */
		protected ISpectrum filterSpectrum(ISpectrum s, double basePeakPercentage)
		{
			return filterSpectrum(s, -1, basePeakPercentage);
		}
	}
}

