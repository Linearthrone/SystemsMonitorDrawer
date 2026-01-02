using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SystemMonitor
{
    public partial class GaugeControl : UserControl
    {
        public GaugeControl()
        {
            InitializeComponent();
        }

        // Helper to set color easily from MainWindow
        public void SetColor(string hexColor)
        {
            ActivePath.Stroke = (SolidColorBrush)new BrushConverter().ConvertFrom(hexColor);
        }

        public void SetLabel(string label)
        {
            LabelText.Text = label;
        }

        public void UpdateValue(float percentage)
        {
            // Clamp value between 0 and 100
            if (percentage < 0) percentage = 0;
            if (percentage > 100) percentage = 100;

            ValueText.Text = $"{Math.Round(percentage)}%";

            // Math: Map 0-100% to 0-180 degrees (PI radians)
            // Center of arc is roughly (70, 80) based on the PathFigure coordinates
            // Radius is 50

            double angle = (percentage / 100.0) * 180.0;
            double radians = (angle + 180) * (Math.PI / 180); // +180 to start from left side

            // Center X=70, Center Y=80 (Calculated from StartPoint 20,80 + Radius 50)
            double cx = 70;
            double cy = 80;
            double r = 50;

            double x = cx + r * Math.Cos(radians);
            double y = cy + r * Math.Sin(radians);

            ActiveSegment.Point = new Point(x, y);
        }
    }
}
