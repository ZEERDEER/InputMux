using System.Windows;
using System.Windows.Threading;

namespace InputMux
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        public App()
        {
            // 全局未处理异常处理
            DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // 记录异常并显示友好错误消息
            System.Windows.MessageBox.Show($"发生错误: {e.Exception.Message}\n\n如果问题持续存在，请检查是否以管理员权限运行或联系开发者。", 
                            "错误", 
                            MessageBoxButton.OK, 
                            MessageBoxImage.Error);
            
            e.Handled = true;
        }
    }
}

