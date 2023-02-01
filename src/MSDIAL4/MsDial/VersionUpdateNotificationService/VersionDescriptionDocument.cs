using System;
using System.Runtime.Serialization;

/* Author: rfmejia */
namespace Rfx.Riken.OsakaUniv
{
    [DataContract]
    class VersionDescriptionDocument
    {
        [DataMember(Name = "latestVersion")]
        public string LatestVersion { get; set; }

        [DataMember(Name = "changeLog")]
        public string ChangeLog { get; set; }
        
        [DataMember(Name = "datePublished")]
        public DateTime DatePublished { get; set; }
        
        [DataMember(Name = "downloadUri")]
        public Uri DownloadUri { get; set; }
    }
}
