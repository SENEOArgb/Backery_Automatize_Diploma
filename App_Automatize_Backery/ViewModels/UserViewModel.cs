using App_Automatize_Backery.Helper;
using App_Automatize_Backery.View;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace App_Automatize_Backery.ViewModels
{
    internal class UserViewModel : INotifyPropertyChanged
    {

        public ICommand ExitCommand { get; }

        public UserViewModel()
        {
            ExitCommand = new RelayCommand(ExecuteExit);
        }

        private void ExecuteExit(object obj)
        {
            // Открываем LoginWindow перед закрытием MainWindow
            var loginWindow = new LoginWindow();
            loginWindow.Show();

            // Закрываем MainWindow
            var mainWindow = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            if (mainWindow != null)
            {
                mainWindow.Close();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private int _dailyProduction;
        public int DailyProduction
        {
            get => _dailyProduction;
            set
            {
                if (_dailyProduction != value)
                {
                    _dailyProduction = value;
                    OnPropertyChanged(nameof(DailyProduction));
                }
            }
        }

        // Метод для загрузки данных о произведенных продуктах за сегодняшний день

    }
}
