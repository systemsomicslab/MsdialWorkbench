using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialImmsCore.Parameter;
using CompMs.MsdialLcImMsApi.Parameter;
using Reactive.Bindings.Extensions;
using System;
using System.ComponentModel;

namespace CompMs.App.Msdial.Model.Setting
{
    public interface IDataCollectionRangeSetting : INotifyPropertyChanged
    {
        float Begin { get; set; }
        float End { get; set; }

        bool NeedAccumulation { get; }
        float AccumulatedRange { get; set; }

        void Commit();
        void Update(ParameterBase parameter);
    }

    public abstract class DataCollectionRangeSetting : BindableBase
    {
        public DataCollectionRangeSetting(bool needAccumulation) {
            NeedAccumulation = needAccumulation;
        }

        public float Begin {
            get => begin;
            set => SetProperty(ref begin, value);
        }
        private float begin;

        public float End {
            get => end;
            set => SetProperty(ref end, value);
        }
        private float end;

        public bool NeedAccumulation { get; }

        public float AccumulatedRange {
            get => accumulatedRange;
            set => SetProperty(ref accumulatedRange, value);
        }
        private float accumulatedRange;
    }

    public class RetentionTimeCollectionRangeSetting : DataCollectionRangeSetting, IDataCollectionRangeSetting
    {
        private readonly MsdialLcImMsParameter? lcImMsParameter;

        public RetentionTimeCollectionRangeSetting(PeakPickBaseParameterModel peakPickBaseParameterModel, bool needAccmulation) : base(needAccmulation) {
            Begin = peakPickBaseParameterModel.RetentionTimeBegin;
            End = peakPickBaseParameterModel.RetentionTimeEnd;
            this.ObserveProperty(p => p.Begin).Subscribe(v => peakPickBaseParameterModel.RetentionTimeBegin = v);
            this.ObserveProperty(p => p.End).Subscribe(v => peakPickBaseParameterModel.RetentionTimeEnd = v);
            peakPickBaseParameterModel.ObserveProperty(p => p.RetentionTimeBegin).Subscribe(v => Begin = v);
            peakPickBaseParameterModel.ObserveProperty(p => p.RetentionTimeEnd).Subscribe(v => End = v);
        }

        public RetentionTimeCollectionRangeSetting(MsdialLcImMsParameter parameter, PeakPickBaseParameterModel peakPickBaseParameterModel, bool needAccmulation) : this(peakPickBaseParameterModel, needAccmulation) {
            AccumulatedRange = parameter.AccumulatedRtRange;
            lcImMsParameter = parameter;
        }

        public void Commit() {
            if (lcImMsParameter != null) {
                lcImMsParameter.AccumulatedRtRange = AccumulatedRange;
            }
        }

        public void Update(ParameterBase parameter) {
            Begin = parameter.PeakPickBaseParam.RetentionTimeBegin;
            End = parameter.PeakPickBaseParam.RetentionTimeEnd;
            if (parameter is MsdialLcImMsParameter p) {
                AccumulatedRange = p.AccumulatedRtRange;
            }
        }
    }

    public class DriftTimeCollectionRangeSetting : DataCollectionRangeSetting, IDataCollectionRangeSetting
    {
        private readonly MsdialLcImMsParameter? lcImMsParameter;
        private readonly MsdialImmsParameter? immsParameter;

        public DriftTimeCollectionRangeSetting(MsdialLcImMsParameter parameter, bool needAccmulation) : base(needAccmulation) {
            Begin = parameter.DriftTimeBegin;
            End = parameter.DriftTimeEnd;
            lcImMsParameter = parameter;
        }

        public DriftTimeCollectionRangeSetting(MsdialImmsParameter parameter, bool needAccmulation) : base(needAccmulation) {
            Begin = parameter.DriftTimeBegin;
            End = parameter.DriftTimeEnd;
            immsParameter = parameter;
        }

        public void Commit() {
            if (lcImMsParameter != null) {
                lcImMsParameter.DriftTimeBegin = Begin;
                lcImMsParameter.DriftTimeEnd = End;
            }
            else if (immsParameter != null) {
                immsParameter.DriftTimeBegin = Begin;
                immsParameter.DriftTimeEnd = End;
            }
        }

        public void Update(ParameterBase parameter) {
            switch (parameter) {
                case MsdialImmsParameter imms:
                    Begin = imms.DriftTimeBegin;
                    End = imms.DriftTimeEnd;
                    break;
                case MsdialLcImMsParameter lcimms:
                    Begin = lcimms.DriftTimeBegin;
                    End = lcimms.DriftTimeEnd;
                    break;
            }
        }
    }

    public class Ms1CollectionRangeSetting : DataCollectionRangeSetting, IDataCollectionRangeSetting
    {
        public Ms1CollectionRangeSetting(PeakPickBaseParameterModel peakPickBaseParameterModel, bool needAccmulation) : base(needAccmulation) {
            Begin = peakPickBaseParameterModel.MassRangeBegin;
            End = peakPickBaseParameterModel.MassRangeEnd;
            this.ObserveProperty(p => p.Begin).Subscribe(v => peakPickBaseParameterModel.MassRangeBegin = v);
            this.ObserveProperty(p => p.End).Subscribe(v => peakPickBaseParameterModel.MassRangeEnd = v);
            peakPickBaseParameterModel.ObserveProperty(p => p.MassRangeBegin).Subscribe(v => Begin = v);
            peakPickBaseParameterModel.ObserveProperty(p => p.MassRangeEnd).Subscribe(v => End = v);
        }

        public void Commit() {

        }

        public void Update(ParameterBase parameter) {
            Begin = parameter.PeakPickBaseParam.MassRangeBegin;
            End = parameter.PeakPickBaseParam.MassRangeEnd;
        }
    }

    public class Ms2CollectionRangeSetting : DataCollectionRangeSetting, IDataCollectionRangeSetting
    {
        public Ms2CollectionRangeSetting(PeakPickBaseParameterModel peakPickBaseParameterModel, bool needAccmulation) : base(needAccmulation) {
            Begin = peakPickBaseParameterModel.Ms2MassRangeBegin;
            End = peakPickBaseParameterModel.Ms2MassRangeEnd;
            this.ObserveProperty(p => p.Begin).Subscribe(v => peakPickBaseParameterModel.Ms2MassRangeBegin = v);
            this.ObserveProperty(p => p.End).Subscribe(v => peakPickBaseParameterModel.Ms2MassRangeEnd = v);
            peakPickBaseParameterModel.ObserveProperty(p => p.Ms2MassRangeBegin).Subscribe(v => Begin = v);
            peakPickBaseParameterModel.ObserveProperty(p => p.Ms2MassRangeEnd).Subscribe(v => End = v);
        }

        public void Commit() {

        }

        public void Update(ParameterBase parameter) {
            Begin = parameter.PeakPickBaseParam.Ms2MassRangeBegin;
            End = parameter.PeakPickBaseParam.Ms2MassRangeEnd;
        }
    }
}
