using System.ComponentModel;

namespace InputMux.Models
{
    public enum InputSource
    {
        [Description("未知")]
        Unknown = 0,
        
        [Description("DisplayPort")]
        DisplayPort = 15,
        
        [Description("HDMI1")]
        HDMI1 = 17,
        
        [Description("HDMI2")]
        HDMI2 = 18,
        
        // [Description("USB-C")]
        // USBC = 27
    }

    public static class InputSourceExtensions
    {
        public static string GetDescription(this InputSource source)
        {
            var fieldInfo = source.GetType().GetField(source.ToString());
            
            if (fieldInfo?.GetCustomAttributes(typeof(DescriptionAttribute), false) is DescriptionAttribute[] attributes && attributes.Length > 0)
            {
                return attributes[0].Description;
            }
            
            return source.ToString();
        }
        
        public static Dictionary<InputSource, string> GetAllSources()
        {
            return Enum.GetValues(typeof(InputSource))
                .Cast<InputSource>()
                .Where(s => s != InputSource.Unknown)
                .ToDictionary(s => s, s => s.GetDescription());
        }
    }
} 