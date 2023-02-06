using edu.ucdavis.fiehnlab.MonaRestApi.model;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Security;

namespace edu.ucdavis.fiehnlab.MonaRestApi
{
	public interface IMonaRestClient
	{
		int GetSpectraCount(string query);

		MonaToken Login(string email, SecureString password);
		List<MonaSpectrum> UploadSpectra(List<MonaSpectrum> spectra, string authToken);
		List<MonaSpectrum> Search(string query);
		ObservableCollection<MonaSpectrum> DownloadSpectra(string query = "");
        Submitter GetSubmitterInfo(string submitterId, MonaToken token);

		List<Tag> GetCommonTags();
		List<string> GetMetadataNames();
        HashSet<string> GetSpecies();
        HashSet<string> GetOrgans();
    }
}
