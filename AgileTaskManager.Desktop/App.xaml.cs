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
