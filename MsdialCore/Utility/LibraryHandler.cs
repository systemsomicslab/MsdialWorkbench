using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Common.Parser;
using CompMs.Common.Query;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.MsdialCore.Utility {
    public sealed class LibraryHandler {
        private LibraryHandler() { }

        public static List<MoleculeMsReference> ReadLipidMsLibrary(string filepath, ParameterBase param) {
            
            var container = param.LipidQueryContainer;
            var collosionType = container.CollisionType;
            var solventType = container.SolventType;
            
            var ionMode = param.IonMode;
            var queries = new List<LbmQuery>();

            foreach (var lQuery in container.LbmQueries) {
                if (lQuery.IsSelected == true && lQuery.IonMode == ionMode)
                    queries.Add(lQuery);
            }

            List<MoleculeMsReference> mspQueries = null;
            var extension = System.IO.Path.GetExtension(filepath).ToLower();
            if (extension == ".lbm")
                mspQueries = MspFileParser.LbmFileReader(filepath, queries, ionMode, solventType, collosionType);
            else if (extension == ".lbm2")
                mspQueries = MspFileParser.ReadSerializedLbmLibrary(filepath, queries, ionMode, solventType, collosionType);
            
            return mspQueries;
        }

        public static List<MoleculeMsReference> ReadMspLibrary(string filepath) {
            List<MoleculeMsReference> mspQueries = null;
            var extension = System.IO.Path.GetExtension(filepath).ToLower();
            if (extension.Contains("2"))
                mspQueries = MspFileParser.ReadSerializedMspObject(filepath);
            else
                mspQueries = MspFileParser.MspFileReader(filepath);

            return mspQueries;
        }
    }
}
