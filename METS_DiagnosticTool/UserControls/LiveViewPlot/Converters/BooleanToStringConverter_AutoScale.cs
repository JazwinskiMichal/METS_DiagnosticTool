using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace METS_DiagnosticTool_UI.UserControls.LiveViewPlot.Converters
{
    public class BooleanToStringConverter_AutoScale : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool)
            {
                if ((bool)value == true)
                    return "Auto Scale is Active";
                else
                    return "Auto Scale is Inactive";
            }
            else
                return "Auto Scale is Inactive";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return true;
        }
    }
}
