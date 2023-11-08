using CompMs.Common.Algorithm.Function;
using CompMs.Common.DataObj.NodeEdge;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.Algorithm
{
    public sealed class CytoscapeMolecularNetworkClient
    {
        private readonly HttpClient _client;
        private readonly MolecularNetworkInstance _instance;
        private int _networkSUID;

        public CytoscapeMolecularNetworkClient(MolecularNetworkInstance instance, string url)
        {
            _instance = instance;
            _client = new HttpClient();
            var baseUri = new Uri(url);
            _ = Task.Run(async () => {
                HttpResponseMessage response;
                string content;
                var json = ToCyjs(instance);
                using (var jsonContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json")) {
                    response = await _client.PostAsync(new Uri(baseUri, "v1/networks"), jsonContent).ConfigureAwait(false);
                    content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    System.Diagnostics.Debug.WriteLine($"networks: {content}");
                }

                var r = JsonConvert.DeserializeObject<Response>(content);
                _networkSUID = r.networkSUID;
                response = await _client.GetAsync(new Uri(baseUri, $"v1/apply/layouts/force-directed/{_networkSUID}")).ConfigureAwait(false);
                content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                System.Diagnostics.Debug.WriteLine($"layouts: {content}");

                using (var jsonContent = new StringContent("[{ \"mappingType\": \"passthrough\", \"mappingColumn\": \"backgroundcolor\", \"mappingColumnType\": \"String\", \"visualProperty\": \"NODE_FILL_COLOR\" }]", System.Text.Encoding.UTF8, "application/json")) {
                    response = await _client.PostAsync(new Uri(baseUri, "v1/styles/default/mappings"), jsonContent).ConfigureAwait(false);
                    content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    System.Diagnostics.Debug.WriteLine($"visual property: {content}");
                }
            });
        }

        private static string ToCyjs(MolecularNetworkInstance instance) {
            object NodeTo(Node node) {
                return new {
                    data = new {
                        id = node.data.id.ToString(),
                        node.data.segment,
                        node.data.position, 
                        node.data.Name, 
                        node.data.Comment, 
                        node.data.Title, 
                        node.data.Property, 
                        node.data.Ontology, 
                        node.data.Method, 
                        node.data.Rt, 
                        node.data.Ri, 
                        node.data.Mz, 
                        node.data.Adduct, 
                        node.data.IonMode, 
                        node.data.Formula, 
                        node.data.InChiKey, 
                        node.data.Smiles, 
                        node.data.MsMin, 
                        node.data.MsmsMin, 
                        node.data.MsLabel, 
                        node.data.MsMsLabel, 
                        node.data.Size, 
                        node.data.backgroundcolor,
                        node.data.bordercolor,
                        MSMz = node.data.MS.Select(p => p[0]).ToArray(),
                        MSIntensity = node.data.MS.Select(p => p[1]).ToArray(),
                        MSMSMz = node.data.MSMS.Select(p => p[0]).ToArray(),
                        MSMSIntensity = node.data.MSMS.Select(p => p[1]).ToArray(),
                        ChartType = node.data.BarGraph.type,
                        ChartData = node.data.BarGraph.data.datasets[0].data,
                        ChartLabel = node.data.BarGraph.data.labels,
                    },
                    node.classes,
                };
            }
            var id = instance.Root.nodes.Select(node => node.data.id).DefaultIfEmpty().Max();
            object EdgeTo(Edge edge) {
                return new {
                    data = new {
                        id = (++id).ToString(),
                        source = edge.data.source.ToString(),
                        target = edge.data.target.ToString(),
                        edge.data.sourceName,
                        edge.data.targetName,
                        edge.data.score,
                        edge.data.matchpeakcount,
                        edge.data.linecolor,
                        edge.data.comment,
                    },
                    edge.classes,
                };
            }
            var root = new {
                elements = new {
                    nodes = instance.Root.nodes.Select(NodeTo).ToArray(),
                    edges = instance.Root.edges.Select(EdgeTo).ToArray(),
                }
            };
            return JsonConvert.SerializeObject(root);
        }

        class Response {
            public int networkSUID { get; set; }
        }
    }
}
