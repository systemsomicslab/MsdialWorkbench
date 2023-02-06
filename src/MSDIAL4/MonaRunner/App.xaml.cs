using System.Windows;
using System.Diagnostics;
using edu.ucdavis.fiehnlab.MonaExport.Windows;
using edu.ucdavis.fiehnlab.MonaRestApi.model;
using edu.ucdavis.fiehnlab.MonaExport.DataObjects;
using edu.ucdavis.fiehnlab.MonaExport.ViewModels;
using System.Windows.Threading;
using System.Collections.ObjectModel;
using System;

namespace MonaRunner {
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application {

        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e) {
			MessageBox.Show("An unhandled exception just occurred: " + e.Exception.Message, "Exception Sample", MessageBoxButton.OK, MessageBoxImage.Warning);
			e.Handled = true;
		}

		protected override void OnStartup(StartupEventArgs e) {

			var spec = new MonaSpectrum {
				compounds = new ObservableCollection<Compound> { new Compound() {
					inchiKey = "VAYOSLLFUXYJDT-UHFFFAOYSA-N",
					names = new ObservableCollection<Name> { new Name { name = "lsd" }, new Name { name = "lisergic acid" } },
					metaData = new ObservableCollection<MetaData>(),
					tags = new ObservableCollection<Tag>()
				} },
				tags = new ObservableCollection<Tag> { new Tag { text = "spec1" } },
				metaData = new ObservableCollection<MetaData> {
					new MetaData { Name ="accurate mass", Value="123.1234" },
					new MetaData { Name ="ms level", Value="MS" },
                    new MetaData { Name ="ionization mode", Value="Positive" },
                    new MetaData { Name ="ionization potential", Value="40ev" },
					new MetaData { Name ="precursor m/z", Value="147.1234" }
				},
				splash = new Splash { block1 = "splash10", block2 = "1df1", block3 = "7940000000", block4 = "c82697559a690c6121ea", splash = "splash10-1df1-7940000000-c82697559a690c6121ea" },
				spectrum = "53.0385:0.708585 54.0335:0.665758 55.0173:1.086237 59.0491:0.545065 60.0554:0.825384 65.0382:7.915126 66.0421:0.525599 67.0413:0.599572 68.049:18.189605 69.0331:1.031731 72.0439:1.203037 78.0338:1.911622 79.0173:2.40218 80.0498:1.537863 92.0496:19.026669 93.0575:20.611252 94.0648:2.347674 96.0448:1.38213 99.0562:15.939264 100.0585:0.856531 107.0613:1.27701 108.046:30.885731 109.0488:2.943352 110.0605:8.421258 111.0648:1.082344 119.0609:0.747518 120.0562:3.710337 131.0597:0.883784 132.0682:0.992797 133.0628:0.790345 146.071:6.684836 147.0791:19.260269 148.0864:5.283239 156.0119:100 157.0146:9.087016 158.0078:4.960093 160.0873:20.868211 161.0015:2.051781 161.0911:2.5073 172.087:0.552852 173.0594:0.747518 174.0211:1.12517 176.0288:1.34709 188.0822:13.60327 189.0855:1.26533 190.0981:3.340471 194.0379:3.145805 254.0603:25.999611 255.0624:2.57738 256.0566:0.985011"
			};
			//var spec1 = new MonaSpectrum {
			//	compounds = new ObservableCollection<Compound> { new Compound() {
			//		inchiKey = "BBBBBBBBBBBBBB-AAAAAAAAAA-A",
			//		names = new ObservableCollection<Name> { new Name { name = "alanine" }, new Name { name = "ALA" } },
			//		metaData = new ObservableCollection<MetaData>(),
			//		tags = new ObservableCollection<Tag> { new Tag { text = "asdasd" }, new Tag { text = "asdasdasdasd" } }
			//	} },
			//	tags = new ObservableCollection<Tag> { new Tag { text = "spec1" }, new Tag { text = "second spec tag" } },
			//	metaData = new ObservableCollection<MetaData> {
			//		new MetaData { name ="ms level", value="MS" },
			//		new MetaData { name ="ionization potential", value="40ev" },
			//		new MetaData { name ="precursor m/z", value="147.1234" }
			//	},
			//	splash = new Splash { block1 = "splash10", block2 = "1df1", block3 = "7940000000", block4 = "c82697559a690c6121ea", splash = "splash10-1df1-7940000000-c82697559a690c6121ea" },
			//	spectrum = "53.0385:0.708585 54.0335:0.665758 55.0173:1.086237 59.0491:0.545065 60.0554:0.825384 65.0382:7.915126 66.0421:0.525599 67.0413:0.599572 68.049:18.189605 69.0331:1.031731 72.0439:1.203037 78.0338:1.911622 79.0173:2.40218 80.0498:1.537863 92.0496:19.026669 93.0575:20.611252 94.0648:2.347674 96.0448:1.38213 99.0562:15.939264 100.0585:0.856531 107.0613:1.27701 108.046:30.885731 109.0488:2.943352 110.0605:8.421258 111.0648:1.082344 119.0609:0.747518 120.0562:3.710337 131.0597:0.883784 132.0682:0.992797 133.0628:0.790345 146.071:6.684836 147.0791:19.260269 148.0864:5.283239 156.0119:100 157.0146:9.087016 158.0078:4.960093 160.0873:20.868211 161.0015:2.051781 161.0911:2.5073 172.087:0.552852 173.0594:0.747518 174.0211:1.12517 176.0288:1.34709 188.0822:13.60327 189.0855:1.26533 190.0981:3.340471 194.0379:3.145805 254.0603:25.999611 255.0624:2.57738 256.0566:0.985011"
			//};
			//var spec2 = new MonaSpectrum {
			//	compounds = new ObservableCollection<Compound> { new Compound() {
			//		inchiKey = "CCCCCCCCCCCCCC-AAAAAAAAAA-A",
			//		names = new ObservableCollection<Name> { new Name { name = "necessary evil" }, new Name { name = "caffeine" } },
			//		metaData = new ObservableCollection<MetaData>(),
			//		tags = new ObservableCollection<Tag> { new Tag { text = "asdasd" }, new Tag { text = "asdasdasdasd" } }
			//	} },
			//	tags = new ObservableCollection<Tag> { new Tag { text = "spec1" }, new Tag { text = "second spec tag" } },
			//	metaData = new ObservableCollection<MetaData> {
			//		new MetaData { name ="ms level", value="MS" },
			//		new MetaData { name ="ionization potential", value="40ev" },
			//		new MetaData { name ="precursor m/z", value="147.1234" }
			//	},
			//	splash = new Splash { block1 = "splash10", block2="1df1", block3 = "7940000000", block4 = "c82697559a690c6121ea", splash = "splash10-1df1-7940000000-c82697559a690c6121ea" },
			//	spectrum = "53.0385:0.708585 54.0335:0.665758 55.0173:1.086237 59.0491:0.545065 60.0554:0.825384 65.0382:7.915126 66.0421:0.525599 67.0413:0.599572 68.049:18.189605 69.0331:1.031731 72.0439:1.203037 78.0338:1.911622 79.0173:2.40218 80.0498:1.537863 92.0496:19.026669 93.0575:20.611252 94.0648:2.347674 96.0448:1.38213 99.0562:15.939264 100.0585:0.856531 107.0613:1.27701 108.046:30.885731 109.0488:2.943352 110.0605:8.421258 111.0648:1.082344 119.0609:0.747518 120.0562:3.710337 131.0597:0.883784 132.0682:0.992797 133.0628:0.790345 146.071:6.684836 147.0791:19.260269 148.0864:5.283239 156.0119:100 157.0146:9.087016 158.0078:4.960093 160.0873:20.868211 161.0015:2.051781 161.0911:2.5073 172.087:0.552852 173.0594:0.747518 174.0211:1.12517 176.0288:1.34709 188.0822:13.60327 189.0855:1.26533 190.0981:3.340471 194.0379:3.145805 254.0603:25.999611 255.0624:2.57738 256.0566:0.985011"
			//};


			var author = new Submitter{firstName="test", lastName="user", id="test@mail.com" };

			var projectData = new MonaProjectData {Submitter = author };
			projectData.Spectra.Add(spec);
            //projectData.Spectra.Add(spec1);
            //projectData.Spectra.Add(spec2);
            try {
                var monaVM = new MonaExportWindowVM(projectData);

                var mona = new MonaExportWindow(monaVM);

                mona.Show();
            } catch (Exception ex) {
                MessageBox.Show(string.Format("There was an error opening the MonaExport window. Please inform the developers.\nError: {0}\nSource: {1}\nTarget: {2}", ex.Message, ex.Source, ex.TargetSite));
                Current.Shutdown(-1);
            }
        }
    }
}
