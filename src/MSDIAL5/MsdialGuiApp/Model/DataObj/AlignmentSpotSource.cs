using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using System;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.DataObj
{
    public sealed class AlignmentSpotSource : IDisposable
    {
        private readonly AlignmentFileBeanModel _alignmentFile;
        private readonly AlignmentResultContainer _container;
        private readonly ChromatogramSerializer<ChromatogramSpotInfo> _chromatogramSerializer;
        private AlignmentSpotPropertyModelCollection? _spots;
        private bool _disposedValue;

        public AlignmentSpotSource(AlignmentFileBeanModel alignmentFile, AlignmentResultContainer container, ChromatogramSerializer<ChromatogramSpotInfo> chromatogramSerializer) {
            _alignmentFile = alignmentFile ?? throw new ArgumentNullException(nameof(alignmentFile));
            _container = container ?? throw new ArgumentNullException(nameof(container));
            _spots = new AlignmentSpotPropertyModelCollection(container.AlignmentSpotProperties);
            _chromatogramSerializer = chromatogramSerializer ?? throw new ArgumentNullException(nameof(chromatogramSerializer));
        }

        public AlignmentSpotPropertyModelCollection? Spots => _spots;

        public async Task DuplicateSpotAsync(AlignmentSpotPropertyModel spot) {
            if (spot is null || _spots is null) {
                return;
            }
            var mSDecResult = await _alignmentFile.LoadMSDecResultByIndexAsync(spot.MasterAlignmentID).ConfigureAwait(false);
            if (mSDecResult is null) {
                return;
            }
            _spots.Duplicates(spot);
            var spotInfo = await _alignmentFile.LoadEicInfoByIndexAsync(spot.MasterAlignmentID, _chromatogramSerializer).ConfigureAwait(false);
            await Task.WhenAll(new[]
            {
                _alignmentFile.SaveAlignmentResultAsync(_container),
                _alignmentFile.AppendMSDecResultAsync(mSDecResult),
                _alignmentFile.AppendEicInfoAsync(_chromatogramSerializer, spotInfo),
            }).ConfigureAwait(false);
        }

        private void Dispose(bool disposing) {
            if (!_disposedValue) {
                if (disposing) {
                    // TODO: dispose managed state (managed objects)
                    _spots?.Dispose();
                    _spots = null;
                }
                _disposedValue = true;
            }
        }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
