using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace OllamaChat.Behaviors;

/// <summary>
/// Attached behavior to enable binding FlowDocument to RichTextBox
/// </summary>
public static class RichTextBoxBehavior
{
    public static readonly DependencyProperty DocumentProperty =
        DependencyProperty.RegisterAttached(
            "Document",
            typeof(FlowDocument),
            typeof(RichTextBoxBehavior),
            new FrameworkPropertyMetadata(null, OnDocumentChanged));

    public static FlowDocument GetDocument(DependencyObject obj)
    {
        return (FlowDocument)obj.GetValue(DocumentProperty);
    }

    public static void SetDocument(DependencyObject obj, FlowDocument value)
    {
        obj.SetValue(DocumentProperty, value);
    }

    private static void OnDocumentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RichTextBox richTextBox)
        {
            if (e.NewValue is FlowDocument newDocument)
            {
                // We need to create a copy because FlowDocument can only belong to one RichTextBox
                richTextBox.Document = CloneFlowDocument(newDocument);
            }
            else
            {
                richTextBox.Document = new FlowDocument();
            }
        }
    }

    private static FlowDocument CloneFlowDocument(FlowDocument source)
    {
        // Create a new FlowDocument and copy blocks from the source
        var clone = new FlowDocument
        {
            PagePadding = source.PagePadding,
            FontFamily = source.FontFamily,
            FontSize = source.FontSize,
            LineHeight = source.LineHeight,
            Foreground = source.Foreground,
            Background = source.Background
        };

        // Use TextRange to copy content
        var sourceRange = new TextRange(source.ContentStart, source.ContentEnd);
        var targetRange = new TextRange(clone.ContentStart, clone.ContentEnd);

        using (var stream = new System.IO.MemoryStream())
        {
            sourceRange.Save(stream, DataFormats.Xaml);
            stream.Seek(0, System.IO.SeekOrigin.Begin);
            targetRange.Load(stream, DataFormats.Xaml);
        }

        return clone;
    }
}
