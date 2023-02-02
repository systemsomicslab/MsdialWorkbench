using edu.ucdavis.fiehnlab.mona;
using System;
using System.Diagnostics;
using System.Net;
using System.Runtime.Serialization.Json;

namespace edu.ucdavis.fiehnlab.MonaRestApi
{
	public sealed class MonaRestProtocol
	{
		public static MonaResponse GetMonaResponse(string prolog)
		{
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

			var url = prolog;
			var req = WebRequest.Create(url);
			var res = getWebResponse(req);
			Console.WriteLine("INFO: Response length: " + res.ContentLength);
			if (res == null) return null;

			var monaResponse = getMonaResponse(res);

			if (monaResponse == null) return null;

			return monaResponse;
		}

		public static int GetSpectraCount(string query)
		{
			try {
				Console.WriteLine("Response: " + GetMonaResponse(query));
			} catch(Exception ex)
			{
				Console.WriteLine("ERROR: " + ex.Message);
			}
			return -1;
		}

		private static MonaResponse getMonaResponse(WebResponse res)
		{
			using (res)
			{
				using (var resStream = res.GetResponseStream())
				{
					var serializer = new DataContractJsonSerializer(typeof(MonaResponse));
					var jsonResponse = (MonaResponse)serializer.ReadObject(resStream);
					return jsonResponse;
				}
			}
		}

		private static WebResponse getWebResponse(WebRequest req)
		{
			WebResponse res = null;

			try
			{
				res = req.GetResponse();
			}
			catch (WebException ex)
			{
				Console.WriteLine("{0}: {1}", ex.Status, ex.Message);
				res = null;
			}

			return res;
		}
	}
}

