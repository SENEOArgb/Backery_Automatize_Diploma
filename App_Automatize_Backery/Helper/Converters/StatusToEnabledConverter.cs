using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace App_Automatize_Backery.Helper.Converters
{
    public class StatusToEnabledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int statusId)
            {
                return statusId != 2;  // Если статус 2 (Принята), кнопка блокируется
            }
            return true;  // В остальных случаях кнопка доступна
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
