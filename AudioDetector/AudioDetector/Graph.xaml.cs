using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace AudioDetector {
    /// <summary>
    /// Interaction logic for Graph.xaml
    /// </summary>
    public partial class Graph : UserControl {

        //private LinkedList<Point> data = new LinkedList<Point>();
        private Dictionary<string, GraphSeries> series = new Dictionary<string, GraphSeries>();
        private const int MaxDatapoints = 3000;

        //private int nextX = 0;
        private int canvasHeight;

        public Graph() {
            InitializeComponent();
        }

        public void AddSeries(string name, Brush color) {
            Polyline polyline = new Polyline();
            polyline.StrokeThickness = 1;
            polyline.Stroke = color;
            polyline.Points = new PointCollection();

            series.Add(name, new GraphSeries {
                Polyline = polyline,
            });

            MainWindow.UiAction(() => {
                uiGraph.Children.Add(polyline);
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="point">0 to 1</param>
        public void AddData(string seriesName, double point) {
            if (canvasHeight == 0) {
                // Not yet initialized
                return;
            }

            //point = new Random().NextDouble();
            //point = 0.1;

            bool removeFirst = false;
            Point newPoint;

            if (!series.ContainsKey(seriesName)) {
                throw new Exception("No series added named " + seriesName);
            }

            GraphSeries thisSeries = series[seriesName];

            lock (series) {
                // TODO: On resize, need to recalculate these point Ys
                newPoint = new Point(thisSeries.NextX++, (1-point) * canvasHeight);
                thisSeries.Data.AddFirst(newPoint);

                if (thisSeries.Data.Count > MaxDatapoints) {
                    removeFirst = true;
                    thisSeries.Data.RemoveLast();
                }
            }

            MainWindow.UiAction(() => {
                if (removeFirst) {
                    thisSeries.Polyline.Points.RemoveAt(0);
                }

                thisSeries.Polyline.Points.Add(newPoint);

                // Scroll graph when full
                int widthInt = (int)ActualWidth;
                int maxNextX = series.Values.Max(s => s.NextX);
                if (maxNextX > widthInt) {
                    Canvas.SetLeft(thisSeries.Polyline, widthInt - maxNextX);
                }
            });
        }

        private void Graph_OnLoaded(object sender, RoutedEventArgs e) {
            /*const double margin = 10;
            xmin = margin;
            xmax = uiGraph.Width - margin;
            ymin = margin;
            ymax = uiGraph.Height - margin;

            // Make the X axis.
            GeometryGroup xaxis_geom = new GeometryGroup();
            xaxis_geom.Children.Add(new LineGeometry(
                new Point(0, ymax), new Point(uiGraph.Width, ymax)));
            for (double x = xmin + step;
                x <= uiGraph.Width - step; x += step) {
                xaxis_geom.Children.Add(new LineGeometry(
                    new Point(x, ymax - margin / 2),
                    new Point(x, ymax + margin / 2)));
            }

            Path xaxis_path = new Path();
            xaxis_path.StrokeThickness = 1;
            xaxis_path.Stroke = Brushes.Black;
            xaxis_path.Data = xaxis_geom;

            uiGraph.Children.Add(xaxis_path);

            // Make the Y ayis.
            GeometryGroup yaxis_geom = new GeometryGroup();
            yaxis_geom.Children.Add(new LineGeometry(
                new Point(xmin, 0), new Point(xmin, uiGraph.Height)));
            for (double y = step; y <= uiGraph.Height - step; y += step) {
                yaxis_geom.Children.Add(new LineGeometry(
                    new Point(xmin - margin / 2, y),
                    new Point(xmin + margin / 2, y)));
            }

            Path yaxis_path = new Path();
            yaxis_path.StrokeThickness = 1;
            yaxis_path.Stroke = Brushes.Black;
            yaxis_path.Data = yaxis_geom;

            uiGraph.Children.Add(yaxis_path);
            */

            canvasHeight = (int)uiGraph.ActualHeight;
            /*PointCollection points = new PointCollection();

            polyline = new Polyline();
            polyline.StrokeThickness = 1;
            polyline.Stroke = Brushes.Green;
            polyline.Points = points;*/

            // This doesn't work to enable the Y axis to be between 0 and 1
            //polyline.LayoutTransform = new ScaleTransform { ScaleX = 1, ScaleY = -1 };
            //polyline.LayoutTransform = new ScaleTransform(1, 10);
            uiGraph.ClipToBounds = true;

            AddGraphData();

            //uiGraph.Children.Add(polyline);

            //Canvas.SetBottom(polyline, 0);
            //Canvas.SetTop(polyline, 1);

        }

        /// <summary>
        /// https://stackoverflow.com/a/13093518
        /// </summary>
        /// <param name="canvas"></param>
        /// <param name="xMin"></param>
        /// <param name="xMax"></param>
        /// <param name="yMin"></param>
        /// <param name="yMax"></param>
        public static void SetCoordinateSystem(Canvas canvas, double xMin, double xMax, double yMin, double yMax) {
            var width = xMax - xMin;
            var height = yMax - yMin;

            var translateX = -xMin;
            var translateY = height + yMin;

            var group = new TransformGroup();

            group.Children.Add(new TranslateTransform(0, -translateY));
            group.Children.Add(new ScaleTransform(1, canvas.ActualHeight / -height));

            canvas.RenderTransform = group;


        }

        private void AddGraphData() {
            /*lock (data) {
                
                // TODO: Reuse points
                PointCollection points = new PointCollection();
                double x = xmin;
                foreach (double point in data) {
                    if (x > xmax) {
                        break;
                    }

                    double y = (ymax - ymin) * point + ymin;
                    if (y < ymin) y = (int)ymin;
                    if (y > ymax) y = (int)ymax;
                    //points.Add(new Point(x, y));
                    points.Add(new Point(new Random().Next(1, (int) ActualWidth), 0));

                    x += step;
                }

                polyline.Points = points;
            }*/
        }

        private class GraphSeries {

            public Polyline Polyline;
            public LinkedList<Point> Data = new LinkedList<Point>();
            public int NextX = 0;
        }
    }
}
