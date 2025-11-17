using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace CodeExplainer.Desktop.Helpers;

public class BoolToBrushConverter : IValueConverter
{
    public Brush UserBrush { get; set; } = new SolidColorBrush(Color.FromRgb(43, 108, 176));
    public Brush AssistantBrush { get; set; } = new SolidColorBrush(Color.FromRgb(244, 245, 247));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isUser)
        {
            return isUser ? UserBrush : AssistantBrush;
        }
        return AssistantBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
