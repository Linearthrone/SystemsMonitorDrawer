using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SystemMonitor
{
    public partial class RamBarControl : UserControl
    {
        private float _lastAvailableGB = 0;
        private float _lastTotalGB = 0;
        private bool _isLoaded = false;

        public RamBarControl()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;
            UpdateValue(_lastAvailableGB, _lastTotalGB);
        }

        public void UpdateValue(float availableGB, float totalGB)
        {
            _lastAvailableGB = availableGB;
            _lastTotalGB = totalGB;

            if (!_isLoaded)
                return;

            if (totalGB <= 0)
            {
                totalGB = 1; // Avoid division by zero
            }

            // Calculate percentage used for bar width
            float usedGB = totalGB - availableGB;
            float percentageUsed = (usedGB / totalGB) * 100;

            // Clamp between 0 and 100
            if (percentageUsed < 0) percentageUsed = 0;
            if (percentageUsed > 100) percentageUsed = 100;

            // Update bar width based on the actual background width
            double barWidth = (percentageUsed / 100.0) * BarBackground.ActualWidth;
            BarFill.Width = barWidth;

            // Update gradient based on percentage used
            UpdateGradient(percentageUsed);

            // Update text to show available/total
            ValueText.Text = $"{availableGB:F1} GB / {totalGB:F1} GB";
        }

        private void UpdateGradient(float percentageUsed)
        {
            Color startColor;
            Color endColor;

            if (percentageUsed < 30)
            {
                // Green (0-30%)
                startColor = Color.FromRgb(0, 255, 136);
                endColor = Color.FromRgb(0, 255, 136);
            }
            else if (percentageUsed < 50)
            {
                // Green to Yellow (30-50%)
                float ratio = (percentageUsed - 30) / 20.0f;
                startColor = Color.FromRgb(0, 255, 136);
                endColor = Color.FromRgb(255, 255, 0);
            }
            else if (percentageUsed < 70)
            {
                // Yellow to Orange (50-70%)
                float ratio = (percentageUsed - 50) / 20.0f;
                startColor = Color.FromRgb(255, 255, 0);
                endColor = Color.FromRgb(255, 165, 0);
            }
            else if (percentageUsed < 85)
            {
                // Orange to Red (70-85%)
                float ratio = (percentageUsed - 70) / 15.0f;
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
