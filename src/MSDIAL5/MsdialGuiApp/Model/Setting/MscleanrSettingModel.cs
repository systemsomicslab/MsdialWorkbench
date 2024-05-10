using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Mathematics.Basic;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialLcmsApi.Parameter;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CompMs.App.Msdial.Model.Setting {
    public class MscleanrSettingModel : BindableBase {
        public bool BlankRatioChecked {
            get => blankRatioChecked;
            set => SetProperty(ref blankRatioChecked, value);
        }
        private bool blankRatioChecked;


        public double BlankRatioMinimum {
            get => blankRatioMinimum;
            set => SetProperty(ref blankRatioMinimum, value);
        }
        private double blankRatioMinimum;

        public bool DeleteGhostPeaksChecked {
            get => deleteGhostPeaksChecked;
            set => SetProperty(ref deleteGhostPeaksChecked, value);
        }
        private bool deleteGhostPeaksChecked;

        public bool IncorrectMassChecked {
            get => incorrectMassChecked;
            set => SetProperty(ref incorrectMassChecked, value);
        }
        private bool incorrectMassChecked;

        public bool RSDChecked {
            get => rsdChecked;
            set => SetProperty(ref rsdChecked, value);
        }
        private bool rsdChecked;

        public double RSDMaximum {
            get => rsdMaximum;
            set => SetProperty(ref rsdMaximum, value);
        }
        private double rsdMaximum;

        public bool RMDChecked {
            get => rmdChecked;
            set => SetProperty(ref rmdChecked, value);
        }
        private bool rmdChecked;

        public double RMDMinimum {
            get => rmdMinimum;
            set => SetProperty(ref rmdMinimum, value);
        }
        private double rmdMinimum;

        public double RMDMaximum {
            get => rmdMaximum;
            set => SetProperty(ref rmdMaximum, value);
        }

        private readonly MsdialLcmsParameter _parameter;
        private readonly ReadOnlyObservableCollection<AlignmentSpotPropertyModel> _spotprops;
        //public MsdialLcmsParameter Parameter { get; }
        //public ObservableCollection<AlignmentSpotPropertyModel> Spotprops { get; }

        private double rmdMaximum;

        //private readonly ParameterBase _parameter;
        //private readonly IReadOnlyList<AlignmentSpotProperty> _spots;

        //public MscleanrSettingModel(ParameterBase parameter, IReadOnlyList<AlignmentSpotProperty> spots) {
        //    _parameter = parameter;
        //    _spots = spots;
        //}

        public MscleanrSettingModel(MsdialLcmsParameter parameter, ReadOnlyObservableCollection<AlignmentSpotPropertyModel> spotprops) {
            _parameter = parameter;
            _spotprops = spotprops;
        }

        public void RunMscleanrFilter() {
            var ghosts = new List<double>();
            var postcurparam = _parameter.PostCurationParameter;
            Console.WriteLine("just check where I'm in ....");
            postcurparam.IsBlankFilter = BlankRatioChecked;
            postcurparam.IsBlankGhostFilter = DeleteGhostPeaksChecked;
            postcurparam.IsMzFilter = IncorrectMassChecked;
            postcurparam.IsRmdFilter = RMDChecked;
            postcurparam.IsRsdFilter = RSDChecked;
            postcurparam.FilterBlankThreshold = BlankRatioMinimum;
            postcurparam.FilterRsdThreshold = RSDMaximum;
            postcurparam.FilterMinRmdThreshold = RMDMinimum;
            postcurparam.FilterMaxRmdThreshold = RMDMaximum;

            // process
            //foreach (var spot in _spots) {
            foreach (var spot in _spotprops) {
                spot.IsBlankFilteredByPostCurator = false;
                spot.IsBlankGhostFilteredByPostCurator = false;
                spot.IsMzFilteredByPostCurator = false;
                spot.IsRsdFilteredByPostCurator = false;
                spot.IsRmdFilteredByPostCurator = false;

                var blankProps = spot.AlignedPeakPropertiesModelProperty.Value?.Where(x => _parameter.FileID_AnalysisFileType[x.FileID] == AnalysisFileType.Blank).ToList();
                var isBlankDataAvailable = blankProps.IsEmptyOrNull() ? false : true;
                var avgBlank = isBlankDataAvailable ? blankProps.Average(x => x.PeakHeightTop) : 0.0;

                var qcProps = spot.AlignedPeakPropertiesModelProperty.Value?.Where(x => _parameter.FileID_AnalysisFileType[x.FileID] == AnalysisFileType.QC).ToList();
                var isQcDataAvailable = qcProps.IsEmptyOrNull() ? false : true;
                var avgQC = isQcDataAvailable ? qcProps.Average(x => x.PeakHeightTop) : 0.0;

                var sampleProps = spot.AlignedPeakPropertiesModelProperty.Value?.Where(x => _parameter.FileID_AnalysisFileType[x.FileID] == AnalysisFileType.Sample).ToList();
                var isSampleDataAvailable = sampleProps.IsEmptyOrNull() ? false : true;
                var avgSample = isSampleDataAvailable ? sampleProps.Average(x => x.PeakHeightTop) : 0.0;

                if (postcurparam.IsBlankFilter && isBlankDataAvailable && (isQcDataAvailable || isSampleDataAvailable)) {
                    var ratioBlank = avgBlank / Math.Max(avgQC, avgSample);
                    if (ratioBlank >= postcurparam.FilterBlankThreshold) {
                        spot.IsBlankFilteredByPostCurator = true;
                        Console.WriteLine("A spot is filtered by MS-CleanR blank filter!!");
                        ghosts.Add(Math.Round(spot.MassCenter, 3));
                    }
                }

                if (postcurparam.IsMzFilter) {
                    if (spot.MassCenter - Math.Floor(spot.MassCenter) >= (spot.MassCenter * 0.00111 + 0.039)) {
                        Console.WriteLine("A spot is filtered by MS-CleanR MZ filter!!");
                        spot.IsMzFilteredByPostCurator = true;
                    }
                }

                if (postcurparam.IsRsdFilter) {
                    var classToHeights = new Dictionary<string, List<double>>();
                    var classToRsd = new Dictionary<string, double>();
                    foreach (var class_ in _parameter.FileID_ClassName.Values.Distinct()) {
                        classToHeights[class_] = new List<double>();
                    }
                    foreach (var props in spot.AlignedPeakPropertiesModelProperty.Value.OrEmptyIfNull()) {
                        if (_parameter.FileID_AnalysisFileType[props.FileID] != AnalysisFileType.Blank) {
                            classToHeights[_parameter.FileID_ClassName[props.FileID]].Add(props.PeakHeightTop);
                        }
                    }
                    foreach (var class_ in classToHeights.Keys) {
                        if (classToHeights[class_].Any()) {
                            var avg = BasicMathematics.Mean(classToHeights[class_]);
                            var sd = BasicMathematics.Stdev(classToHeights[class_]);
                            classToRsd[class_] = sd * 100 / avg;
                        }
                    }
                    if (BasicMathematics.Min(classToRsd.Values.ToArray()) >= postcurparam.FilterRsdThreshold) {
                        spot.IsRsdFilteredByPostCurator = true;
                        Console.WriteLine("A spot is filtered by MS-CleanR RSD filter!!");
                    }
                }

                if (postcurparam.IsRmdFilter) {
                    var rmd = (spot.MassCenter - Math.Floor(spot.MassCenter)) / spot.MassCenter * 1000000;
                    if (rmd < postcurparam.FilterMinRmdThreshold | rmd > postcurparam.FilterMaxRmdThreshold) {
                        spot.IsRmdFilteredByPostCurator = true;
                        Console.WriteLine("A spot is filtered by MS-CleanR RMD filter!!");
                    }
                }

                //if (postcurparam.IsRsdFilter) {
                //    foreach (scriptclass in param.FileID_ClassName)
                //}

                // filtering process
                #region
                // var mz = spot.MassCenter;
                // var rt = spot.TimesCenter.RT.Value;
                // var alignedPeaks = spot.AlignedPeakProperties;

                // foreach (var peak in alignedPeaks) {
                //     var intensity = peak.PeakHeightTop;
                //     var area = peak.PeakAreaAboveZero;
                //     var mzOfPeak = peak.Mass;
                //     var fileID = peak.FileID;

                //     var fileObj = files[fileID];
                //     var fileType = fileObj.AnalysisFileType;
                //     if (fileType == Common.Enum.AnalysisFileType.Blank) {

                //     }

                // }
                #endregion                

                // if (frag == true) {
                //     spot.IsFilteredByPostCurator = true;
                // }

            }

            foreach (var spot in _spotprops) {
                if (postcurparam.IsBlankFilter && postcurparam.IsBlankGhostFilter) {
                    if (spot.IsBlankFilteredByPostCurator == false) {
                        if (ghosts.Contains(Math.Round(spot.MassCenter, 3))) {
                            spot.IsBlankGhostFilteredByPostCurator = true;
                            Console.WriteLine("A spot is filtered by MS-CleanR Blank Ghost filter!!");
                        }
                    }
                }
            }

        }
    }
}
