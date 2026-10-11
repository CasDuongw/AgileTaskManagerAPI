using System.Configuration;
using System.Data;
using System.Windows;

using Microsoft.Extensions.DependencyInjection;
using AgileTaskManager.Desktop.Services;
using AgileTaskManager.Desktop.ViewModels;
using System;

namespace AgileTaskManager.Desktop
{
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
            {
                var ex = args.ExceptionObject as Exception;
                MessageBox.Show($"Lỗi hệ thống: {ex?.Message}\n{ex?.StackTrace}", "Lỗi không xác định", MessageBoxButton.OK, MessageBoxImage.Error);
            };

            this.DispatcherUnhandledException += (s, args) =>
            {
                MessageBox.Show($"Lỗi ứng dụng: {args.Exception.Message}\n{args.Exception.StackTrace}", "Lỗi ứng dụng", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            ServiceProvider = serviceCollection.BuildServiceProvider();

            var loginWindow = ServiceProvider.GetRequiredService<LoginWindow>();
            loginWindow.Show();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // DI: Services
            services.AddSingleton<ApiService>();

            // DI: ViewModels
            services.AddTransient<KanbanBoardViewModel>();

            // DI: Windows
            services.AddTransient<LoginWindow>();
            services.AddTransient<DashboardWindow>();
        }
    }
}
