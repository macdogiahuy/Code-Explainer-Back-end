using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace CodeExplainer.Desktop.Helpers;

public class BoolToBrushConverter : IValueConverter
{
    public Brush UserBrush { get; set; } = new SolidColorBrush(Color.FromRgb(43, 108, 176));
    public Brush AssistantBrush { get; set; } = new SolidColorBrush(Color.FromRgb(244, 245, 247));
    public Brush UserAccentBrush { get; set; } = new SolidColorBrush(Color.FromRgb(30, 96, 165));
    public Brush AssistantAccentBrush { get; set; } = new SolidColorBrush(Color.FromRgb(100, 116, 139));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var param = parameter as string;
        var isAccent = string.Equals(param, "AccentBar", StringComparison.OrdinalIgnoreCase);

        if (value is bool isUser)
        {
            if (isAccent)
            {
                return isUser ? UserAccentBrush : AssistantAccentBrush;
            }
            return isUser ? UserBrush : AssistantBrush;
        }
        return isAccent ? AssistantAccentBrush : AssistantBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
