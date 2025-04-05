using App_Automatize_Backery.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;

namespace App_Automatize_Backery.ViewModels
{
    internal class LoginViewModel : INotifyPropertyChanged
    {
        private readonly AuthService _authService;
        private string _login;
        private string _password;

        public string Login
        {
            get => _login;
            set => SetProperty(ref _login, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public ICommand LoginCommand { get; }

        public LoginViewModel(AuthService authService)
        {
            _authService = authService;
            LoginCommand = new RelayCommand(ExecuteLogin);
        }

        private void ExecuteLogin(object parameter)
        {
            var user = _authService.Authenticate(Login, Password);
            if (user != null)
            {

                MessageBox.Show($"Добро пожаловать, {user.UserName} {user.UserSurname}");
                var mainWindow = new MainWindow(user);
                mainWindow.Show();
                Application.Current.Windows[0]?.Close();
            }
            else
            {
                MessageBox.Show("Неверный логин или пароль!");
            }
        }

        // Метод для обработки изменения пароля
        public void UpdatePassword(string newPassword)
        {
            Password = newPassword;
        }

        protected bool SetProperty<T>(ref T backingField, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingField, value))
                return false;

            backingField = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }
}
