using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace SystemMonitor
{
    public partial class MainWindow : Window
    {
        private HardwareService _monitor;
        private DispatcherTimer _timer;

        public MainWindow()
        {
            InitializeComponent();

            // Initialize Hardware Monitor
            _monitor = new HardwareService();

            // Setup Gauges
            CpuGauge.SetLabel("CPU");
            CpuGauge.SetColor("#FF3333"); // Red

            GpuGauge.SetLabel("GPU");
            GpuGauge.SetColor("#00FF88"); // Green/Cyan

            RamGauge.SetLabel("RAM");
            RamGauge.SetColor("#3388FF"); // Blue

            // Setup Timer (1 second update)
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            var stats = _monitor.GetUsage();

            CpuGauge.UpdateValue(stats.cpu);
            GpuGauge.UpdateValue(stats.gpu);
            RamGauge.UpdateValue(stats.ram);
        }

        // Allow dragging the window
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        // Cleanup on close
        protected override void OnClosed(EventArgs e)
        {
            _monitor.Close();
            base.OnClosed(e);
        }
    }
}
