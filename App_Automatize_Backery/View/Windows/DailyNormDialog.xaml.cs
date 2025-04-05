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

namespace App_Automatize_Backery.View.Windows
{
    /// <summary>
    /// Логика взаимодействия для DailyNormDialog.xaml
    /// </summary>
    public partial class DailyNormDialog : Window
    {
        public int InputNorm { get; private set; }

        public DailyNormDialog()
        {
            InitializeComponent();
        }

        private void OnSetNormClick(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(NormInput.Text, out int norm))
            {
                InputNorm = norm;
                DialogResult = true;  // Закрываем окно и передаем значение
            }
            else
            {
                MessageBox.Show("Введите корректное число", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
