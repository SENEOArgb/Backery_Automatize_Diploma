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
    public class TechnologistArchiveVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2 ||
                !(values[0] is bool isArchiveVisible) ||
                !(values[1] is Visibility technologistVisibility))
                return Visibility.Collapsed;

            if (technologistVisibility != Visibility.Visible)
                return Visibility.Collapsed;

            string mode = parameter as string;
            if (mode == "Archive" && !isArchiveVisible)
                return Visibility.Visible;
            if (mode == "Restore" && isArchiveVisible)
                return Visibility.Visible;

            return Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

}
