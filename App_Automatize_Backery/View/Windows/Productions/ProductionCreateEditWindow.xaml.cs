using App_Automatize_Backery.Helper;
using App_Automatize_Backery.Models;
using App_Automatize_Backery.ViewModels.ProductionsVM.SupportVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace App_Automatize_Backery.View.Windows.Productions
{
    /// <summary>
    /// Логика взаимодействия для ProductionCreateEditWindow.xaml
    /// </summary>
    public partial class ProductionCreateEditWindow : Window
    {
        public ProductionCreateEditWindow()
        {
            InitializeComponent();
        }

        public ProductionCreateEditWindow(MinBakeryDbContext minBakeryDbContext, StockService stockService, Sale existSale)
        {
            InitializeComponent();
            DataContext = new ProductionCreateViewModel(minBakeryDbContext, stockService, existSale);
        }

        private void timeEnd_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Аналогично для времени окончания
            string time = ((TextBox)sender).Text + e.Text;
            TimeSpan parsedTime;

            if (TimeSpan.TryParse(time, out parsedTime))
            {
                if (parsedTime < TimeSpan.FromHours(8) || parsedTime > TimeSpan.FromHours(21))
                {
                    e.Handled = true; // Отклонить ввод, если время вне диапазона
                }
            }
        }

        private void timeStart_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Проверка времени на диапазон от 08:00 до 21:00
            string time = ((TextBox)sender).Text + e.Text;
            TimeSpan parsedTime;
            TimeSpan currentTime = DateTime.Now.TimeOfDay;

            if (TimeSpan.TryParse(time, out parsedTime))
            {
                if (parsedTime < TimeSpan.FromHours(8) || parsedTime > TimeSpan.FromHours(21) || parsedTime < currentTime)
                {
                    e.Handled = true; // Отклонить ввод, если время вне диапазона
                }
            }
        }
    }
}
