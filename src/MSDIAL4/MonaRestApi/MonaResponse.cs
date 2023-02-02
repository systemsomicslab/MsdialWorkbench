using System.Collections.Generic;
using System.Runtime.Serialization;

namespace edu.ucdavis.fiehnlab.mona
{
	[DataContract]
    public class MonaResponse
    {
        [DataMember(Name = "count")]
        public string Count { get; set; }

        [DataMember(Name = "_embedded")]
        public Embedded Embedded { get; set; }

        [DataMember(Name = "total")]
        public string Total { get; set; }

        [DataMember(Name = "title")]
        public string Title { get; set; }

        [DataMember(Name = "_links")]
        public Links Links { get; set; }
    }

    [DataContract]
    public class Embedded
    {
        [DataMember(Name = "items")]
        public Items[] Items { get; set; }
    }

    [DataContract]
    public class Links
    {
        [DataMember(Name = "next")]
        public Next Next { get; set; }
    }

    [DataContract]
    public class Next
    {
        [DataMember(Name = "href")]
        public string Href;
    }


    [DataContract]
    public class Items
    {
        [DataMember(Name = "mediaType")]
        public string MediaType { get; set; }

        [DataMember(Name = "id")]
        public string Id { get; set; }

        [DataMember(Name = "metadata")]
        public Metadata Metadata { get; set; }

        [DataMember(Name = "title")]
        public string Title { get; set; }
    }

    [DataContract]
    public class Metadata
    {
        [DataMember(Name = "retention_time")]
        public string RetentionTime { get; set; }

        [DataMember(Name = "precursor_type")]
        public string PrecursorType { get; set; }

        [DataMember(Name = "ion_mode")]
        public string IonMode { get; set; }

        [DataMember(Name = "precursor_mz")]
        public string PrecursorMz { get; set; }

        [DataMember(Name = "origin_id")]
        public string OriginId { get; set; }

        [DataMember(Name = "formula")]
        public string Formula { get; set; }

        [DataMember(Name = "ms_type")]
        public string MsType { get; set; }

        [DataMember(Name = "peaks")]
        public List<double[]> Peaks { get; set; }

        [DataMember(Name = "title")]
        public string Title { get; set; }

        [DataMember(Name = "inchi_key")]
        public string InchiKey { get; set; }

    }
}
