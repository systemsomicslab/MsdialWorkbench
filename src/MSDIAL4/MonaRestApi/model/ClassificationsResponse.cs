using System;
using System.Runtime.Serialization;

namespace edu.ucdavis.fiehnlab.MonaRestApi.model {
    [DataContract]
    public class ClassificationsResponse {
        [IgnoreDataMember]
        private string Id { get; set; }
        [DataMember]
        public string Species { get; set; }
        [DataMember]
        public string Organ { get; set; }
    }
}
