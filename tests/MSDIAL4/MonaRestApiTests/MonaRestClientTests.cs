using Microsoft.VisualStudio.TestTools.UnitTesting;
using edu.ucdavis.fiehnlab.MonaRestApi.client;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using edu.ucdavis.fiehnlab.MonaRestApi.model;
using System.IO;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using System.Security;
using System;
using System.Configuration;

namespace edu.ucdavis.fiehnlab.MonaRestApiTests {
    [TestClass]
    [Ignore]
    public class MonaRestClientTests {
        private readonly string USERNAME = "test@mail.com";
        private readonly string PASSWORD = "test";

        MonaClient mClient;

        [TestInitialize]
        public void setUp() {
            var mHost = ConfigurationManager.AppSettings.Get("monaHost");
            var bvHost = ConfigurationManager.AppSettings.Get("bvHost");
            Debug.WriteLine(string.Format("Config mona: {0}, binvestigate: {1}", mHost, bvHost));

            mClient = new MonaClient(mHost, bvHost);

        }

        [TestCleanup]
        public void cleanUp() {
            mClient = null;
        }

        [TestMethod]
        public void TestGetMonaResponseWithSpectraCount() {
            int res = mClient.GetSpectraCount();
            Debug.WriteLine("Count: " + res);

            Assert.IsTrue(0 < res, "Calling the spectra count endpoint should return a value > 0");
        }

        [TestMethod]
        public void TestLoginGoodCredentials() {
            var pw = new SecureString();
            Array.ForEach(PASSWORD.ToCharArray(), pw.AppendChar);

            var res = mClient.Login(USERNAME, pw);
            Debug.WriteLine("RESPONSE: " + res.Token);

            Assert.IsTrue(Regex.IsMatch(res.Token, "[a-z0-9]+"));
        }

        [TestMethod]
        [ExpectedException(typeof(WebException))]
        public void TestLoginWrongCredentials() {
            var res = new MonaToken();
            var pw = new SecureString();
            Array.ForEach("hacker".ToCharArray(), pw.AppendChar);

            res = mClient.Login("baduser", pw);

            Assert.IsNull(res);
        }

        [TestMethod]
        public void TestGetOrgans() {
            var res = new HashSet<string>();

            res = mClient.GetOrgans();

            Assert.IsNotNull(res);
            Assert.IsTrue(res.Count > 10);
        }

        [TestMethod]
        public void TestGetSpecies() {
            var res = new HashSet<string>();

            res = mClient.GetSpecies();

            Assert.IsNotNull(res);
            Assert.IsTrue(res.Count > 10);
        }

        //[TestMethod]
        //public void TestSearchSpectraUsingMongoSyntax()
        //{
        //	var res = client.Search("compound.names.name=='Benzyl benzoate'");
        //	//var res = client.Search("compound.names.name=='4-Butoxyphenol'");

        //	Assert.IsNotNull(res, "Response should not be null", "querying for spectra should return a json array");
        //	Assert.IsTrue(res.Count > 0, "Response should have at least 1 spectrum");
        //	Debug.WriteLineIf(res.Count > 0, string.Format("Found {0} spectrum/a", res.Count));

        //	var spec = res.Select(sp => sp.compounds.Select(cp => cp.names.Where(name => name.name == "Benzyl benzoate")));
        //	Assert.IsTrue(spec.Count() > 0, "There shoud be one spectrum with a compound named 'Benzyl benzoate'");
        //}

        //[TestMethod]
        //public void TestSearchSpectraUsingRSqlSyntax()
        //{
        //	var res = client.Search("compound.names=q='name==\"Benzyl benzoate\"'");

        //	Assert.IsNotNull(res, "Response should not be null", "querying for spectra should return a json array");
        //	Assert.IsTrue(res.Count > 0, "Response should have at least 1 spectrum");
        //	Debug.WriteLineIf(res.Count > 0, string.Format("Found {0} spectrum/a", res.Count));

        //	var spec = res.Select(sp => sp.compounds.Select(cp => cp.names.Where(name => name.name == "Benzyl benzoate")));
        //	Assert.IsTrue(spec.Count() > 0, "There shoud be one spectrum with a compound named 'Benzyl benzoate'");
        //}

        [TestMethod]
        public void TestAuthTokenPost() {
            var pw = new SecureString();
            Array.ForEach(PASSWORD.ToCharArray(), pw.AppendChar);
            var ti = mClient.Login(USERNAME, pw);

            var res = mClient.GetTokenInfo(ti.Token);
            Debug.WriteLine("RESPONSE: " + res);

            Assert.IsNotNull(res);
            Assert.IsInstanceOfType(res, typeof(TokenInfo));
        }

