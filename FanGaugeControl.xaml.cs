using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SystemMonitor
{
    public partial class FanGaugeControl : UserControl
    {
        private const double StartAngle = -135.0; // Degrees
        private const double SweepAngle = 90.0;   // Degrees
        private const double Radius = 55.0;
        private const double MaxRpm = 5000.0;
        private const double TickStep = 250.0;
        private readonly Point _center = new Point(70.0, 80.0);

        private Line _needle;
        private EllipseGeometry _hubGeometry;

        public FanGaugeControl()
        {
            InitializeComponent();
            BuildGauge();
            UpdateRpm(0);
        }

        public void UpdateRpm(float rpm)
        {
            if (rpm < 0) rpm = 0;
            if (rpm > MaxRpm) rpm = (float)MaxRpm;

            RpmText.Text = $"{Math.Round(rpm)} RPM";

            var angle = StartAngle + (rpm / MaxRpm) * SweepAngle;
            var endPoint = PointFromAngle(angle, Radius - 8);
            _needle.X2 = endPoint.X;
            _needle.Y2 = endPoint.Y;
        }

        private void BuildGauge()
        {
            GaugeCanvas.Children.Clear();
            GaugeCanvas.RenderTransform = new RotateTransform(-45, _center.X, _center.Y);

            // Background arc
            var arcPath = new Path
            {
                Stroke = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                StrokeThickness = 10,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Data = CreateArcGeometry(StartAngle, SweepAngle, Radius)
            };
            GaugeCanvas.Children.Add(arcPath);

            // Tick marks (minor every 250 RPM, major every 1500 RPM)
            for (double value = 0; value <= MaxRpm + 0.1; value += TickStep)
            {
                var isMajor = Math.Abs(value % 1500.0) < 0.1;
                var angle = StartAngle + (value / MaxRpm) * SweepAngle;
                var outer = PointFromAngle(angle, Radius + 2);
                var inner = PointFromAngle(angle, isMajor ? Radius - 18 : Radius - 10);

                var tick = new Line
                {
                    X1 = inner.X,
                    Y1 = inner.Y,
                    X2 = outer.X,
                    Y2 = outer.Y,
                    Stroke = new SolidColorBrush(Color.FromRgb(102, 102, 102)),
                    StrokeThickness = isMajor ? 2.2 : 1,
                    SnapsToDevicePixels = true
                };
                GaugeCanvas.Children.Add(tick);
            }

            // Needle
            _needle = new Line
            {
                X1 = _center.X,
                Y1 = _center.Y,
                X2 = _center.X,
                Y2 = _center.Y,
                Stroke = new SolidColorBrush(Color.FromRgb(255, 204, 51)),
                StrokeThickness = 3,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };
            GaugeCanvas.Children.Add(_needle);

            // Hub
            _hubGeometry = new EllipseGeometry(_center, 4, 4);
            var hubPath = new Path
            {
                Fill = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                Stroke = new SolidColorBrush(Color.FromRgb(255, 204, 51)),
                StrokeThickness = 1.5,
                Data = _hubGeometry
            };
            GaugeCanvas.Children.Add(hubPath);
        }

        private Geometry CreateArcGeometry(double startAngle, double sweepAngle, double radius)
        {
            var start = PointFromAngle(startAngle, radius);
            var end = PointFromAngle(startAngle + sweepAngle, radius);

            var figure = new PathFigure
            {
                StartPoint = start,
                IsClosed = false
            };

            figure.Segments.Add(new ArcSegment
            {
                Point = end,
                Size = new System.Windows.Size(radius, radius),
                SweepDirection = SweepDirection.Clockwise,
                IsLargeArc = Math.Abs(sweepAngle) > 180
            });

            var geometry = new PathGeometry();
            geometry.Figures.Add(figure);
            return geometry;
        }

        private Point PointFromAngle(double angleDegrees, double radius)
        {
            var radians = angleDegrees * (Math.PI / 180.0);
            var x = _center.X + radius * Math.Cos(radians);
            var y = _center.Y + radius * Math.Sin(radians);
            return new Point(x, y);
        }
    }
}
