using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace WebBoggler.Converters
{
    public class PlayerReadyToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isReady)
            {
                if (isReady)
                {
                    // Verde per "pronto"
                    return new SolidColorBrush(Color.FromArgb(255, 0, 200, 0));
                }
                else
                {
                    // Rosso per "non pronto"
                    return new SolidColorBrush(Color.FromArgb(255, 200, 0, 0));
                }
            }

            // Default: grigio
            return new SolidColorBrush(Color.FromArgb(255, 160, 160, 240));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
