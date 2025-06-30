using System.ComponentModel;

namespace InputMux.Models
{
    public class DisplayDevice : INotifyPropertyChanged
    {
        private string? _name;
        private string? _deviceId;
        private string? _serialNumber;
        private int _displayNumber;
        private string? _resolution;
        private InputSource _currentSource;
        private InputSource _targetSource;
        private string _status = "就绪";

        public string? Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        public string? DeviceId
        {
            get => _deviceId;
            set { _deviceId = value; OnPropertyChanged(nameof(DeviceId)); }
        }

        public string? SerialNumber
        {
            get => _serialNumber;
            set { _serialNumber = value; OnPropertyChanged(nameof(SerialNumber)); }
        }

        public int DisplayNumber
        {
            get => _displayNumber;
            set { _displayNumber = value; OnPropertyChanged(nameof(DisplayNumber)); }
        }

        public string? Resolution
        {
            get => _resolution;
            set { _resolution = value; OnPropertyChanged(nameof(Resolution)); }
        }

        public InputSource CurrentSource
        {
            get => _currentSource;
            set { _currentSource = value; OnPropertyChanged(nameof(CurrentSource)); }
        }

        public InputSource TargetSource
        {
            get => _targetSource;
            set { _targetSource = value; OnPropertyChanged(nameof(TargetSource)); }
        }

        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(nameof(Status)); }
        }

        public bool SupportsDDC { get; set; }

        public string DisplayInfo => $"显示器 {DisplayNumber}: {Name ?? "未知"} {Resolution ?? ""}";

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 