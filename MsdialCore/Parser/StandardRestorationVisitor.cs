using CompMs.Common.Components;
using CompMs.Common.Parser;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompMs.MsdialCore.Parser
{
    public class StandardRestorationVisitor : IRestorationVisitor
    {
        public StandardRestorationVisitor(ParameterBase parameter) {
            Parameter = parameter;
        }

        public ParameterBase Parameter { get; }

        public virtual IMatchResultRefer Visit(MspDbRestorationKey key) {
            var dbpath = Path.GetFullPath(Path.Combine(Parameter.ProjectFolderPath, key.DataBasePath));
            var db = new List<MoleculeMsReference>();
            if (File.Exists(dbpath)) {
                var ext = Path.GetExtension(dbpath);
                if (ext == ".msp" || ext == ".msp2") {
                    db = LibraryHandler.ReadMspLibrary(dbpath).OrderBy(msp => msp.PrecursorMz).ToList();
                }
                else if (ext == ".lbm" || ext == ".lbm2") {
                    db = LibraryHandler.ReadLipidMsLibrary(dbpath, Parameter).OrderBy(msp => msp.PrecursorMz).ToList();
                }
            }
            return new RestorableDataBaseRefer(db, dbpath);
        }

        public virtual IMatchResultRefer Visit(TextDbRestorationKey key) {
            var dbpath = Path.GetFullPath(Path.Combine(Parameter.ProjectFolderPath, key.DataBasePath));
            var db = new List<MoleculeMsReference>();
            if (File.Exists(dbpath)) {
                db = TextLibraryParser.TextLibraryReader(dbpath, out var _);
            }
            return new RestorableDataBaseRefer(db, dbpath);
        }
    }
}
