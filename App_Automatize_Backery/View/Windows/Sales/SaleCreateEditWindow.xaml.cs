using App_Automatize_Backery.Models;
using App_Automatize_Backery.ViewModels;
using App_Automatize_Backery.ViewModels.SalesVM;
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

namespace App_Automatize_Backery.View.Windows.Sales
{
    /// <summary>
    /// Логика взаимодействия для SaleCreateEditWindow.xaml
    /// </summary>
    public partial class SaleCreateEditWindow : Window
    {
        public SaleCreateEditWindow(MinBakeryDbContext context, MainViewModel mainViewModel, Action action)
        {
            InitializeComponent();
            this.DataContext = new SaleCreateEditViewModel(context, mainViewModel, action, this);
        }

        public SaleCreateEditWindow(MinBakeryDbContext context, MainViewModel mainViewModel,Action action, Sale? sale )
            : this(context, mainViewModel, action) // Передаем mainViewModel
        {
            this.DataContext = new SaleCreateEditViewModel(context, mainViewModel, action,this, sale);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DatePickerControl.DisplayDateStart = DateTime.Today;
            DatePickerControl.DisplayDateEnd = DateTime.Today.AddDays(7);
        }

        private void selTime_SelectedTimeChanged(object sender, RoutedPropertyChangedEventArgs<DateTime?> e)
        {
            if (selTime.SelectedTime.HasValue)
            {
                var selectedTime = selTime.SelectedTime.Value.TimeOfDay;

                if (selectedTime < TimeSpan.FromHours(8) || selectedTime > TimeSpan.FromHours(21))
                {
                    MessageBox.Show("Выберите время между 08:00 и 21:00.", "Недопустимое время", MessageBoxButton.OK, MessageBoxImage.Warning);
                    selTime.SelectedTime = null;
                }
            }
        }
    }
}
