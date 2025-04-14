using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace App_Automatize_Backery.Helper.Converters
{
    public class BoolToCommandConverter : IValueConverter
    {
        // ConverterParameter: строка вида "КомандаЕслиFalse;КомандаЕслиTrue"
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(parameter is string param) || !(value is bool boolVal)) return null;

            var parts = param.Split(';');
            var commandName = boolVal ? parts[1] : parts[0];

            var dataContext = App.Current.MainWindow?.DataContext;
            var vm = dataContext as dynamic;

            return vm?.RawMaterialsUC?.DataContext?.GetType().GetProperty(commandName)?.GetValue(vm.RawMaterialsUC.DataContext)
                   ?? vm?.GetType().GetProperty(commandName)?.GetValue(vm);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
