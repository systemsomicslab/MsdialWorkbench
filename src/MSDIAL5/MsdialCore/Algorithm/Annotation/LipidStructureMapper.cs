using CompMs.Common.DataObj.Result;
using CompMs.Common.Lipidomics;
using System.Collections.Concurrent;

namespace CompMs.MsdialCore.Algorithm.Annotation
{
    public sealed class LipidStructureMapper
    {
        private readonly ConcurrentDictionary<MsScanMatchResult, ILipid> _map = new ConcurrentDictionary<MsScanMatchResult, ILipid>();

        public ILipid Map(MsScanMatchResult result) {
            if (_map.TryGetValue(result, out var lipid)) {
                return lipid;
            }
            var estimatedName = result.Name.Split('|')[0];
            lipid = FacadeLipidParser.Default.Parse(estimatedName);
            if (lipid is null) {
                return null;
            }
            _map.TryAdd(result, lipid);
            return lipid;
        }

        public void Add(MsScanMatchResult result, ILipid lipid) {
            _map.TryAdd(result, lipid);
        }

        public void Remove(MsScanMatchResult result) {
            _map.TryRemove(result, out _);
        }

        public void Clear() {
            _map.Clear();
        }
    }
}
