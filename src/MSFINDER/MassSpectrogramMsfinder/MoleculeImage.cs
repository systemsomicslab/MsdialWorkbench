using NCDK;
using NCDK.Depict;
using NCDK.Graphs;
using NCDK.Smiles;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// Draws a structure image from a SMILES string with NCDK.
    /// </summary>
    public static class MoleculeImage
    {
        private static readonly SmilesParser parser = new SmilesParser(CDK.Builder);

        /// <summary>
        /// Returns null when the size is not positive, the SMILES cannot be parsed, or the molecule is not connected.
        /// </summary>
        public static BitmapImage SmilesToMediaImageSource(string smiles, int width, int height) {
            if (width <= 0 || height <= 0) return null;

            IAtomContainer container;
            try {
                container = parser.ParseSmiles(smiles);
            }
            catch (InvalidSmilesException) {
                return null;
            }
            if (!ConnectivityChecker.IsConnected(container)) return null;

            var depiction = new DepictionGenerator().Depict(container);
            return ToBitmapImage(depiction.ToBitmap(width, height, PixelFormats.Pbgra32));
        }

        private static BitmapImage ToBitmapImage(RenderTargetBitmap renderTargetBitmap) {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));

            var bitmapImage = new BitmapImage();
            using (var stream = new MemoryStream()) {
                encoder.Save(stream);
                stream.Seek(0, SeekOrigin.Begin);

                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();
            }
            return bitmapImage;
        }
    }
}
