using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WebBoggler.Converters
{
    public class PlayerReadyToFontWeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isReady)
            {
                // Grassetto se pronto, normale altrimenti
                return isReady ? FontWeights.Bold : FontWeights.Normal;
            }

            return FontWeights.Normal;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
