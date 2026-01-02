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

            UpdateGradient(percentage);

            // Math: Map 0-100% to arc from bottom (90 degrees) to top (-90 degrees)
            // Center of arc is (70, 80)
            // Radius is 50
            // Arc goes from bottom to top counter-clockwise

            double angle = 90.0 - (percentage / 100.0) * 180.0; // Start from bottom (90 degrees) and go counter-clockwise
            double radians = angle * (Math.PI / 180.0);

            // Center X=70, Center Y=80
            double cx = 70;
            double cy = 80;
            double r = 50;

            double x = cx + r * Math.Cos(radians);
            double y = cy + r * Math.Sin(radians);

            ActiveSegment.Point = new Point(x, y);
        }

        private void UpdateGradient(float percentage)
        {
            Color startColor;
            Color endColor;

            if (percentage < 30)
            {
                // Green (0-30%)
                startColor = Color.FromRgb(0, 255, 136);
                endColor = Color.FromRgb(0, 255, 136);
            }
            else if (percentage < 50)
            {
                // Green to Yellow (30-50%)
                float ratio = (percentage - 30) / 20.0f;
                startColor = Color.FromRgb(0, 255, 136);
                endColor = Color.FromRgb(255, 255, 0);
            }
            else if (percentage < 70)
            {
                // Yellow to Orange (50-70%)
                float ratio = (percentage - 50) / 20.0f;
                startColor = Color.FromRgb(255, 255, 0);
                endColor = Color.FromRgb(255, 165, 0);
            }
            else if (percentage < 85)
            {
                // Orange to Red (70-85%)
                float ratio = (percentage - 70) / 15.0f;
                startColor = Color.FromRgb(255, 165, 0);
                endColor = Color.FromRgb(255, 51, 51);
            }
            else
            {
                // Red (85-100%)
                startColor = Color.FromRgb(255, 51, 51);
                endColor = Color.FromRgb(255, 51, 51);
            }

            GradientStart.Color = startColor;
            GradientEnd.Color = endColor;
        }
    }
}
