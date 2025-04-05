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
        public SaleCreateEditWindow(MinBakeryDbContext context, MainViewModel mainViewModel)
        {
            InitializeComponent();
            this.DataContext = new SaleCreateEditViewModel(context, mainViewModel);
        }

        public SaleCreateEditWindow(MinBakeryDbContext context, MainViewModel mainViewModel, Sale? sale )
            : this(context, mainViewModel) // Передаем mainViewModel
        {
            this.DataContext = new SaleCreateEditViewModel(context, mainViewModel, sale);
        }
    }
}
