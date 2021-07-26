using CompMs.Common.Interfaces;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.Algorithm.Annotation;
using System;

namespace CompMs.App.Msdial.Model.DataObj
{
    class Annotator
    {
        public Annotator(IAnnotator<IMSIonProperty, IMSScanProperty> annotatorImpl, MsRefSearchParameterBase parameter) {
            AnnotatorImpl = annotatorImpl;
            Parameter = parameter;
        }

        public IAnnotator<IMSIonProperty, IMSScanProperty> AnnotatorImpl { get; }

        public MsRefSearchParameterBase Parameter { get; }
    }
}
