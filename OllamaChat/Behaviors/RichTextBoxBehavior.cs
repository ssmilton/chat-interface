using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

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
            // Ensure we only attach the handler once
            richTextBox.PreviewMouseLeftButtonDown -= RichTextBox_PreviewMouseLeftButtonDown;
            richTextBox.PreviewMouseLeftButtonDown += RichTextBox_PreviewMouseLeftButtonDown;

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

    private static void RichTextBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is RichTextBox richTextBox)
        {
            // Get the position where the user clicked
            var position = e.GetPosition(richTextBox);
            var textPointer = richTextBox.GetPositionFromPoint(position, true);

            if (textPointer != null)
            {
                // Walk up the parent chain to find a Hyperlink
                var parent = textPointer.Parent;
                while (parent != null)
                {
                    if (parent is Hyperlink hyperlink)
                    {
                        string? url = null;

                        // First try NavigateUri
                        if (hyperlink.NavigateUri != null)
                        {
                            url = hyperlink.NavigateUri.AbsoluteUri;
                        }

                        // If NavigateUri is null, try to get URL from the hyperlink's text content
                        if (string.IsNullOrEmpty(url))
                        {
                            var textRange = new TextRange(hyperlink.ContentStart, hyperlink.ContentEnd);
                            var text = textRange.Text?.Trim();
                            if (!string.IsNullOrEmpty(text) && (text.StartsWith("http://") || text.StartsWith("https://")))
                            {
                                url = text;
                            }
                        }

                        if (!string.IsNullOrEmpty(url))
                        {
                            try
                            {
                                Debug.WriteLine($"Opening URL: {url}");
                                Process.Start(new ProcessStartInfo
                                {
                                    FileName = url,
                                    UseShellExecute = true
                                });
                                e.Handled = true;
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Failed to open URL: {ex.Message}");
                            }
                        }
                        return;
                    }

                    // Move up the tree
                    if (parent is FrameworkContentElement fce)
                    {
                        parent = fce.Parent;
                    }
                    else if (parent is FrameworkElement fe)
                    {
                        parent = fe.Parent;
                    }
                    else
                    {
                        break;
                    }
                }
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
