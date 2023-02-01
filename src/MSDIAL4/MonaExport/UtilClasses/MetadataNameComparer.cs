using edu.ucdavis.fiehnlab.MonaRestApi.model;
using System.Collections.Generic;
using System;

namespace edu.ucdavis.fiehnlab.MonaExport.UtilClasses {
    public class MetadataNameComparer : IEqualityComparer<MetaData> {
        public bool Equals(MetaData x, MetaData y) {
            return x.Name.Equals(y.Name);
        }

        public int GetHashCode(MetaData obj) {
            return obj.Name.GetHashCode();
        }
    }
}
