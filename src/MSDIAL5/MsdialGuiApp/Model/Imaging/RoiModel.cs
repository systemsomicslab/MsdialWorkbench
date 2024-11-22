using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.DataObj;
using CompMs.CommonMVVM;
using CompMs.RawDataHandler.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CompMs.App.Msdial.Model.Imaging
{
    internal sealed class RoiModel : BindableBase {
        public RoiModel(AnalysisFileBeanModel file, int id, MaldiFrames frames, Color color) {
            File = file ?? throw new ArgumentNullException(nameof(file));
            Id = id;
            Frames = frames ?? throw new ArgumentNullException(nameof(frames));
            Color = color;
            int width = BitmapImageModel.WithMarginToLength(frames.XIndexWidth);
            int height = BitmapImageModel.WithMarginToLength(frames.YIndexHeight);
            int xmin = BitmapImageModel.WithMarginToPoint(frames.XIndexPosMin);
            int ymin = BitmapImageModel.WithMarginToPoint(frames.YIndexPosMin);
            var image = new ulong[width, (height + 63) / 64];
            foreach (var frame in frames.Infos) {
                image[frame.XIndexPos - xmin, (frame.YIndexPos - ymin) / 64] |= 1ul << ((frame.YIndexPos - ymin) % 64);
            }
            var mask = new ulong[width, (height + 63) / 64];
            var m = image.GetLength(0);
            var n = image.GetLength(1);
            for (int i = 0; i < m; i++) {
                for (int j = 0; j < n; j++) {
                    mask[i, j] |= image[i, j] >> 1;
                    mask[i, j] |= image[i, j] << 1;
                    if (i >= 1) {
                        mask[i, j] |= image[i - 1, j];
                    }
                    if (i + 1 < m) {
                        mask[i, j] |= image[i + 1, j];
                    }
                    if (j >= 1) {
                        mask[i, j] |= image[i, j - 1] >> 63;
                    }
                    if (j + 1 < n) {
                        mask[i, j] |= image[i, j + 1] << 63;
                    }
                    mask[i, j] &= ~image[i, j];
                }
            }
            var imageBytes = new byte[width * height];
            var k = width;
            var l = height;
            for (int i = 0; i < k; i++) {
                for (int j = 0; j < l; j++) {
                    imageBytes[j * k + i] = (byte)((mask[i, j / 64] >> (j % 64)) & 1); 
                }
            }
            var bp = new BitmapPalette(Enumerable.Repeat(color, 1 << 8 - 1).Prepend(Color.FromArgb(0, 0, 0, 0)).ToArray());
            // var bp = new BitmapPalette(new[] { Color.FromArgb(0, 0, 0, 0), color});
            RoiImage = BitmapImageModel.Create(imageBytes, width, height, PixelFormats.Indexed8, bp, "ROI");
        }

        public AnalysisFileBeanModel File { get; }
        public int Id { get; }
        public MaldiFrames Frames { get; }
        public Color Color { get; }
        public BitmapImageModel RoiImage { get; }

        public RawSpectraOnPixels RetrieveRawSpectraOnPixels(List<Raw2DElement> targetElements) {
            using RawDataAccess rawDataAccess = new RawDataAccess(File.AnalysisFilePath, 0, true, true, true);
            return rawDataAccess.GetRawPixelFeatures(targetElements, [.. Frames.Infos], isNewProcess: false)
                ?? new RawSpectraOnPixels { PixelPeakFeaturesList = new List<RawPixelFeatures>(0), XYFrames = new List<MaldiFrameInfo>(0), };
        }

        public RawIntensityOnPixelsLoader GetIntensityOnPixelsLoader(List<Raw2DElement> targetElements) {
            return new RawIntensityOnPixelsLoader(targetElements, File, Frames);
        }
    }
}
