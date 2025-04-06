using App_Automatize_Backery.Helper;
using App_Automatize_Backery.Models;
using App_Automatize_Backery.View.UserControls_Pages_.General_WorkerPages;
using App_Automatize_Backery.ViewModels.ProductionsVM;
using Microsoft.EntityFrameworkCore;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace App_Automatize_Backery
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static MinBakeryDbContext DbContext { get; private set; }

        public App()
        {
            CultureInfo cultureInfo = new CultureInfo("ru-RU");
            Thread.CurrentThread.CurrentCulture = cultureInfo;
            Thread.CurrentThread.CurrentUICulture = cultureInfo;
            InitializeComponent();
            DbContext = new MinBakeryDbContext();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var stockService = new StockService(App.DbContext);
            var productionViewModel = new ProductionViewModel(App.DbContext, stockService);

            //_ = productionViewModel.MonitorProductionsAsync();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            DbContext.Dispose();
            base.OnExit(e);
        }
    }

}
