using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace CspProject.Converters
{
    // IValueConverter arayüzünü kullanarak özel bir dönüştürücü oluşturuyoruz.
    public class  BooleanConverter :MarkupExtension, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (object) (bool) value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (object) (bool) value;
        }

        public override object ProvideValue(IServiceProvider serviceProvider) => (object) this;
    }
}