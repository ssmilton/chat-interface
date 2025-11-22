using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using Markdig;
using Markdig.Wpf;

namespace OllamaChat.Converters;

/// <summary>
/// Converts markdown text to a FlowDocument using Markdig.Wpf
/// </summary>
public class MarkdownToFlowDocumentConverter : IValueConverter
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseSupportedExtensions()
        .Build();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string markdown && !string.IsNullOrEmpty(markdown))
        {
            try
            {
                var flowDocument = Markdig.Wpf.Markdown.ToFlowDocument(markdown, Pipeline);

                // Apply styling to match our dark theme
                flowDocument.PagePadding = new Thickness(0);
                flowDocument.FontFamily = new System.Windows.Media.FontFamily("Segoe UI");
                flowDocument.FontSize = 14;
                flowDocument.LineHeight = 24;

                return flowDocument;
            }
            catch
            {
                // If markdown parsing fails, return a simple FlowDocument with the raw text
                var fallbackDocument = new FlowDocument(new Paragraph(new Run(markdown)));
                fallbackDocument.PagePadding = new Thickness(0);
                return fallbackDocument;
            }
        }

        // Return empty FlowDocument for null/empty content
        var emptyDocument = new FlowDocument();
        emptyDocument.PagePadding = new Thickness(0);
        return emptyDocument;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
