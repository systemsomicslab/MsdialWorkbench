using CompMs.Common.DataObj.Database;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Alignment;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialImmsCore.Algorithm.Alignment;
using CompMs.MsdialImmsCore.Parameter;
using System;
using System.Collections.Generic;

namespace CompMs.MsdialImmsCore.Algorithm
{
    public class ImmsProcessFactory : IProcessFactory
    {
        private readonly MsdialImmsParameter Parameter;
        private readonly IupacDatabase Iupac;

        public ImmsProcessFactory(MsdialImmsParameter parameter, IupacDatabase iupac) {
            Parameter = parameter;
            Iupac = iupac;
        }

        public AlignmentProcessFactory CreateAlignmentFactory() {
            return new ImmsAlignmentProcessFactory(Parameter, Iupac);
        }

        public IDataProvider CreateProvider(AnalysisFileBean file) {
            return new ImmsAverageDataProvider(file);
        }
    }
}
