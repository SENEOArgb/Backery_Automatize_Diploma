using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace App_Automatize_Backery.Helper.Converters
{
    public class BoolToIconConverter : IValueConverter
    {
        // ConverterParameter: строка вида "ИконкаЕслиFalse;ИконкаЕслиTrue"
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(parameter is string param) || !param.Contains(";"))
                return null;

            var parts = param.Split(';');
            return (value is bool b && b) ? parts[1] : parts[0];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
