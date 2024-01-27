using CompMs.CommonMVVM;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialGcMsApi.Parameter;
using CompMs.MsdialImmsCore.Parameter;
using CompMs.MsdialLcImMsApi.Parameter;
using System.ComponentModel;

namespace CompMs.App.Msdial.Model.Setting
{
    public interface IPeakEqualityParameterSetting : INotifyPropertyChanged
    {
        float Tolerance { get; set; }
        float Factor { get; set; }

        void Commit();
        void Update(ParameterBase parameter);
    }

    public abstract class PeakEqualityParameterSetting : BindableBase
    {
        public PeakEqualityParameterSetting(float tolerance, float factor) {
            Tolerance = tolerance;
            Factor = factor;
        }

        public float Tolerance {
            get => tolerance;
            set => SetProperty(ref tolerance, value);
        }
        private float tolerance;

        public float Factor {
            get => factor;
            set => SetProperty(ref factor, value);
        }
        private float factor;
    }

    public class RetentionTimeEqualityParameterSetting : PeakEqualityParameterSetting, IPeakEqualityParameterSetting
    {
        private readonly AlignmentBaseParameter parameter;

        public RetentionTimeEqualityParameterSetting(AlignmentBaseParameter parameter) : base(parameter.RetentionTimeAlignmentTolerance, parameter.RetentionTimeAlignmentFactor) {
            this.parameter = parameter;
        }

        public void Commit() {
            parameter.RetentionTimeAlignmentTolerance = Tolerance;
            parameter.RetentionTimeAlignmentFactor = Factor;
        }

        public void Update(ParameterBase parameter) {
            Tolerance = parameter.AlignmentBaseParam.RetentionTimeAlignmentTolerance;
            Factor = parameter.AlignmentBaseParam.RetentionTimeAlignmentFactor;
        }
    }

    public class RetentionIndexEqualityParameterSetting : PeakEqualityParameterSetting, IPeakEqualityParameterSetting
    {
        private readonly MsdialGcmsParameter _parameter;

        public RetentionIndexEqualityParameterSetting(MsdialGcmsParameter parameter) : base(parameter.RetentionIndexAlignmentTolerance, parameter.RetentionTimeAlignmentFactor) {
            _parameter = parameter;
        }

        public void Commit() {
            _parameter.RetentionIndexAlignmentTolerance = Tolerance;
            _parameter.AlignmentBaseParam.RetentionTimeAlignmentFactor = Factor;
        }

        public void Update(ParameterBase parameter) {
            if (parameter is MsdialGcmsParameter gcms) {
                Tolerance = gcms.RetentionIndexAlignmentTolerance;
                Factor = gcms.AlignmentBaseParam.RetentionTimeAlignmentFactor;
            }
        }
    }

    public class DriftTimeEqualityParameterSetting : PeakEqualityParameterSetting, IPeakEqualityParameterSetting
    {
        private readonly MsdialLcImMsParameter? lcimmsParameter;
        private readonly MsdialImmsParameter? immsParameter;

        public DriftTimeEqualityParameterSetting(MsdialLcImMsParameter parameter) : base(parameter.DriftTimeAlignmentTolerance, parameter.DriftTimeAlignmentFactor) {
            lcimmsParameter = parameter;
        }

        public DriftTimeEqualityParameterSetting(MsdialImmsParameter parameter) : base(parameter.DriftTimeAlignmentTolerance, parameter.DriftTimeAlignmentFactor) {
            immsParameter = parameter;
        }

        public void Commit() {
            if (lcimmsParameter != null) {
                lcimmsParameter.DriftTimeAlignmentTolerance = Tolerance;
                lcimmsParameter.DriftTimeAlignmentFactor = Factor;
            }
            else if (immsParameter != null) {
                immsParameter.DriftTimeAlignmentTolerance = Tolerance;
                immsParameter.DriftTimeAlignmentFactor = Factor;
            }
        }

        public void Update(ParameterBase parameter) {
            switch (parameter) {
                case MsdialLcImMsParameter lcimms:
                    Tolerance = lcimms.DriftTimeAlignmentTolerance;
                    Factor = lcimms.DriftTimeAlignmentFactor;
                    break;
                case MsdialImmsParameter imms:
                    Tolerance = imms.DriftTimeAlignmentTolerance;
                    Factor = imms.DriftTimeAlignmentFactor;
                    break;
            }
        }
    }

    public class Ms1EqualityParameterSetting : PeakEqualityParameterSetting, IPeakEqualityParameterSetting
    {
        private readonly AlignmentBaseParameter parameter;

        public Ms1EqualityParameterSetting(AlignmentBaseParameter parameter) : base(parameter.Ms1AlignmentTolerance, parameter.Ms1AlignmentFactor) {
            this.parameter = parameter;
        }

        public void Commit() {
            parameter.Ms1AlignmentTolerance = Tolerance;
            parameter.Ms1AlignmentFactor = Factor;
        }

        public void Update(ParameterBase parameter) {
            Tolerance = parameter.AlignmentBaseParam.Ms1AlignmentTolerance;
            Factor = parameter.AlignmentBaseParam.Ms1AlignmentFactor;
        }
    }
}
