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

                    // Get CPU temperature - get all temperature sensors and find the highest valid value
                    var tempSensors = hw.Sensors.Where(s => s.SensorType == SensorType.Temperature && s.Value.HasValue && s.Value.Value > 20).ToList();
                    if (tempSensors.Any())
                    {
                        // Get the maximum temperature (usually the hottest core/package)
                        cpuTemp = tempSensors.Max(s => s.Value.Value);
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
                    // LibreHardwareMonitor reports memory differently per system
                    
                    // Try multiple sensor name patterns
                    var availableSensor = hw.Sensors.FirstOrDefault(s => 
                        s.SensorType == SensorType.Data && 
                        (s.Name.Contains("Available", StringComparison.OrdinalIgnoreCase) ||
                         s.Name.Contains("Free", StringComparison.OrdinalIgnoreCase)));
                    
                    var usedSensor = hw.Sensors.FirstOrDefault(s => 
                        s.SensorType == SensorType.Data && 
                        s.Name.Contains("Used", StringComparison.OrdinalIgnoreCase));
                    
                    var totalSensor = hw.Sensors.FirstOrDefault(s => 
                        s.SensorType == SensorType.Data && 
                        s.Name.Contains("Memory", StringComparison.OrdinalIgnoreCase) &&
                        !s.Name.Contains("Available", StringComparison.OrdinalIgnoreCase) &&
                        !s.Name.Contains("Used", StringComparison.OrdinalIgnoreCase));

                    // Get available memory
                    if (availableSensor != null && availableSensor.Value.HasValue && availableSensor.Value.Value > 0)
                        ramAvailable = (float)(availableSensor.Value.Value / (1024.0 * 1024.0 * 1024.0)); // Convert to GB

                    // Get total memory
                    if (totalSensor != null && totalSensor.Value.HasValue && totalSensor.Value.Value > 0)
                        ramTotal = (float)(totalSensor.Value.Value / (1024.0 * 1024.0 * 1024.0)); // Convert to GB

                    // If still no total, calculate from used + available
                    if (ramTotal <= 0 && usedSensor != null && usedSensor.Value.HasValue && usedSensor.Value.Value > 0)
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
