using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Rfx.Riken.OsakaUniv
{
    [DataContract]
    public class PubResponse
    {
        [DataMember(Name = "Waiting")]
        public Waiting Waiting { get; set; }

        [DataMember(Name = "IdentifierList")]
        public IdentifierList IdentifierList { get; set; }

        [DataMember(Name = "PropertyTable")]
        public PropertyTable PropertyTable { get; set; }

        [DataMember(Name = "InformationList")]
        public InformationList InformationList { get; set; }
    }

    [DataContract]
    public class Waiting
    {
        [DataMember(Name = "ListKey")]
        public string ListKey { get; set; }
        [DataMember(Name = "Message")]
        public string Message { get; set; }
    }

    [DataContract]
    public class Properties
    {
        [DataMember(Name = "CID")]
        public string CID { get; set; }
        [DataMember(Name = "MolecularFormula")]
        public string MolecularFormula { get; set; }
        [DataMember(Name = "InChIKey")]
        public string InChIKey { get; set; }

        public int PubMedID { get; set; }
        public int SynonymsNumber { get; set; }
    }

    [DataContract]
    public class InformationList
    {
        [DataMember(Name = "Information")]
        public List<Information> Information { get; set; }

        public InformationList() { Information = new List<Information>(); }
    }

    [DataContract]
    public class Information
    {
        [DataMember(Name = "CID")]
        public string CID { get; set; }
        [DataMember(Name = "PubMedID")]
        public string[] PubMedID { get; set; }
        [DataMember(Name = "RegistryID")]
        public string[] RegistryID { get; set; }
        [DataMember(Name = "Synonym")]
        public string[] Synonym { get; set; }
    }

    [DataContract]
    public class PropertyTable
    {
        [DataMember(Name = "Properties")]
        public List<Properties> Properties { get; set; }

        public PropertyTable() { Properties = new List<Properties>(); }
    }

    [DataContract]
    public class IdentifierList
    {
        [DataMember(Name = "CID")]
        public List<int> CID { get; set; }
    }
}
