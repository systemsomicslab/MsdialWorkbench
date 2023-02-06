using System;
using System.Net;
using System.Diagnostics;
using System.Collections.Generic;
using edu.ucdavis.fiehnlab.MonaRestApi.model;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Linq;
using System.Collections.ObjectModel;
using System.Windows;
using System.Security;
using System.Runtime.InteropServices;
using System.IO;

namespace edu.ucdavis.fiehnlab.MonaRestApi.client {
    public class MonaClient : IMonaRestClient {
        private readonly string REST_PATH_ROOT = "rest";
        private readonly string REST_PATH_LOGIN = "rest/auth/login";
		private readonly string REST_PATH_AUTHINFO = "rest/auth/info";
		private readonly string REST_PATH_SPECTRUM = "rest/spectra";
        private readonly string REST_PATH_SUBMITTERS = "rest/submitters";
        private List<string> excludedMetaDataNames = new List<string>() { "adductionaccuratemass", "adduct ion accurate mass", "last auto-curation" };

        private List<ClassificationsResponse> classification = new List<ClassificationsResponse>();

		WebClient client = new WebClient();
        BinvestigateClient bvClient;

		public MonaClient(string mHost, string bvHost) {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            Debug.WriteLine("Mona Client base address: " + mHost);
            client.BaseAddress = Path.Combine("https://", mHost);

            bvClient = new BinvestigateClient(bvHost);
		}

		/// <summary>
		/// counts the number of spectra, optionally matching the specified query
		/// </summary>
		/// <param name="query">conditions to filter the spectra before counting (rSql format)</param>
		/// <returns>the number of spectra found</returns>
		public int GetSpectraCount(string query = "") {
			int res = getSpectraCountsync(query);
			return res;
		}

		/// <summary>
		/// Executes a query
		/// </summary>
		/// <param name="query"></param>
		/// <returns></returns>
		public List<MonaSpectrum> Search(string query = "") {
			client.Headers[HttpRequestHeader.ContentType] = "application/json; charset=UTF-8";
			client.Headers[HttpRequestHeader.Accept] = "application/json";

			var response = "";
			var res = new List<MonaSpectrum>();
			try {
				Debug.WriteLine("Calling Mona-REST API: " + client.BaseAddress + REST_PATH_SPECTRUM + "/search?query=" + query);

				response = client.DownloadString(REST_PATH_SPECTRUM + "/search?query=" + query);
				Debug.WriteLineIf(response != null, "Got response: " + response);

				res = JArray.Parse(response).ToObject<List<MonaSpectrum>>();
				Debug.WriteLine("Objects in response: " + res.Count() + " -- Type of objects in response: " + res.First().GetType().Name);
			} catch (JsonReaderException ex) {
				Debug.WriteLine("error: " + ex.Message);
				Debug.WriteLineIf(ex.Data.Count > 0, "data: (" + ex.Data.Count + ")");
				foreach (var d in ex.Data) {
					Debug.WriteLine("\t" + d);
				}
			} catch (WebException ex) {
				Debug.WriteLine("client: " + client.BaseAddress + REST_PATH_SPECTRUM + "/search?query=" + query);
				Debug.WriteLine("Error message: " + ex.Message);
				Debug.WriteLine("Error data: " + ex.Data);
			}

			return res;
		}


		/// <summary>
		/// Requests an Authentication token for the given credentials
		/// </summary>
		/// <param name="email"></param>
		/// <param name="password"></param>
		/// <returns></returns>
		public MonaToken Login(string email, SecureString password) {
            if (password == null) return null;
			client.Headers[HttpRequestHeader.ContentType] = "application/json; charset=UTF-8";
			client.Headers[HttpRequestHeader.Accept] = "application/json";

			Debug.WriteLine("Trying to login at url: " + client.BaseAddress);

			var parseToken = "";
			var token = new MonaToken();
			JObject data = JObject.FromObject(new { username = email, password = PasswordHelper.ConvertToUnsecureString(password) });

			parseToken = client.UploadString(REST_PATH_LOGIN, data.ToString());
            Debug.WriteLine("Login response: " + parseToken);
			token = JObject.Parse(parseToken).ToObject<MonaToken>();

			Debug.WriteLine("Login successful: " + token);
			return token;
		}

        /// <summary>
        /// Gets the submitter information for the passed in login name
        /// </summary>
        /// <param name="submitterId">Login username</param>
        /// <param name="token">Authorization token after login in</param>
        /// <returns>A MonaSubmitter object with the submitter Information</returns>
        public Submitter GetSubmitterInfo(string submitterId, MonaToken token) {
            client.Headers[HttpRequestHeader.ContentType] = "application/json; charset=UTF-8";
            client.Headers[HttpRequestHeader.Accept] = "application/json";
            client.Headers[HttpRequestHeader.Authorization] = "Bearer " + token.Token;

            Debug.WriteLine("Trying to login at url: " + client.BaseAddress);

            var data = client.DownloadString(REST_PATH_SUBMITTERS + "/" + submitterId);
            Submitter submitter = JObject.Parse(data).ToObject<Submitter>();

            return submitter;
        }

