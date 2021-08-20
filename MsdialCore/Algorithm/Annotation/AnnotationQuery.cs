using CompMs.Common.DataObj.Property;
using CompMs.Common.Interfaces;
using CompMs.Common.Parameter;
using System;
using System.Collections.Generic;

namespace CompMs.MsdialCore.Algorithm.Annotation
{
    public interface IAnnotationQuery
    {
        IMSIonProperty Property { get; }
        IMSScanProperty Scan { get; }
        IReadOnlyList<IsotopicPeak> Isotopes { get; }
        MsRefSearchParameterBase Parameter { get; }
    }

    public class AnnotationQuery : IAnnotationQuery
    {
        public IMSIonProperty Property { get; }
        public IMSScanProperty Scan { get; }
        public IReadOnlyList<IsotopicPeak> Isotopes { get; }
        public MsRefSearchParameterBase Parameter { get; }

        public AnnotationQuery(
            IMSIonProperty property,
            IMSScanProperty scan,
            IReadOnlyList<IsotopicPeak> isotopes,
            MsRefSearchParameterBase parameter) {
            if (property is null) {
                throw new ArgumentNullException(nameof(property));
            }

            if (scan is null) {
                throw new ArgumentNullException(nameof(scan));
            }
            Property = property;
            Scan = scan;
            Isotopes = isotopes;
            Parameter = parameter;
        }
    }
}
