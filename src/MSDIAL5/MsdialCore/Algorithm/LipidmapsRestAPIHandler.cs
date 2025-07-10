using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.Algorithm;

/// <summary>
/// Provides methods to interact with the LipidMaps database via REST API calls.
/// </summary>
/// <remarks>
/// This class is designed to facilitate the retrieval of lipid information from the LIPIDMAPS database.
/// It constructs and sends HTTP requests to the LIPIDMAPS REST API and processes the responses. The base URI for the
/// API can be customized through the constructor.
/// </remarks>
public sealed class LipidmapsRestAPIHandler
{
    private static readonly HttpClient _client = new();
    private static readonly string _compoundAbbrev = "/rest/compound/abbrev/";
    private static readonly string _compoundAbbrevWithChains = "/rest/compound/abbrev_chains/";
    private static readonly string _lipidPage = "/databases/lmsd/";
    private static readonly string _pubchemCompoundPage = "https://pubchem.ncbi.nlm.nih.gov/compound/";

    private string _baseUri = "https://www.lipidmaps.org";

    public LipidmapsRestAPIHandler(string? baseUri = null) {
        if (baseUri is not null) {
            _baseUri = baseUri;
        }
    }

    /// <summary>
    /// Asynchronously retrieves lipid information from the LipidMaps database.
    /// </summary>
    /// <remarks>
    /// This method sends a request to the LipidMaps database to retrieve information about the specified lipid.
    /// It constructs the request URI using the lipid name and expects a JSON response containing lipid abbreviations.
    /// If the lipid name is not available, the systematic name will be used instead.
    /// </remarks>
    /// <param name="rawlipid">The raw lipid string to search for in the database.</param>
    /// <param name="token">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>An array of tuples, each containing the lipid name (or systematic name if unavailable), a <see cref="Uri"/> to the LIPIDMAPS page, and a <see cref="Uri"/> to the PubChem page. The array will
    /// be empty if no lipids are found or if an error occurs.</returns>
    public async Task<(string? LipidName, Uri? LipidmapsPage, Uri? PubChemPage)[]> RetrieveLipidsAsync(string rawlipid, CancellationToken token) {
        var (lipid, abbrev) = GetLipidNameAndAnnotationLevel(rawlipid);
        var builder = new UriBuilder(_baseUri)
        {
            Path = $"{abbrev}{lipid}/name,sys_name,lm_id,pubchem_cid/json"
        };
        Uri requestUri = builder.Uri;

        try {
            HttpResponseMessage response = await _client.GetAsync(requestUri, token).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            var lipidAbbreviations = Deserialize(responseBody);

            var result = new (string?, Uri?, Uri?)[lipidAbbreviations.Count];
            var idx = 0;
            foreach (var item in lipidAbbreviations) {
                result[idx++] = (
                    !string.IsNullOrEmpty(item.Name) ? item.Name : item.SysName,
                    item.LmId is not null ? new Uri($"{_baseUri}{_lipidPage}{item.LmId}") : null,
                    item.PubChemCID is not null ? new Uri($"{_pubchemCompoundPage}{item.PubChemCID}") : null
                );
            }
            return result;
        }
        catch {
            return [];
        }
    }

    /// <summary>
    /// Deserializes the JSON response from the LipidMaps database.
    /// </summary>
    /// <param name="responseBody">The JSON response body as a string.</param>
    /// <returns>A list of <see cref="LipidAbbreviation"/> objects representing lipid abbreviations.</returns>
    private List<LipidAbbreviation> Deserialize(string responseBody) {
        try {
            return [.. JsonConvert.DeserializeObject<Dictionary<string, LipidAbbreviation>>(responseBody).Values];
        }
        catch (JsonSerializationException) {
            return [JsonConvert.DeserializeObject<LipidAbbreviation>(responseBody)];
        }
    }

    /// <summary>
    /// Retrieves the lipid name and its annotation level based on the provided lipid string.
    /// </summary>
    /// <remarks>
    /// If the lipid string contains a '|', the method processes the substring after the last
    /// occurrence of '|'. Otherwise, it processes the entire lipid string.
    /// </remarks>
    /// <param name="lipid">The lipid string to process, which may contain a delimiter '|'.</param>
    /// <returns>
    /// A tuple containing the processed lipid name,
    /// and the corresponding annotation level URI.
    /// </returns>
    private (string lipidName, string abbrev_uri) GetLipidNameAndAnnotationLevel(string lipid) {
        if (lipid.Contains('|')) {
            lipid = lipid.Split('|').Last();
            return (Uri.EscapeDataString(lipid), _compoundAbbrevWithChains);
        }
        else {
            return (Uri.EscapeDataString(lipid), _compoundAbbrev);
        }
    }

    /// <summary>
    /// Represents an abbreviation for a lipid, including its name, LIPIDMAPS identifier, systematic name, and PubChem CID.
    /// </summary>
    class LipidAbbreviation
    {
        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("lm_id")]
        public string? LmId { get; set; }

        [JsonProperty("sys_name")]
        public string? SysName { get; set; }

        [JsonProperty("pubchem_cid")]
        public string PubChemCID { get; set; }
    }
}