		/// <summary>
		/// retrieves details about the current authentication token, current username, validity time frame and rules.
		/// </summary>
		/// <param name="token">the authentication token string</param>
		/// <returns>a TokenInfo object with data about the token</returns>
		public TokenInfo GetTokenInfo(string token) {
			Debug.WriteLine("Token: " + token);
			client.Headers[HttpRequestHeader.ContentType] = "application/json; charset=UTF-8";
			client.Headers[HttpRequestHeader.Accept] = "application/json";
			client.Headers[HttpRequestHeader.Authorization] = "Bearer " + token;

			MonaToken data = new MonaToken { Token = token };

			var res = client.UploadString(REST_PATH_AUTHINFO, JObject.FromObject(data).ToString());
			Debug.WriteLine("(CheckToken) token info: " + res);

			TokenInfo ti = JObject.Parse(res).ToObject<TokenInfo>();

			return ti;
		}

		/// <summary>
		/// Sends a spectrun to the Mona database
		/// </summary>
		/// <param name="spectra">The list of spectra to upload</param>
		/// <param name="authToken">Authorization token. To get it first Login</param>
		/// <returns>True if the submission succeeded or false in case of an error</returns>
		public List<MonaSpectrum> UploadSpectra(List<MonaSpectrum> spectra, string authToken) {
			var results = new List<MonaSpectrum>();

			try {
				var res = "";
				spectra.ForEach(spec => {
					client.Headers[HttpRequestHeader.ContentType] = "application/json;charset=UTF-8";
					client.Headers[HttpRequestHeader.Accept] = "application/json";
					client.Headers[HttpRequestHeader.Authorization] = "Bearer " + authToken;

                    Debug.WriteLine("request: " + spec);
                    var jsonSpec = JObject.FromObject(spec).ToString();
					res = client.UploadString(REST_PATH_SPECTRUM, jsonSpec);
                    Debug.WriteLine("response: " + res);

                    results.Add(JObject.Parse(res).ToObject<MonaSpectrum>());
                    Debug.WriteLine("result: " + results.First());
                });
			} catch (UriFormatException ex) {
				Debug.WriteLineIf(ex.Data.Count > 0, "Exception data: " + ex.Data);
				foreach (var key in ex.Data.Keys) {
					Debug.WriteLine(string.Format("\t{0} = {1}", key, ex.Data[key]));
				}
				Debug.WriteLine("Exception 1: " + ex.Source);
				Debug.WriteLine("Exception Message: " + ex.Message);

				//saveExceptionFile(client, spectra, authToken);
			} catch (WebException ex) {
				Debug.WriteLine("Exception Message: " + ex.Message);
				Debug.WriteLine("Exception inner: " + ex.InnerException);
				//saveExceptionFile(client, spectra, authToken);
			} catch (Exception ex) {
				Debug.WriteLine("Exception type: " + ex.GetType().Name);
				Debug.WriteLine("Exception Message: " + ex.Message);
				Debug.WriteLine("Exception inner: " + ex.InnerException);
				//saveExceptionFile(client, spectra, authToken);
			}

			return results;
		}

		void alert(object sender, UploadStringCompletedEventArgs a) {
			Debug.WriteLine("uploaded " + a.GetType().Name);
		}

		/*
		 * Downloads a single spectrum, AKA executes a query
		 */
		public ObservableCollection<MonaSpectrum> DownloadSpectra(string query = "") {
			return (new ObservableCollection<MonaSpectrum>(Search(query)));
		}

		/*
		 * Counts the number of spectra in the database
		 */
		private int getSpectraCountsync(string query = "") {
			client.Headers[HttpRequestHeader.ContentType] = "application/json; charset=UTF-8";
			client.Headers[HttpRequestHeader.Accept] = "application/json";

			var resp = client.DownloadString(REST_PATH_SPECTRUM + "/count");

			return int.Parse(resp);
		}

		private void saveExceptionFile(WebClient client, List<MonaSpectrum> spectra, string token) {
			throw new NotImplementedException();
		}

