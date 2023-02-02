using System.Runtime.Serialization;

namespace edu.ucdavis.fiehnlab.MonaRestApi.model {
    [DataContract]
    public class MetadataNameResponse {
        [DataMember(Name = "name")]
        public string Name { get; set; }
        [DataMember(Name = "count")]
        public int Count { get; set; }
    }
}
