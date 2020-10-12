using System;
using System.Collections.Generic;
using System.Text;

namespace Rfx.Riken.OsakaUniv
{
    public sealed class PostIdentificationReferenceDataRetrieve
    {
        public static string GetOntology(int id, List<PostIdentificatioinReferenceBean> postIdentificationTxtDB) {
            if (postIdentificationTxtDB == null || postIdentificationTxtDB.Count - 1 < id || id < 0) return string.Empty;
            else if (string.IsNullOrEmpty(postIdentificationTxtDB[id].Ontology)) return string.Empty;
            else return postIdentificationTxtDB[id].Ontology;
        }
    }
}