		public List<Tag> GetCommonTags() {

			client.Headers[HttpRequestHeader.ContentType] = "application/json; charset=UTF-8";
			client.Headers[HttpRequestHeader.Accept] = "application/json";

			var query = "/tags";

			var response = "";
			var tags = new List<Tag>();
			try {
				response = client.DownloadString(REST_PATH_ROOT + query);
				var tmptags = JArray.Parse(response).ToObject<List<ResponseTag>>();
				tags.AddRange(tmptags.Where(t => t.Category==null || !t.Category.Equals("library")).ToList().ConvertAll(t => new Tag(t.Text)).ToList());
			} catch (JsonReaderException ex) {
				Debug.WriteLine("error: " + ex.Message);
				Debug.WriteLineIf(ex.Data.Count > 0, "data: (" + ex.Data.Count + ")");
				foreach (var d in ex.Data) {
					Debug.WriteLine("\t" + d);
				}
				MessageBox.Show("There was an error getting tags\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Asterisk);
			} catch (WebException ex) {
				Debug.WriteLine("client: " + client.BaseAddress + REST_PATH_ROOT + query);
				Debug.WriteLine("Error message: " + ex.Message);
				Debug.WriteLine("Error data: " + ex.Data);
				MessageBox.Show("Can't contact the MoNA server\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Asterisk);
			}

			return tags;
		}

		public List<string> GetMetadataNames() {

			client.Headers[HttpRequestHeader.ContentType] = "application/json; charset=UTF-8";
			client.Headers[HttpRequestHeader.Accept] = "application/json";

			var mdNames = new List<MetadataNameResponse>();
			var query = "rest/metaData/names";
			try {
				var response = client.DownloadString(query);
				mdNames = JArray.Parse(response).ToObject<List<MetadataNameResponse>>().Where(md => !excludedMetaDataNames.Contains(md.Name.ToLower())).ToList();
			} catch (JsonReaderException ex) {
				Debug.WriteLine("error: " + ex.Message);
				Debug.WriteLineIf(ex.Data.Count > 0, "data: (" + ex.Data.Count + ")");
				foreach (var d in ex.Data) {
					Debug.WriteLine("\t" + d);
				}
				MessageBox.Show("There was an error getting metadata\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Asterisk);
			} catch (WebException ex) {
				Debug.WriteLine("client: " + client.BaseAddress + REST_PATH_ROOT + query);
				Debug.WriteLine("Error message: " + ex.Message);
				Debug.WriteLine("Error data: " + ex.Data);
				MessageBox.Show("Can't contact the MoNA server\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Asterisk);
			}

            // return list of names sorted by count (descending) then alphabetically
            return mdNames
                //.OrderByDescending(md => md.Count)
                //.ThenBy(md => md.Name)
                .OrderBy(i => i.Name)
                .Select(md => md.Name.ToLower())
                .ToList<string>();
		}

        public HashSet<string> GetSpecies() {
            if (classification.Count <= 0) { 
                classification = bvClient.getClassification();
                Debug.WriteLine("Got " + classification.Count + " classifications");
            }
            try {
                HashSet<string> species = new HashSet<string>();
                foreach (string specie in classification.Where(it => it.Species != "").Select(it => it.Species).Distinct()) {
                    species.Add(specie);
                }
                Debug.WriteLine(string.Format("found ({0}) species", species.Count));
                return species;
            } catch (Exception e) {
                return new HashSet<string>() {
                    "human",
                    "rat",
                    "cow",
                    "plant"
                };
            }
        }

        public HashSet<string> GetOrgans() {
            if (classification.Count <= 0) {
                classification = bvClient.getClassification();
                Debug.WriteLine("Got " + classification.Count + " classifications");
            }

            try {
                HashSet<string> organs = new HashSet<string>();
                foreach (string organ in classification.Where(it => it.Organ != "").Select(it => it.Organ).Distinct()) {
                    organs.Add(organ);
                }
                Debug.WriteLine(string.Format("found ({0}) organs", organs.Count));
                return organs;
            } catch (Exception e) {
                return new HashSet<string>() {
                    "lung",
                    "brain",
                    "plasma",
                    "liver"
                };
            }
        }

        //private async int getSpectraCountAsync(string query = "")
        //{
        //	Debug.WriteLine("Calling spectra count (async): query= " + query != "" ? query : "empty");

        //	HttpClient client = new HttpClient();
        //	string url = BASE_API_URL + REST_PATH_SPECTRUM + "/count";

        //	var response = client.GetAsync(url);
        //	var res = await response.Conten

        //	return -1;
        //}
    }

	public static class PasswordHelper {
		public static string ConvertToUnsecureString(this SecureString securePassword) {
            if (securePassword == null) {
                MessageBox.Show("Enter username and password correctly.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
                //throw new ArgumentNullException("securePassword");
            }

			IntPtr unmanagedString = IntPtr.Zero;
			try {
				unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(securePassword);
				return Marshal.PtrToStringUni(unmanagedString);
			} finally {
				Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
			}
		}
	}
}
