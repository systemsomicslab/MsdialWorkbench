using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.MSDec
{
    public sealed class MSDecResultCollection
    {
        private readonly List<MSDecResult> _mSDecResults;
        private readonly double _collisionEnergy;

        public MSDecResultCollection(List<MSDecResult> mSDecResults, double collisionEnergy) {
            _collisionEnergy = collisionEnergy;
            _mSDecResults = mSDecResults;
            MSDecResults = _mSDecResults.AsReadOnly();
        }

        public double CollisionEnergy => _collisionEnergy;
        public ReadOnlyCollection<MSDecResult> MSDecResults { get; }

        public Task SerializeAsync(AnalysisFileBean analysisFile, CancellationToken token = default) {
            return Task.Run(() => MsdecResultsWriter.Write(analysisFile.DeconvolutionFilePath, _mSDecResults), token);
        }

        public Task SerializeWithCEAsync(AnalysisFileBean analysisFile, CancellationToken token = default) {
            return Task.Run(() => {
                var dclfile = analysisFile.DeconvolutionFilePath;
                var suffix = Math.Round(_collisionEnergy * 100, 0); // CE 34.50 -> 3450
                var dclfile_suffix = Path.Combine(Path.GetDirectoryName(dclfile),  $"{Path.GetFileNameWithoutExtension(dclfile)}_{suffix}.dcl");
                MsdecResultsWriter.Write(dclfile_suffix, _mSDecResults);
                analysisFile.DeconvolutionFilePathList.Add(dclfile_suffix);
            }, token);
        }

        public static List<Task<MSDecResultCollection>> DeserializeAsync(AnalysisFileBean analysisFile, CancellationToken token = default) {
            var results = new List<Task<MSDecResultCollection>>();
            var files = analysisFile.DeconvolutionFilePathList;
            if (files.Count <= 1) {
                var result = Task.Run(() =>
                {
                    var r = MsdecResultsReader.ReadMSDecResults(analysisFile.DeconvolutionFilePath, out var _, out var _);
                    return new MSDecResultCollection(r, -1d);
                }, token);
                results.Add(result);
            }
            else {
                var baseDcl = Path.GetFileNameWithoutExtension(analysisFile.DeconvolutionFilePath);
                foreach (var file in files) {
                    var name = Path.GetFileNameWithoutExtension(file);
                    if (name.StartsWith(baseDcl)) {
                        var ceStr = name.Substring(baseDcl.Length + 1); // {baseDcl}_{ce}
                        if (double.TryParse(ceStr, out var ce)) {
                            var result = Task.Run(() =>
                            {
                                token.ThrowIfCancellationRequested();
                                var r = MsdecResultsReader.ReadMSDecResults(file, out var _, out var _);
                                return new MSDecResultCollection(r, ce / 100); // 3450 -> CE 34.50
                            }, token);
                            results.Add(result);
                        }
                    }
                }
            }
            return results;
        }
    }
}
