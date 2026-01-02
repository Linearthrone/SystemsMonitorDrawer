using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SystemMonitor
{
    public partial class TemperatureBarControl : UserControl
    {
        private float _lastTemp = 0;
        private bool _isLoaded = false;

        public TemperatureBarControl()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;
            UpdateValue(_lastTemp);
        }

        public void UpdateValue(float temperatureCelsius)
        {
            _lastTemp = temperatureCelsius;

            if (!_isLoaded)
                return;

            // Store original temperature for display
            float originalTemp = temperatureCelsius;

            // Temperature range: 20°C to 100°C (normal to critical)
            float minTemp = 20.0f;
            float maxTemp = 100.0f;

            // Clamp temperature for bar height calculation
            if (temperatureCelsius < minTemp) temperatureCelsius = minTemp;
            if (temperatureCelsius > maxTemp) temperatureCelsius = maxTemp;

            // Calculate percentage for bar height
            float percentage = (temperatureCelsius - minTemp) / (maxTemp - minTemp) * 100.0f;

            // Update bar height based on the background height
            double barHeight = (percentage / 100.0) * ThermometerBackground.ActualHeight;
            TemperatureFill.Height = barHeight;

            // Update gradient based on temperature
            UpdateGradient(temperatureCelsius);

            // Update text with the original temperature value
            TemperatureText.Text = $"{Math.Round(originalTemp)}°C";
        }

        private void UpdateGradient(float temperatureCelsius)
        {
            Color startColor;
            Color endColor;

            // Temperature-based gradient
            // Normal: 20-60°C (Green)
            // Moderate: 60-75°C (Green to Yellow)
            // High: 75-85°C (Yellow to Orange)
            // Critical: 85-100°C (Orange to Red)

            if (temperatureCelsius < 60)
            {
                // Normal - Green
                startColor = Color.FromRgb(0, 255, 136);
                endColor = Color.FromRgb(0, 255, 136);
            }
            else if (temperatureCelsius < 75)
            {
                // Moderate - Green to Yellow
                float ratio = (temperatureCelsius - 60) / 15.0f;
                startColor = Color.FromRgb(0, 255, 136);
                endColor = Color.FromRgb(255, 255, 0);
            }
            else if (temperatureCelsius < 85)
            {
                // High - Yellow to Orange
                float ratio = (temperatureCelsius - 75) / 10.0f;
                startColor = Color.FromRgb(255, 255, 0);
                endColor = Color.FromRgb(255, 165, 0);
            }
            else
            {
                // Critical - Orange to Red
                startColor = Color.FromRgb(255, 165, 0);
                endColor = Color.FromRgb(255, 51, 51);
            }

            GradientStart.Color = startColor;
            GradientEnd.Color = endColor;
        }
    }
}
