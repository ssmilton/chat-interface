using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace OllamaChat.Converters;

/// <summary>
/// Converts bool to color (for connection status indicator)
/// </summary>
public class BoolToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue
                ? new SolidColorBrush(Color.FromRgb(16, 163, 127)) // Green - connected
                : new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Red - disconnected
        }

        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts bool to message style (user vs assistant)
/// </summary>
public class BoolToMessageStyleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Returns style key name based on whether it's a user message
        if (value is bool isUser)
        {
            return isUser ? "UserMessageBorder" : "AssistantMessageBorder";
        }
        return "AssistantMessageBorder";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
