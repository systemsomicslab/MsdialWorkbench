using System;
using System.Collections.Generic;
using edu.ucdavis.fiehnlab.MonaRestApi;
using edu.ucdavis.fiehnlab.MonaRestApi.model;
using System.Collections.ObjectModel;
using System.Security;

namespace edu.ucdavis.fiehnlab.MonaExport.UtilClasses {
    class DesignTimeMonaClient : IMonaRestClient {
		public ObservableCollection<MonaSpectrum> DownloadSpectra(string query = "") {
			var specs = new ObservableCollection<MonaSpectrum> {
				new MonaSpectrum {
					spectrum="53.0385:0.708585 54.0335:0.665758 55.0173:1.086237 59.0491:0.545065 60.0554:0.825384 65.0382:7.915126 66.0421:0.525599 67.0413:0.599572 68.049:18.189605 69.0331:1.031731 72.0439:1.203037 78.0338:1.911622 79.0173:2.40218 80.0498:1.537863 92.0496:19.026669 93.0575:20.611252 94.0648:2.347674 96.0448:1.38213 99.0562:15.939264 100.0585:0.856531 107.0613:1.27701 108.046:30.885731 109.0488:2.943352 110.0605:8.421258 111.0648:1.082344 119.0609:0.747518 120.0562:3.710337 131.0597:0.883784 132.0682:0.992797 133.0628:0.790345 146.071:6.684836 147.0791:19.260269 148.0864:5.283239 156.0119:100 157.0146:9.087016 158.0078:4.960093 160.0873:20.868211 161.0015:2.051781 161.0911:2.5073 172.087:0.552852 173.0594:0.747518 174.0211:1.12517 176.0288:1.34709 188.0822:13.60327 189.0855:1.26533 190.0981:3.340471 194.0379:3.145805 254.0603:25.999611 255.0624:2.57738 256.0566:0.985011",
					splash= new Splash { block1="splash10", block2="dz40000000", block3="a82697559a690c6121ea", splash="splash10-dz40000000-a82697559a690c6121ea"},
					compounds = new ObservableCollection<Compound> {
						new Compound {
							inchi="InChI=1S/H2/h1H", inchiKey="HHHHHHHHHHHHHH-HHHHHHHHHH-H",
							molFile = "\n783\n  -OEChem-04271616422D\n\n  2  1  0     0  0  0  0  0  0999 V2000\n    2.0000    0.0000    0.0000 H   0  0  0  0  0  0  0  0  0  0  0  0\n    3.0000    0.0000    0.0000 H   0  0  0  0  0  0  0  0  0  0  0  0\n  1  2  1  0  0  0  0\nM  END\n",
							names = new ObservableCollection<Name> {
								new Name { computed=false, name="Hydrogen", score=10.0, source="cts" }
							},
							metaData = new ObservableCollection<MetaData> {
                                new MetaData { Name="kind", Value="standard" },
								new MetaData { Name="PubChem CID", Value="783" },
								new MetaData { Name="PubChem SID", Value="12" }
							},
							tags= new ObservableCollection<Tag>(),
							computed=false
						}
					},
					metaData = new ObservableCollection<MetaData> {
						new MetaData {Name="fragmentation mode", Value="CID"},
						new MetaData {Name="ion mode", Value="positive"},
						new MetaData {Name="ionization",Value="ESI"},
						new MetaData {Name="ms level",Value="MS2"}
					},
					submitter = new Submitter { emailAddress="test@mail.com", firstName="Test", lastName="Tester", institution="Testing Inc."},
					tags=new ObservableCollection<Tag> {
						new Tag {ruleBased=false, text="test tag1"},
						new Tag {ruleBased=false, text="test tag2"},
					}
				},
				new MonaSpectrum {
					spectrum="53.0385:0.708585 54.0335:0.665758 55.0173:1.086237 59.0491:0.545065 60.0554:0.825384 65.0382:7.915126 66.0421:0.525599 67.0413:0.599572 68.049:18.189605 69.0331:1.031731 72.0439:1.203037 78.0338:1.911622 79.0173:2.40218 80.0498:1.537863 92.0496:19.026669 93.0575:20.611252 94.0648:2.347674 96.0448:1.38213 99.0562:15.939264 100.0585:0.856531 107.0613:1.27701 108.046:30.885731 109.0488:2.943352 110.0605:8.421258 111.0648:1.082344 119.0609:0.747518 120.0562:3.710337 131.0597:0.883784 132.0682:0.992797 133.0628:0.790345 146.071:6.684836 147.0791:19.260269 148.0864:5.283239 156.0119:100 157.0146:9.087016 158.0078:4.960093 160.0873:20.868211 161.0015:2.051781 161.0911:2.5073 172.087:0.552852 173.0594:0.747518 174.0211:1.12517 176.0288:1.34709 188.0822:13.60327 189.0855:1.26533 190.0981:3.340471 194.0379:3.145805 254.0603:25.999611 255.0624:2.57738 256.0566:0.985011",
					splash= new Splash { block1="splash10", block2="dz40000000", block3="a82697559a690c6121ea", splash="splash10-dz40000000-a82697559a690c6121ea"},
					compounds = new ObservableCollection<Compound> {
						new Compound {
							inchi="InChI=1S/H2/h1H", inchiKey="HHHHHHHHHHHHHH-HHHHHHHHHH-H",
							molFile = "\n783\n  -OEChem-04271616422D\n\n  2  1  0     0  0  0  0  0  0999 V2000\n    2.0000    0.0000    0.0000 H   0  0  0  0  0  0  0  0  0  0  0  0\n    3.0000    0.0000    0.0000 H   0  0  0  0  0  0  0  0  0  0  0  0\n  1  2  1  0  0  0  0\nM  END\n",
							names = new ObservableCollection<Name> {
								new Name { computed=false, name="Hydrogen", score=10.0, source="cts" }
							},
							metaData = new ObservableCollection<MetaData> {
								new MetaData { Name="PubChem CID", Value="783" },
								new MetaData { Name="PubChem SID", Value="12" }
							},
							tags = new ObservableCollection<Tag>(),
							computed = false
						}
					},
					metaData = new ObservableCollection<MetaData> {
						new MetaData {Name="fragmentation mode", Value="CID"},
						new MetaData {Name="ion mode", Value="positive"},
						new MetaData {Name="ionization",Value="ESI"},
						new MetaData {Name="ms type",Value="MS2"}
					},
                    submitter = new Submitter { emailAddress="test@mail.com", firstName="Test", lastName="Tester", institution="Testing Inc."},
                    tags=new ObservableCollection<Tag> {
						new Tag {ruleBased=false, text="linux test"},
						new Tag {ruleBased=false, text="h-h"},
					}
				},
				new MonaSpectrum {
					spectrum="53.0385:0.708585 54.0335:0.665758 55.0173:1.086237 59.0491:0.545065 60.0554:0.825384 65.0382:7.915126 66.0421:0.525599 67.0413:0.599572 68.049:18.189605 69.0331:1.031731 72.0439:1.203037 78.0338:1.911622 79.0173:2.40218 80.0498:1.537863 92.0496:19.026669 93.0575:20.611252 94.0648:2.347674 96.0448:1.38213 99.0562:15.939264 100.0585:0.856531 107.0613:1.27701 108.046:30.885731 109.0488:2.943352 110.0605:8.421258 111.0648:1.082344 119.0609:0.747518 120.0562:3.710337 131.0597:0.883784 132.0682:0.992797 133.0628:0.790345 146.071:6.684836 147.0791:19.260269 148.0864:5.283239 156.0119:100 157.0146:9.087016 158.0078:4.960093 160.0873:20.868211 161.0015:2.051781 161.0911:2.5073 172.087:0.552852 173.0594:0.747518 174.0211:1.12517 176.0288:1.34709 188.0822:13.60327 189.0855:1.26533 190.0981:3.340471 194.0379:3.145805 254.0603:25.999611 255.0624:2.57738 256.0566:0.985011",
					splash= new Splash { block1="splash10", block2="dz40000000", block3="a82697559a690c6121ea", splash="splash10-dz40000000-a82697559a690c6121ea"},
					compounds = new ObservableCollection<Compound> {
						new Compound {
							inchi="InChI=1S/H2/h1H", inchiKey="HHHHHHHHHHHHHH-HHHHHHHHHH-H",
							molFile = "\n783\n  -OEChem-04271616422D\n\n  2  1  0     0  0  0  0  0  0999 V2000\n    2.0000    0.0000    0.0000 H   0  0  0  0  0  0  0  0  0  0  0  0\n    3.0000    0.0000    0.0000 H   0  0  0  0  0  0  0  0  0  0  0  0\n  1  2  1  0  0  0  0\nM  END\n",
							names = new ObservableCollection<Name> {
								new Name { computed=false, name="Hydrogen", score=10.0, source="cts" }
							},
							metaData = new ObservableCollection<MetaData> {
								new MetaData { Name="PubChem CID", Value="783" },
								new MetaData { Name="PubChem SID", Value="12" }
							},
							tags= new ObservableCollection<Tag>(),
							computed=false
						}
					},
					metaData = new ObservableCollection<MetaData> {
						new MetaData {Name="fragmentation mode", Value="CID"},
						new MetaData {Name="ion mode", Value="positive"},
						new MetaData {Name="ionization",Value="ESI"},
						new MetaData {Name="ms type",Value="MS2"}
					},
                    submitter = new Submitter { emailAddress="test@mail.com", firstName="Test", lastName="Tester", institution="Testing Inc."},
                    tags=new ObservableCollection<Tag> {
						new Tag {ruleBased=false, text="linux test"},
						new Tag {ruleBased=false, text="h-h"},
					}
				}

			};

			return specs;
		}

