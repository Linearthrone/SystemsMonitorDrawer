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
        private bool _fanGaugeVisible = true;
        private bool _firstTick = true;

        public MainWindow()
        {
            InitializeComponent();

            // Initialize Hardware Monitor
            _monitor = new HardwareService();

            // Setup Gauges
            CpuGauge.SetLabel("CPU");
            GpuGauge.SetLabel("GPU");
            // Colors are dynamically set based on usage

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
            TempBar.UpdateValue(stats.cpuTemp);
            RamBar.UpdateValue(stats.ramAvailable, stats.ramTotal);
            
            // Hide fan gauge if no RPM data available
            if (_firstTick && stats.cpuFanRpm == 0)
            {
                CpuFanGauge.Visibility = Visibility.Collapsed;
                _fanGaugeVisible = false;
            }
            
            if (_fanGaugeVisible)
            {
                CpuFanGauge.UpdateRpm(stats.cpuFanRpm);
            }
            
            _firstTick = false;
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
