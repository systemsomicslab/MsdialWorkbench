using Accord.Statistics.Distributions.DensityKernels;
using Accord.Statistics.Distributions.Multivariate;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.Common.Proteomics.Function {
    public class PeptideAnnotation {
        public MultivariateEmpiricalDistribution GetGussianKernelDistribution(double[][] data) {
            IDensityKernel kernel = new GaussianKernel(dimension: 2);
            var dist = new MultivariateEmpiricalDistribution(kernel, data);
            return dist;
        }

        
    }
}
