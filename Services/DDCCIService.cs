using System.Runtime.InteropServices;
using InputMux.Models;

namespace InputMux.Services
{
    public class DDCCIService
    {
        // DDC/CI 相关常量
        private const int MONITOR_DEFAULTTONULL = 0;
        private const int MONITOR_DEFAULTTOPRIMARY = 1;
        private const int MONITOR_DEFAULTTONEAREST = 2;

        private const int PHYSICAL_MONITOR_DESCRIPTION_SIZE = 128;

        private const uint WM_GETDISPLAYINFO = 0x23;
        private const byte VCP_CODE_INPUT_SELECT = 0x60;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct PHYSICAL_MONITOR
        {
            public IntPtr hPhysicalMonitor;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = PHYSICAL_MONITOR_DESCRIPTION_SIZE)]
            public string szPhysicalMonitorDescription;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

        [DllImport("dxva2.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, uint dwPhysicalMonitorArraySize, [Out] PHYSICAL_MONITOR[] pPhysicalMonitorArray);

        [DllImport("dxva2.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyPhysicalMonitors(uint dwPhysicalMonitorArraySize, [In] PHYSICAL_MONITOR[] pPhysicalMonitorArray);

        [DllImport("dxva2.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, out uint pdwNumberOfPhysicalMonitors);

        [DllImport("dxva2.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCapabilitiesStringLength(IntPtr hMonitor, out uint pdwCapabilitiesStringLengthInCharacters);

        [DllImport("dxva2.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetVCPFeatureAndVCPFeatureReply(IntPtr hMonitor, byte bVCPCode, out IntPtr pvct, out uint pdwCurrentValue, out uint pdwMaximumValue);

        [DllImport("dxva2.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetVCPFeature(IntPtr hMonitor, byte bVCPCode, uint dwNewValue);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MONITORINFOEX
        {
            public uint cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szDevice;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);
        
        // 获取所有连接的显示器
        public List<DisplayDevice> GetAllDisplayDevices()
        {
            List<DisplayDevice> displays = new();
            Dictionary<string, DisplayDevice> deviceMap = new();

            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData) =>
            {
                // 获取显示器信息
                MONITORINFOEX monitorInfo = new MONITORINFOEX();
                monitorInfo.cbSize = (uint)Marshal.SizeOf(monitorInfo);
                GetMonitorInfo(hMonitor, ref monitorInfo);

                // 创建新的显示设备
                DisplayDevice device = new DisplayDevice
                {
                    DeviceId = monitorInfo.szDevice,
                    Resolution = $"{lprcMonitor.right - lprcMonitor.left}x{lprcMonitor.bottom - lprcMonitor.top}"
                };

                // 尝试获取物理显示器信息
                if (GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, out uint numberOfPhysicalMonitors) && 
                    numberOfPhysicalMonitors > 0)
                {
                    PHYSICAL_MONITOR[] physicalMonitors = new PHYSICAL_MONITOR[numberOfPhysicalMonitors];
                    
                    if (GetPhysicalMonitorsFromHMONITOR(hMonitor, numberOfPhysicalMonitors, physicalMonitors))
                    {
                        try
                        {
                            device.Name = physicalMonitors[0].szPhysicalMonitorDescription;
                            
                            // 尝试获取当前输入源
                            try
                            {
                                GetVCPFeatureAndVCPFeatureReply(
                                    physicalMonitors[0].hPhysicalMonitor, 
                                    VCP_CODE_INPUT_SELECT, 
                                    out IntPtr pvct, 
                                    out uint currentValue, 
                                    out uint maxValue);
                                
                                device.CurrentSource = (InputSource)currentValue;
                                device.TargetSource = (InputSource)currentValue;
                                device.SupportsDDC = true;
                            }
                            catch
                            {
                                device.CurrentSource = InputSource.Unknown;
                                device.TargetSource = InputSource.Unknown;
                                device.SupportsDDC = false;
                            }
                        }
                        finally
                        {
                            DestroyPhysicalMonitors(numberOfPhysicalMonitors, physicalMonitors);
                        }
                    }
                }

                deviceMap[device.DeviceId!] = device;
                return true;
            }, IntPtr.Zero);

            // 分配显示器编号 (匹配Windows显示设置中的编号顺序)
            int displayNumber = 1;
            foreach (var device in deviceMap.Values)
            {
                device.DisplayNumber = displayNumber++;
                displays.Add(device);
            }

            return displays;
        }

        // 设置显示器输入源
        public bool SetDisplaySource(DisplayDevice display, InputSource source)
        {
            bool success = false;
            
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData) =>
            {
                MONITORINFOEX monitorInfo = new MONITORINFOEX();
                monitorInfo.cbSize = (uint)Marshal.SizeOf(monitorInfo);
                GetMonitorInfo(hMonitor, ref monitorInfo);

                // 找到匹配的显示器
                if (monitorInfo.szDevice == display.DeviceId)
                {
                    if (GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, out uint numberOfPhysicalMonitors) && 
                        numberOfPhysicalMonitors > 0)
                    {
                        PHYSICAL_MONITOR[] physicalMonitors = new PHYSICAL_MONITOR[numberOfPhysicalMonitors];
                        
                        if (GetPhysicalMonitorsFromHMONITOR(hMonitor, numberOfPhysicalMonitors, physicalMonitors))
                        {
                            try
                            {
                                // 发送VCP命令切换输入源
                                success = SetVCPFeature(
                                    physicalMonitors[0].hPhysicalMonitor,
                                    VCP_CODE_INPUT_SELECT,
                                    (uint)source);
                            }
                            finally
                            {
                                DestroyPhysicalMonitors(numberOfPhysicalMonitors, physicalMonitors);
                            }
                        }
                    }
                }
                
                return true;
            }, IntPtr.Zero);

            return success;
        }
    }
} 