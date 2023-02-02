using System;
using System.Runtime.Serialization;

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
        public string DatePublished { get; set; }

        [DataMember(Name = "downloadUri")]
        public Uri DownloadUri { get; set; }
    }
}
