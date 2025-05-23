﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace App_Automatize_Backery.Helper.Converters
{
    internal class TimeSpanToDateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TimeSpan timeSpan)
            {
                // Преобразуем TimeSpan в DateTime и форматируем для 24-часового отображения
                DateTime time = DateTime.Today.Add(timeSpan);
                return time.ToString("HH:mm", culture); // 24-часовой формат
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
            {
                return dateTime.TimeOfDay; // Преобразуем DateTime обратно в TimeSpan
            }
            return null;
        }
    }
}