		public List<Tag> GetCommonTags() {
			return new List<Tag> { new Tag { text = "LCMS" },
				new Tag { text = "massbank" },
				new Tag { text = "LC-MS" },
				new Tag { text = "noisy spectra" },
				new Tag { text = "Noisy Spectrum" },
				new Tag { text = "GCMS" },
				new Tag { text = "has M-15" },
				new Tag { text = "GC-MS" },
				new Tag { text = "should be derivatized" },
				new Tag { text = "virtually derivatized compound" }
			};
		}

        public Submitter GetSubmitterInfo(string id, MonaToken t) {
            return new Submitter() { id = "test@mail.com", emailAddress = "test@mail.com", firstName = "test", lastName = "user", institution = "this" };
        }

		public List<string> GetMetadataNames() {
			return new List<string> {
				"accession",
				"activation parameter",
				"activation time",
				"analytical time",
				"atom gun current",
				"authors",
				"automatic gain control",
				"base peak",
				"bombardment",
				"capillary temperature",
				"capillary voltage",
				"cdl side octopoles bias voltage",
				"cdl temperature",
				"collision energy",
				"collision gas",
				"column name",
				"column pressure",
				"column temperature",
				"compound class",
				"copyright"
			};
		}

        public HashSet<string> GetSpecies() {
            return new HashSet<string>() {
                "human",
                "rat",
                "cow",
                "plant"
            };
        }

        public HashSet<string> GetOrgans() {
            return new HashSet<string>() {
                "lung",
                "brain",
                "plasma",
                "liver"
            };
        }

		public int GetSpectraCount(string query) {
			return 1;
		}

		public MonaToken Login(string email, SecureString password) {
			return new MonaToken { Token = "the.real-token_goes.here" };
		}

		public List<MonaSpectrum> Search(string query) {
			return new List<MonaSpectrum>();
		}

		public List<MonaSpectrum> UploadSpectra(List<MonaSpectrum> spectra, string authToken) {
			throw new NotImplementedException();
		}
	}
}
