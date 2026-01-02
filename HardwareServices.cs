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

        public (float cpu, float gpu, float ram) GetUsage()
        {
            float cpu = 0;
            float gpu = 0;
            float ram = 0;

            foreach (var hw in _computer.Hardware)
            {
                hw.Update(); // Vital: refreshes the sensor data

                if (hw.HardwareType == HardwareType.Cpu)
                {
                    // "CPU Total" is usually the best metric
                    cpu = hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load && s.Name == "CPU Total")?.Value ?? 0;
                }
                // Check for both Nvidia and AMD
                else if (hw.HardwareType == HardwareType.GpuNvidia || hw.HardwareType == HardwareType.GpuAmd)
                {
                    gpu = hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load && s.Name == "GPU Core")?.Value ?? 0;
                }
                else if (hw.HardwareType == HardwareType.Memory)
                {
                    ram = hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load && s.Name == "Memory")?.Value ?? 0;
                }
            }

            return (cpu, gpu, ram);
        }

        public void Close()
        {
            _computer.Close();
        }
    }
}
