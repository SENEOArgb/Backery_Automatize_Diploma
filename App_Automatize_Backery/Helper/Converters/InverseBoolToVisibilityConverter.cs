using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;

namespace App_Automatize_Backery.Helper.Converters
{
    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        public bool CollapseInsteadOfHide { get; set; } = false;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isVisible = value is bool b && b;
            if (!isVisible)
                return Visibility.Visible;

            return CollapseInsteadOfHide ? Visibility.Collapsed : Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(value is Visibility visibility && visibility == Visibility.Visible);
        }
    }
}
