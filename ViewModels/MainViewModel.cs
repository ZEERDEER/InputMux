using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using InputMux.Models;
using InputMux.Services;

namespace InputMux.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly DisplayManager _displayManager;
        private string _statusMessage = string.Empty;
        
        public ObservableCollection<DisplayDevice> Displays => _displayManager.Displays;
        
        public Dictionary<InputSource, string> AvailableInputSources { get; } = InputSourceExtensions.GetAllSources();
        
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged(nameof(StatusMessage));
            }
        }
        
        public ICommand RefreshCommand { get; }
        public ICommand SwitchInputCommand { get; }
        
        public MainViewModel()
        {
            _displayManager = new DisplayManager();
            
            RefreshCommand = new RelayCommand(_ => RefreshDisplays());
            SwitchInputCommand = new RelayCommand<DisplayDevice>(SwitchInput);
            
            // 初始化时刷新显示器列表
            RefreshDisplays();
        }
        
        private void RefreshDisplays()
        {
            try
            {
                StatusMessage = "正在刷新显示器列表...";
                _displayManager.RefreshDisplays();
                StatusMessage = $"已找到 {Displays.Count} 台显示器";
            }
            catch (Exception ex)
            {
                StatusMessage = $"刷新失败: {ex.Message}";
            }
        }
        
        private void SwitchInput(DisplayDevice? display)
        {
            if (display == null) return;
            
            try
            {
                StatusMessage = $"正在切换 {display.DisplayInfo} 的输入源...";
                
                bool success = _displayManager.SwitchDisplayInput(display, display.TargetSource);
                
                StatusMessage = success 
                    ? $"已将 {display.DisplayInfo} 切换到 {display.TargetSource.GetDescription()}" 
                    : $"切换 {display.DisplayInfo} 失败";
            }
            catch (Exception ex)
            {
                StatusMessage = $"切换失败: {ex.Message}";
            }
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Predicate<object?>? _canExecute;

        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute == null || _canExecute(parameter);
        
        public void Execute(object? parameter) => _execute(parameter);
        
        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
    
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Predicate<T?>? _canExecute;

        public RelayCommand(Action<T?> execute, Predicate<T?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute == null || _canExecute(parameter is T or null ? (T?)parameter : default);
        
        public void Execute(object? parameter) => _execute(parameter is T or null ? (T?)parameter : default);
        
        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
} 