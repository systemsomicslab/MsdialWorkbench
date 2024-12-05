using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Imaging
{
    internal sealed class RoiEditModel : BindableBase
    {
        private readonly AnalysisFileBeanModel _file;
        private readonly MaldiFrames _frames;

        public RoiEditModel(AnalysisFileBeanModel file, MaldiFrames frames) {
            _file = file ?? throw new ArgumentNullException(nameof(file));
            _frames = frames ?? throw new ArgumentNullException(nameof(frames));
            CurrentArea = new RoiArea(frames);
        }

        public RoiArea CurrentArea { get; }

        public RoiModel? CreateRoi(int id, Color color) {
            if (CurrentArea is null) {
                return null;
            }
            return new RoiModel(_file, id, CurrentArea.ContainFrames(), color);
        }
    }

    internal sealed class RoiArea : BindableBase {
        private readonly MaldiFrames _frames;

        public RoiArea(MaldiFrames frames) {
            _frames = frames ?? throw new ArgumentNullException(nameof(frames));
            RelativePoints = new List<(double, double)>();
            DrawWidth = BitmapImageModel.WithMarginToLength(frames.XIndexWidth);
            DrawHeight = BitmapImageModel.WithMarginToLength(frames.YIndexHeight);
        }

        public int DrawWidth { get; }
        public int DrawHeight { get; }

        public List<(double x, double y)>? RelativePoints {
            get => _relativePoints;
            set => SetProperty(ref _relativePoints, value);
        }
        private List<(double, double)>? _relativePoints;

        public MaldiFrames ContainFrames() {
            var board = GetInnerPoints();
            var xMin = BitmapImageModel.WithMarginToPoint(_frames.XIndexPosMin);
            var yMin = BitmapImageModel.WithMarginToPoint(_frames.YIndexPosMin);
            var infos = _frames.Infos.Where(info => board[info.XIndexPos - xMin, info.YIndexPos - yMin]);
            return new MaldiFrames(infos, _frames);
        }

        private bool[,] GetInnerPoints() {
            var width = DrawWidth;
            var height = DrawHeight;
            var board = new bool[width, height];
            var points = RelativePoints;
            if (points is null || points.Count < 2) {
                return board;
            }
            var edges = new Edge[points.Count];
            for (int i = 1; i < points.Count; i++) {
                edges[i] = new Edge(points[i - 1].x * width, points[i - 1].y * height, points[i].x * width, points[i].y * height);
            }
            edges[0] = new Edge(points[points.Count - 1].x * width, points[points.Count - 1].y * height, points[0].x * width, points[0].y * height);

            for (int i = 0; i < width; i++) {
                var x = i + 1 / 2d;
                var ys = edges.Where(edge => edge.Intersect(x)).Select(edge => edge.GetY(x)).OrderBy(y => y).ToArray();
                for (int j = 0; j < ys.Length / 2; j++) {
                    for (int k = Math.Max(0, ys[j * 2]); k <= Math.Min(ys[j * 2 + 1], height - 1); k++) {
                        board[i, k] = true;
                    }
                }
            }
            return board;
        }
    }

    internal sealed class Edge {
        public Edge(double x1, double y1, double x2, double y2) {
            if (x1 <= x2) {
                X1 = x1;
                Y1 = y1;
                X2 = x2;
                Y2 = y2;
            }
            else {
                X1 = x2;
                Y1 = y2;
                X2 = x1;
                Y2 = y1;
            }
        }

        public double X1 { get; }
        public double Y1 { get; }
        public double X2 { get; }
        public double Y2 { get; }

        public bool Intersect(double x) {
            return X1 <= x && x < X2 || X1 < x && x <= X2;
        }

        public int GetY(double x) {
            return Convert.ToInt32((x - X1) / (X2 - X1) * (Y2 - Y1) + Y1);
        }
    }
}
