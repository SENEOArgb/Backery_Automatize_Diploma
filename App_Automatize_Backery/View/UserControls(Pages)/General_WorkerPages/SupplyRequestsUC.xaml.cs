using App_Automatize_Backery.ViewModels;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace App_Automatize_Backery.View.UserControls_Pages_.General_WorkerPages
{
    /// <summary>
    /// Логика взаимодействия для SupplyRequestsUC.xaml
    /// </summary>
    public partial class SupplyRequestsUC : UserControl
    {
        private MainViewModel _mainViewModel;
        public SupplyRequestsUC(MainViewModel mainViewModel)
        {
            InitializeComponent();
            _mainViewModel = mainViewModel;
            this.DataContext = new SupplyRequestViewModel(mainViewModel);
        }
    }
}
