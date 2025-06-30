using System.Collections.ObjectModel;
using InputMux.Models;

namespace InputMux.Services
{
    public class DisplayManager
    {
        private readonly DDCCIService _ddcciService;
        
        public ObservableCollection<DisplayDevice> Displays { get; } = new ObservableCollection<DisplayDevice>();
        
        public DisplayManager()
        {
            _ddcciService = new DDCCIService();
        }
        
        // 刷新显示器列表
        public void RefreshDisplays()
        {
            Displays.Clear();
            
            var detectedDisplays = _ddcciService.GetAllDisplayDevices();
            
            foreach (var display in detectedDisplays)
            {
                Displays.Add(display);
            }
        }
        
        // 切换显示器输入源
        public bool SwitchDisplayInput(DisplayDevice display, InputSource source)
        {
            bool success = _ddcciService.SetDisplaySource(display, source);
            
            if (success)
            {
                display.CurrentSource = source;
                display.Status = "切换成功";
            }
            else
            {
                display.Status = "切换失败";
            }
            
            return success;
        }
    }
} 