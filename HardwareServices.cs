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
                    
                    // Debug: Log all memory sensors
                    var memorySensors = hw.Sensors.Where(s => s.SensorType == SensorType.Data || s.SensorType == SensorType.Load).ToList();
                    
                    // Try multiple strategies to find memory values
                    
                    // Strategy 1: Look for exact matches first
                    var availableSensor = hw.Sensors.FirstOrDefault(s => 
                        s.SensorType == SensorType.Data && 
                        s.Name.Equals("Memory Available", StringComparison.OrdinalIgnoreCase));
                    
                    var totalSensor = hw.Sensors.FirstOrDefault(s => 
                        s.SensorType == SensorType.Data && 
                        s.Name.Equals("Memory", StringComparison.OrdinalIgnoreCase));
                    
                    // Strategy 2: Pattern matching if exact matches fail
                    if (availableSensor == null)
                    {
                        availableSensor = hw.Sensors.FirstOrDefault(s => 
                            s.SensorType == SensorType.Data && 
                            (s.Name.Contains("Available", StringComparison.OrdinalIgnoreCase) ||
                             s.Name.Contains("Free", StringComparison.OrdinalIgnoreCase)));
                    }
                    
                    if (totalSensor == null)
                    {
                        totalSensor = hw.Sensors.FirstOrDefault(s => 
                            s.SensorType == SensorType.Data && 
                            s.Name.Contains("Memory", StringComparison.OrdinalIgnoreCase) &&
                            !s.Name.Contains("Available", StringComparison.OrdinalIgnoreCase) &&
                            !s.Name.Contains("Used", StringComparison.OrdinalIgnoreCase));
                    }
                    
                    // Strategy 3: Try using any data sensor with "Memory" in the name
                    if (totalSensor == null)
                    {
                        totalSensor = hw.Sensors.FirstOrDefault(s => 
                            s.SensorType == SensorType.Data && 
                            s.Name.Contains("Memory", StringComparison.OrdinalIgnoreCase));
                    }

                    // Get available memory
                    if (availableSensor != null && availableSensor.Value.HasValue && availableSensor.Value.Value > 0)
                        ramAvailable = (float)(availableSensor.Value.Value / (1024.0 * 1024.0 * 1024.0)); // Convert to GB

                    // Get total memory
                    if (totalSensor != null && totalSensor.Value.HasValue && totalSensor.Value.Value > 0)
                        ramTotal = (float)(totalSensor.Value.Value / (1024.0 * 1024.0 * 1024.0)); // Convert to GB

                    // Strategy 4: Calculate from memory load percentage if still no values
                    if (ramTotal <= 0)
                    {
                        var memoryLoadSensor = hw.Sensors.FirstOrDefault(s => 
                            s.SensorType == SensorType.Load && 
                            s.Name.Contains("Memory", StringComparison.OrdinalIgnoreCase));
                        
                        if (memoryLoadSensor != null && memoryLoadSensor.Value.HasValue)
                        {
                            // This gives us percentage, but we need actual GB values
                            // If we have a value that looks like GB already, use it
                            var anyMemorySensor = hw.Sensors.FirstOrDefault(s => 
                                s.SensorType == SensorType.Data && 
                                s.Value.HasValue && 
                                s.Value.Value > 1000000); // At least 1MB
                            
                            if (anyMemorySensor != null)
                            {
                                ramTotal = (float)(anyMemorySensor.Value.Value / (1024.0 * 1024.0 * 1024.0));
                                var loadPercent = memoryLoadSensor.Value.Value;
                                ramAvailable = ramTotal * (1.0f - (loadPercent / 100.0f));
                            }
                        }
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
