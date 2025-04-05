using App_Automatize_Backery.Helper;
using App_Automatize_Backery.Models;
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
using System.Windows.Shapes;

namespace App_Automatize_Backery.View
{
    /// <summary>
    /// Логика взаимодействия для LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            this.DataContext = new LoginViewModel(new AuthService(App.DbContext));
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            var viewModel = (LoginViewModel)this.DataContext;
            viewModel.UpdatePassword(((PasswordBox)sender).Password);
            if (this.DataContext is LoginViewModel targetViewModel)
            {
                viewModel.Password = ((PasswordBox)sender).Password;
            }
        }
    }
}
