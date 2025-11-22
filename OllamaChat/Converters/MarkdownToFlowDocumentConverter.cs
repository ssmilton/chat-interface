using System.Diagnostics;
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

                // Make hyperlinks clickable
                AttachHyperlinkHandlers(flowDocument);

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

    /// <summary>
    /// Recursively find and attach click handlers to all hyperlinks in the FlowDocument
    /// </summary>
    private static void AttachHyperlinkHandlers(FlowDocument document)
    {
        foreach (var block in document.Blocks)
        {
            AttachHyperlinkHandlersToBlock(block);
        }
    }

    private static void AttachHyperlinkHandlersToBlock(Block block)
    {
        if (block is Paragraph paragraph)
        {
            AttachHyperlinkHandlersToInlines(paragraph.Inlines);
        }
        else if (block is List list)
        {
            foreach (var listItem in list.ListItems)
            {
                foreach (var listBlock in listItem.Blocks)
                {
                    AttachHyperlinkHandlersToBlock(listBlock);
                }
            }
        }
        else if (block is Section section)
        {
            foreach (var sectionBlock in section.Blocks)
            {
                AttachHyperlinkHandlersToBlock(sectionBlock);
            }
        }
        else if (block is Table table)
        {
            foreach (var rowGroup in table.RowGroups)
            {
                foreach (var row in rowGroup.Rows)
                {
                    foreach (var cell in row.Cells)
                    {
                        foreach (var cellBlock in cell.Blocks)
                        {
                            AttachHyperlinkHandlersToBlock(cellBlock);
                        }
                    }
                }
            }
        }
        else if (block is BlockUIContainer blockUIContainer)
        {
            // Handle BlockUIContainer if needed
        }
    }

    private static void AttachHyperlinkHandlersToInlines(InlineCollection inlines)
    {
        foreach (var inline in inlines)
        {
            if (inline is Hyperlink hyperlink)
            {
                hyperlink.RequestNavigate += Hyperlink_RequestNavigate;
                hyperlink.Cursor = System.Windows.Input.Cursors.Hand;

                // Also process any inlines within the hyperlink
                AttachHyperlinkHandlersToInlines(hyperlink.Inlines);
            }
            else if (inline is Span span)
            {
                AttachHyperlinkHandlersToInlines(span.Inlines);
            }
            else if (inline is InlineUIContainer)
            {
                // Handle InlineUIContainer if needed
            }
        }
    }

    /// <summary>
    /// Handler to open hyperlinks in the default browser
    /// </summary>
    private static void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
    {
        try
        {
            var url = e.Uri?.AbsoluteUri;
            if (!string.IsNullOrEmpty(url))
            {
                // Open URL in default browser
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to open URL: {ex.Message}");
        }

        e.Handled = true;
    }
}
