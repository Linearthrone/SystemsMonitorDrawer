using LibreHardwareMonitor.Hardware;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                IsMemoryEnabled = true,
                IsMotherboardEnabled = true,
                IsControllerEnabled = true
            };
            _computer.Open();
        }

        public (float cpu, float gpu, float cpuTemp, float ramAvailable, float ramTotal, float cpuFanRpm) GetUsage()
        {
            float cpu = 0;
            float gpu = 0;
            float cpuTemp = 0;
            float ramAvailable = 0;
            float ramTotal = 0;
            float cpuFanRpm = 0;
            var fanCandidates = new List<ISensor>();

            foreach (var hw in _computer.Hardware)
            {
                hw.Update(); // Vital: refreshes the sensor data
                Debug.WriteLine($"=== Hardware: {hw.Name} ({hw.HardwareType}) ===");
                Debug.WriteLine($"  Total Sensors: {hw.Sensors.Count()}");

                // Log all sensor types
                var sensorGroups = hw.Sensors.GroupBy(s => s.SensorType);
                foreach (var group in sensorGroups)
                {
                    Debug.WriteLine($"    {group.Key}: {group.Count()} sensors");
                }

                fanCandidates.AddRange(GetFanSensors(hw));

                if (hw.HardwareType == HardwareType.Cpu)
                {
                    // "CPU Total" is usually the best metric
                    cpu = hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load && s.Name == "CPU Total")?.Value ?? 0;

                    // Get CPU temperature - get all temperature sensors
                    var allTempSensors = hw.Sensors.Where(s => s.SensorType == SensorType.Temperature).ToList();

                    System.Diagnostics.Debug.WriteLine($"=== CPU Temperature Sensors ({allTempSensors.Count}) ===");
                    foreach (var ts in allTempSensors)
                    {
                        System.Diagnostics.Debug.WriteLine($"  {ts.Name}: {ts.Value}°C");
                    }

                    // Filter for valid temperatures (between 0 and 150°C)
                    var tempSensors = allTempSensors.Where(s => s.Value.HasValue && s.Value.Value > 0 && s.Value.Value < 150).ToList();

                    if (tempSensors.Any())
                    {
                        // Get the maximum temperature (usually the hottest core/package)
                        cpuTemp = tempSensors.Max(s => s.Value.Value);
                        System.Diagnostics.Debug.WriteLine($"Selected CPU Temp: {cpuTemp}°C");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("No valid CPU temperature sensors found!");
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

                    // Debug: Log all memory sensors to console
                    var memorySensors = hw.Sensors.Where(s => s.SensorType == SensorType.Data || s.SensorType == SensorType.Load).ToList();
                    System.Diagnostics.Debug.WriteLine("=== Memory Sensors ===");
                    foreach (var sensor in memorySensors)
                    {
                        System.Diagnostics.Debug.WriteLine($"Sensor: {sensor.Name}, Type: {sensor.SensorType}, Value: {sensor.Value}");
                    }

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

                    // Get available memory (values are already in GB)
                    if (availableSensor != null && availableSensor.Value.HasValue && availableSensor.Value.Value > 0)
                    {
                        ramAvailable = availableSensor.Value.Value;
                        System.Diagnostics.Debug.WriteLine($"Available RAM from sensor: {ramAvailable:F2} GB");
                    }

                    // Get total memory - calculate from used + available or from load percentage
                    var usedSensor = hw.Sensors.FirstOrDefault(s =>
                        s.SensorType == SensorType.Data &&
                        s.Name.Contains("Used", StringComparison.OrdinalIgnoreCase) &&
                        s.Name.Contains("Memory", StringComparison.OrdinalIgnoreCase) &&
                        !s.Name.Contains("Virtual", StringComparison.OrdinalIgnoreCase));

                    if (usedSensor != null && usedSensor.Value.HasValue && usedSensor.Value.Value > 0)
                    {
                        float usedGB = usedSensor.Value.Value;
                        ramTotal = usedGB + ramAvailable;
                        System.Diagnostics.Debug.WriteLine($"Total RAM calculated from Used ({usedGB:F2} GB) + Available ({ramAvailable:F2} GB) = {ramTotal:F2} GB");
                    }

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

            var fanSensors = fanCandidates
                .Where(s => s.SensorType == SensorType.Fan && s.Value.HasValue && s.Value.Value > 0)
                .OrderByDescending(s => s.Value)
                .ToList();

            Debug.WriteLine($"=== Fan Sensors ({fanSensors.Count}) ===");
            foreach (var sensor in fanCandidates)
            {
                Debug.WriteLine($"  {sensor.Name} [{sensor.SensorType}] => {sensor.Value?.ToString() ?? "null"}");
            }

            if (fanSensors.Any())
            {
                var preferred = fanSensors
                    .FirstOrDefault(s => s.Name.IndexOf("CPU", StringComparison.OrdinalIgnoreCase) >= 0)
                    ?? fanSensors.First();
                cpuFanRpm = preferred.Value ?? 0;
                System.Diagnostics.Debug.WriteLine($"CPU Fan Sensor: {preferred.Name} => {cpuFanRpm} RPM");
            }
            else
            {
                Debug.WriteLine("No fan sensors reported RPM > 0");
            }

            return (cpu, gpu, cpuTemp, ramAvailable, ramTotal, cpuFanRpm);
        }

        private IEnumerable<ISensor> GetFanSensors(IHardware hardware)
        {
            var list = new List<ISensor>();
            var fanSensorsInHW = hardware.Sensors.Where(s => s.SensorType == SensorType.Fan).ToList();
            if (fanSensorsInHW.Any())
            {
                Debug.WriteLine($"  Found {fanSensorsInHW.Count} fan sensors in {hardware.Name}");
                foreach (var fan in fanSensorsInHW)
                {
                    Debug.WriteLine($"    - {fan.Name}: {fan.Value}");
                }
            }
            list.AddRange(fanSensorsInHW);

            foreach (var sub in hardware.SubHardware)
            {
                sub.Update();
                Debug.WriteLine($"  SubHardware: {sub.Name} ({sub.HardwareType})");
                list.AddRange(GetFanSensors(sub));
            }

            return list;
        }

        public void Close()
        {
            _computer.Close();
        }
        public int GetCpuFanRpm()
        {
            int rpm = 0;

            foreach (var hw in _computer.Hardware)
            {
                // ASUS fans usually live in SuperIO or Motherboard types
                if (hw.HardwareType == HardwareType.SuperIO || hw.HardwareType == HardwareType.Motherboard)
                {
                    hw.Update();

                    // Look for specific CPU fan
                    var cpuFan = hw.Sensors.FirstOrDefault(s =>
                        s.SensorType == SensorType.Fan &&
                        (s.Name.Contains("CPU") || s.Name.Contains("#1")) // Common ASUS names
                    );

                    if (cpuFan != null && cpuFan.Value.HasValue)
                    {
                        rpm = (int)cpuFan.Value.Value;
                        break; // Found it, get out
                    }
                }
            }
            return rpm;
        }

    }
}