        [TestMethod]
        public void TestGetSubmitterInfo() {
            var pw = new SecureString();
            Array.ForEach(PASSWORD.ToCharArray(), pw.AppendChar);
            var tkn = mClient.Login(USERNAME, pw);

            var res = mClient.GetSubmitterInfo(USERNAME, tkn);
            Debug.WriteLine("Result: " + res);

            Assert.IsNotNull(res);
            Assert.IsInstanceOfType(res, typeof(Submitter));
            Assert.IsNotNull(res.firstName);
            Assert.AreEqual("test", res.firstName.ToLower());
        }

        [TestMethod]
        public void TestUploadSpectum() {
            var expInchiCode = "InChI=1S/H2/h1H";
            var expInchiKey = "HHHHHHHHHHHHHH-HHHHHHHHHH-H";
            var expName = "Hydrogen";

            var spec = File.ReadAllText(@"../../docs/spectrum.json");

            var spectrum = JArray.Parse(spec).ToObject<List<MonaSpectrum>>();

            var pw = new SecureString();
            Array.ForEach(PASSWORD.ToCharArray(), pw.AppendChar);
            var response = mClient.UploadSpectra(spectrum, mClient.Login(USERNAME, pw).Token);

            Debug.WriteLine(string.Format("RESPONSE: {0}", response.ToString()));

            Assert.IsTrue(response.Count > 0);
            Assert.IsTrue(response[0].metaData.Where(md => md.Name.Equals("accurate mass")).First().Value == "123.123456");

            var ret = response[0].compounds[0].inchi;
            Assert.IsTrue(expInchiCode.Equals(ret), "Values not equals\nExpected: " +
                expInchiCode + " -- Returned: " + ret);

            ret = response[0].compounds[0].inchiKey;
            Assert.IsTrue(expInchiKey == ret, "Values not equals\nExpected: " +
                expInchiKey + " -- Returned: " + ret);

            ret = response[0].compounds[0].names[0].name;
            Assert.IsTrue(expName == ret, "Values not equals\nExpected: " +
                expName + " -- Returned: " + ret);

        }

        [TestMethod]
        public void TestUploadManySpectra() {
            var expInchiCode = "InChI=1S/H2/h1H";
            var expInchiKey = "HHHHHHHHHHHHHH-HHHHHHHHHH-H";
            var expName = "Hydrogen";

            var spec = File.ReadAllText(@"../../docs/spectra.json");

            var spectra = JArray.Parse(spec).ToObject<List<MonaSpectrum>>();

            var pw = new SecureString();
            Array.ForEach(PASSWORD.ToCharArray(), pw.AppendChar);
            var response = mClient.UploadSpectra(spectra, mClient.Login(USERNAME, pw).Token);

            Assert.IsTrue(response.Count == 3);

            var ret = response[0].compounds[0].inchi;
            Assert.IsTrue(expInchiCode.Equals(ret), "Values not equals\nExpected: " +
                expInchiCode + " -- Returned: " + ret);

            ret = response[0].compounds[0].inchiKey;
            Assert.IsTrue(expInchiKey == ret, "Values not equals\nExpected: " +
                expInchiKey + " -- Returned: " + ret);

            ret = response[0].compounds[0].names[0].name;
            Assert.IsTrue(expName == ret, "Values not equals\nExpected: " +
                expName + " -- Returned: " + ret);

        }

        [TestMethod]
        public void TestUploadEmptySpectum() {
            var spec = File.ReadAllText(@"../../docs/spectrum.json");

            var spectrum = new List<MonaSpectrum>();

            var pw = new SecureString();
            Array.ForEach(PASSWORD.ToCharArray(), pw.AppendChar);
            var response = mClient.UploadSpectra(spectrum, mClient.Login(USERNAME, pw).Token);

            Assert.IsTrue(response.Count == 0);
        }

        [TestMethod]
        public void TestGetMetadataNames() {
            var metadata = mClient.GetMetadataNames();
            Debug.WriteLine("Got {0} metadata items", metadata.Count);
            Assert.IsTrue(metadata.Count > 0);
        }

        [TestMethod]
        public void TestGetCommonTags() {
            var tags = mClient.GetCommonTags();
            Debug.WriteLine("Got {0} tags", tags.Count);
            tags.ForEach(t => Debug.WriteLine(t));
            Assert.IsTrue(tags.Count > 0);
        }
    }
}
