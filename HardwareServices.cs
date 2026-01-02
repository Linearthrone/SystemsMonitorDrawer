using LibreHardwareMonitor.Hardware;
using System.Linq;

namespace SystemMonitor
{
    public class HardwareService
    {
        private Computer _computer;

        public HardwareService()
        {
            _computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsMemoryEnabled = true
            };
            _computer.Open();
        }

        public (float cpu, float gpu, float cpuTemp, float ramAvailable, float ramTotal) GetUsage()
        {
            float cpu = 0;
            float gpu = 0;
            float cpuTemp = 0;
            float ramAvailable = 0;
            float ramTotal = 0;

            foreach (var hw in _computer.Hardware)
            {
                hw.Update(); // Vital: refreshes the sensor data

                if (hw.HardwareType == HardwareType.Cpu)
                {
                    // "CPU Total" is usually the best metric
                    cpu = hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load && s.Name == "CPU Total")?.Value ?? 0;

                    // Get CPU temperature - try multiple sensor names and get the maximum temperature
                    var tempSensors = hw.Sensors.Where(s => s.SensorType == SensorType.Temperature).ToList();
                    if (tempSensors.Any())
                    {
                        // Find sensors with common CPU temperature names
                        var cpuPackage = tempSensors.FirstOrDefault(s =>
                            s.Name.Contains("Package", StringComparison.OrdinalIgnoreCase) ||
                            s.Name.Contains("CPU", StringComparison.OrdinalIgnoreCase));

                        // Get all available temperature values
                        var availableTemps = tempSensors.Where(s => s.Value.HasValue && s.Value.Value > 0).Select(s => s.Value.Value).ToList();

                        if (availableTemps.Any())
                        {
                            // Get the maximum temperature (usually the hottest core/package)
                            cpuTemp = availableTemps.Max();
                        }
                    }
                }
                // Check for both Nvidia and AMD
                else if (hw.HardwareType == HardwareType.GpuNvidia || hw.HardwareType == HardwareType.GpuAmd)
                {
                    gpu = hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load && s.Name == "GPU Core")?.Value ?? 0;
                }
                else if (hw.HardwareType == HardwareType.Memory)
                {
                    // Try to get available and total memory in GB
                    // LibreHardwareMonitor may report memory differently
                    var availableSensor = hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Data && s.Name == "Memory Available");
                    var usedSensor = hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Data && s.Name == "Memory Used");
                    var totalSensor = hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Data && s.Name == "Memory");

                    if (availableSensor != null && availableSensor.Value.HasValue)
                        ramAvailable = (float)(availableSensor.Value.Value / (1024.0 * 1024.0 * 1024.0)); // Convert to GB

                    if (totalSensor != null && totalSensor.Value.HasValue)
                        ramTotal = (float)(totalSensor.Value.Value / (1024.0 * 1024.0 * 1024.0)); // Convert to GB

                    // If total is still 0, try using used + available
                    if (ramTotal <= 0 && usedSensor != null && usedSensor.Value.HasValue)
                    {
                        float usedGB = (float)(usedSensor.Value.Value / (1024.0 * 1024.0 * 1024.0));
                        ramTotal = usedGB + ramAvailable;
                    }
                }
            }

            return (cpu, gpu, cpuTemp, ramAvailable, ramTotal);
        }

        public void Close()
        {
            _computer.Close();
        }
    }
}
