using App_Automatize_Backery.Models;
using App_Automatize_Backery.ViewModels;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace App_Automatize_Backery
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow(User user)
        {
            InitializeComponent();
            DataContext = new MainViewModel(user);
        }

        private void Overlay_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            /*if (_isMenuOpen)
            {
                Storyboard sb = (Storyboard)FindResource("CloseMenu");
                sb.Begin();
                _isMenuOpen = false;
            }*/
        }

        private void ToggleMenuButton_Click(object sender, RoutedEventArgs e)
{
   // Если меню скрыто, то открываем
   if (((TranslateTransform)Drawer.RenderTransform).X == -250)
   {
       var openStoryboard = (Storyboard)FindResource("DrawerOpenStoryboard");
       openStoryboard.Begin();

       // Показываем затемнение
       Overlay.Visibility = Visibility.Visible;
   }
   else
   {
       var closeStoryboard = (Storyboard)FindResource("DrawerCloseStoryboard");
       closeStoryboard.Begin();

       // Скрываем затемнение
       Overlay.Visibility = Visibility.Collapsed;
   }
}
    }
}