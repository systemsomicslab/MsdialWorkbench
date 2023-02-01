//
//  SplashRunner.cs
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
using NSSplash.impl;
using System.Security.Cryptography;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Mono.Options;

namespace NSSplash {
	class SplashRunner {
//		private static int LIMIT = 10000;
		private static int UPDATE_INTERVAL = 10000;
		Splash splasher;

		public static void Main(string[] args) {
			SplashRunner app = new SplashRunner();

			bool show_help = false;
			string filename = string.Empty;
			int spec_col = 0;
			int id_col = 1;

			var p = new OptionSet() {
				"Usage: SplashRunner [OPTIONS]+",
				"Calculates splash codes for the specified file.",
				"",
				"Options:",
				{ "f|file=", "the {NAME} of the file containing the spectra to calculate splashes for.",
					v => filename = v },
				{ "s|spectrum=",
					"the zero-based column number of the spectrum.\n" +
					"this must be an integer. Default = 0",
					(int v) => spec_col = v },
				{ "i|id=",
					"the zero-based column number of the origin id.\n" +
					"this must be an string. Default = 1",
					(int v) => id_col = v },
				{ "h|help",  "show this message and exit",
					v => show_help = v != null },
			};

			List<string> extra;
			try {
				extra = p.Parse (args);
			} catch (OptionException e) {
				Console.Write ("SplashRunner: ");
				Console.WriteLine (e.Message);
				Console.WriteLine ("Try `SplashRunner --help' for more information.");
				return;
			}

			if (show_help) {
				p.WriteOptionDescriptions (Console.Out);
				return;
			}

			string error = "\nPlease provide a filename (<name>.csv) with spectra to hash.\nFile should contain a list of coma separated values in the form 'identifier,spectrum' each on a separate line.\n";

			if(string.IsNullOrEmpty(filename)) {
				Console.WriteLine("Error: {0}\n", error);
				p.WriteOptionDescriptions(Console.Out);
				return;
			} else if(!new FileInfo(filename).Exists) {
				Console.WriteLine("File '{0}' doesn't exists.\n");
				p.WriteOptionDescriptions(Console.Out);
				return;
			}

			app.hashFile(filename, spec_col, id_col);
		}

		public SplashRunner() {
			splasher = new Splash();
		}

		public void hashFile(string filename, int spec_col, int id_col) {
			StatisticBuilder stats = new StatisticBuilder();
			DateTime sTime, eTime;
			int count = 0;

			Console.WriteLine("params: {0}, {1}, {2}", filename, spec_col, id_col);

			FileInfo file = new FileInfo(String.Format("{0}-csharp.csv", filename.Substring(0,filename.LastIndexOf('.'))));

			if(file.Exists) {
				file.Delete();
				Console.WriteLine("deleted old file...");
			}

			sTime = DateTime.Now;

			using (StreamReader sr = File.OpenText(filename)) {
				string s = String.Empty;

				using(StreamWriter fout = new StreamWriter(File.OpenWrite(file.Name))) {
					StringBuilder result = new StringBuilder();
					fout.AutoFlush = true;

					while ((s = sr.ReadLine()) != null)	{
						if(string.IsNullOrWhiteSpace(s) || !s.Contains(",")) {
							Console.WriteLine("bad line... skipping.");
							continue;
						}

						string[] input = s.Split(',');
						if(!input[spec_col].Contains(":") && !input[id_col].Contains(":")) {
							result.Append("invalid input data").Append(",").Append(input[id_col]).Append(",").Append(input[spec_col]);
							continue;
						} else if(input[id_col].Contains(":")) {
							int tmp = id_col;
								id_col = spec_col;
								spec_col = tmp;
						}

						DateTime psTime = DateTime.Now;
						string hash = splasher.splashIt(new MSSpectrum(input[spec_col]));
						DateTime peTime = DateTime.Now;
						TimeSpan lap = new TimeSpan();

						if(count % UPDATE_INTERVAL == 0) {
							lap = DateTime.Now.Subtract(sTime);
							Console.WriteLine("Elapsed {2:F5}s, average {3:F5}ms, this item: {4:F5}ms - {0} [{1}]", input[id_col], count, lap.TotalSeconds, lap.TotalMilliseconds/(count+1), peTime.Subtract(psTime).TotalMilliseconds);
						}

						result.Append(hash).Append(",").Append(input[id_col]).Append(",").Append(input[spec_col]);
						//Console.WriteLine(hash);
						fout.WriteLine(String.Format(result.ToString()));
						result.Clear();

						eTime = DateTime.Now;
						stats.addTime(lap.TotalMilliseconds);
						count++;
					}

					fout.Flush();
				}
			}

			Console.WriteLine(stats.getTimeData());
		}
	}
}
	